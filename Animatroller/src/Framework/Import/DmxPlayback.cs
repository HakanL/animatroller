using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Animatroller.Framework.Controller;
using Animatroller.Framework.Import.FileFormat;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.Import
{
    public class DmxPlayback : ICanExecute, IDisposable
    {
        private string name;
        private Subroutine sub;
        private FileStream file;
        private BinaryReader binRead;
        private IPixel2 device;
        private Stopwatch watch;
        private long nextStop;
        private DmxFrame dmxFrame;
        private int triggerSyncOnUniverse;
        private Dictionary<int, int[]> pixelMapping;
        private byte[] rgbValues;
        private int pixelWidth;
        private int pixelHeight;
        private bool loop;

        public DmxPlayback([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;
            this.sub = new Subroutine("SUB_" + this.name);
            this.watch = new Stopwatch();
            this.pixelMapping = new Dictionary<int, int[]>();
            long timestampOffset = 0;

            this.sub.RunAction(ins =>
            {
                var bitmap = (Bitmap)this.device.GetFrameBuffer(this.sub.Token, this.device)[DataElements.PixelBitmap];
                if (bitmap.Width != this.pixelWidth || bitmap.Height != this.pixelHeight)
                    throw new ArgumentException("Invalid bitmap size");

                var bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                int stride = 4 * ((bitmap.Width * bytesPerPixel + 3) / 4);
                int byteCount = stride * bitmap.Height;
                this.rgbValues = new byte[byteCount];

                do
                {
                    // See if we should restart
                    if (this.file.Position >= this.file.Length)
                    {
                        // Restart
                        this.file.Position = 0;
                        this.dmxFrame = null;
                        this.watch.Reset();
                    }

                    if (this.dmxFrame == null)
                    {
                        this.dmxFrame = ReadFrame(this.binRead);
                        timestampOffset = this.dmxFrame.TimestampMS;
                    }

                    this.watch.Start();

                    while (!ins.IsCancellationRequested && this.file.Position < this.file.Length)
                    {
                        // Calculate when the next stop is
                        this.nextStop = this.dmxFrame.TimestampMS - timestampOffset;

                        long msLeft = this.nextStop - this.watch.ElapsedMilliseconds;
                        if (msLeft <= 0)
                        {
                            // Output
                            OutputData(this.dmxFrame, bitmap, bitmapRect, stride);

                            // Read next frame
                            this.dmxFrame = ReadFrame(this.binRead);
                            continue;
                        }
                        else if (msLeft < 16)
                        {
                            SpinWait.SpinUntil(() => this.watch.ElapsedMilliseconds >= this.nextStop);
                            continue;
                        }

                        Thread.Sleep(1);
                    }
                } while (!ins.IsCancellationRequested && this.loop);

                this.watch.Stop();
            });
        }

        private void OutputData(DmxFrame dmxFrame, Bitmap bitmap, Rectangle bitmapRect, int stride)
        {
            int[] mapping;
            if (dmxFrame.Data != null && this.pixelMapping.TryGetValue(dmxFrame.Universe, out mapping))
            {
                BitmapData bitmapData = bitmap.LockBits(bitmapRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

                int maxLen = Math.Min(dmxFrame.Data.Length, mapping.Length * 3);
                for (int pos = 0; pos < maxLen; pos++)
                {
                    int bytePos = mapping[pos];
                    if (bytePos >= 0 && bytePos < this.rgbValues.Length)
                        this.rgbValues[bytePos] = dmxFrame.Data[pos];
                }

                System.Runtime.InteropServices.Marshal.Copy(this.rgbValues, 0, bitmapData.Scan0, this.rgbValues.Length);
                bitmap.UnlockBits(bitmapData);
            }

            if (dmxFrame.Universe == this.triggerSyncOnUniverse)
                this.device.PushOutput(this.sub.Token);
        }

        private DmxFrame ReadFrame(BinaryReader binRead)
        {
            var target = new DmxFrame();
            target.Start = binRead.ReadByte();
            target.TimestampMS = (uint)binRead.ReadInt32();
            target.Universe = (ushort)binRead.ReadInt16();
            switch (target.Start)
            {
                case 1:
                    target.Len = (ushort)binRead.ReadInt16();
                    target.Data = binRead.ReadBytes(target.Len);
                    break;

                case 2:
                    break;

                default:
                    throw new ArgumentException("Invalid data");
            }
            target.End = binRead.ReadByte();

            if (target.End != 4)
                throw new ArgumentException("Invalid data");

            return target;
        }

        public void SetOutput(IPixel1D2 device, Dictionary<int, Utility.PixelMap[]> pixelMapping)
        {
            if (this.device != null)
                throw new ArgumentException("Can only control one device");

            this.device = device;
            this.pixelWidth = device.Pixels;
            this.pixelHeight = 1;

            UpdatePixelMapping(pixelMapping);

            this.sub.LockWhenRunning(device);
        }

        public void SetOutput(IPixel2D2 device, Dictionary<int, Utility.PixelMap[]> pixelMapping)
        {
            if (this.device != null)
                throw new ArgumentException("Can only control one device");

            this.device = device;
            this.pixelWidth = device.PixelWidth;
            this.pixelHeight = device.PixelHeight;

            UpdatePixelMapping(pixelMapping);

            this.sub.LockWhenRunning(device);
        }

        private void UpdatePixelMapping(Dictionary<int, Utility.PixelMap[]> pixelMapping)
        {
            int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
            int stride = 4 * ((this.pixelWidth * bytesPerPixel + 3) / 4);

            foreach (var kvp in pixelMapping)
            {
                int[] mapping;
                if (!this.pixelMapping.TryGetValue(kvp.Key, out mapping))
                {
                    mapping = new int[512];
                    for (int i = 0; i < mapping.Length; i++)
                        mapping[i] = -1;

                    this.pixelMapping.Add(kvp.Key, mapping);
                }

                for (int pos = 0; pos < Math.Min(kvp.Value.Length, mapping.Length); pos++)
                {
                    var map = kvp.Value[pos];

                    if (map.X >= this.pixelWidth || map.Y >= this.pixelHeight)
                        continue;

                    int rgbOffset = -1;
                    switch (map.ColorComponent)
                    {
                        case Utility.ColorComponent.R:
                            rgbOffset = 2;
                            break;

                        case Utility.ColorComponent.G:
                            rgbOffset = 1;
                            break;

                        case Utility.ColorComponent.B:
                            rgbOffset = 0;
                            break;
                    }
                    if (rgbOffset == -1)
                        continue;

                    int bytePos = map.X * 3 + map.Y * stride + rgbOffset;

                    if (bytePos >= stride * this.pixelHeight)
                        throw new ArgumentOutOfRangeException("Invalid pixel mapping");

                    mapping[pos] = bytePos;
                }
            }
        }

        public bool IsMultiInstance
        {
            get { return false; }
        }

        public string Name
        {
            get { return this.name; }
        }

        public bool Loop
        {
            get { return this.loop; }
            set { this.loop = value; }
        }

        public void Execute(CancellationToken cancelToken)
        {
            this.sub.Execute(cancelToken);
        }

        public void Load(string fileName, int triggerSyncOnUniverse)
        {
            this.triggerSyncOnUniverse = triggerSyncOnUniverse;
            this.file = System.IO.File.OpenRead(fileName);
            this.binRead = new System.IO.BinaryReader(file);
        }

        public void Dispose()
        {
            if (this.file != null)
            {
                this.file.Dispose();

                this.file = null;
            }
        }

        public Task Run()
        {
            Task task;
            Executor.Current.Execute(this, out task);

            return task;
        }

        public Task Run(out System.Threading.CancellationTokenSource cts)
        {
            Task task;
            cts = Executor.Current.Execute(this, out task);

            return task;
        }

        public void Stop()
        {
            Executor.Current.Cancel(this);
        }
    }
}
