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
        private IFileReader reader;
        private IPixel2 device;
        private Stopwatch masterClock;
        private long nextStop;
        private DmxData dmxFrame;
        private int triggerSyncOnUniverse;
        private Dictionary<int, int[]> pixelMapping;
        private byte[] rgbValues;
        private int pixelWidth;
        private int pixelHeight;
        private bool loop;
        private int channel;
        private Bitmap bitmap;
        private Rectangle bitmapRect;

        public DmxPlayback(int channel = 0, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;
            this.channel = channel;
            this.sub = new Subroutine("SUB_" + this.name);
            this.masterClock = new Stopwatch();
            this.pixelMapping = new Dictionary<int, int[]>();
            long timestampOffset = 0;

            this.sub.RunAction(ins =>
            {
                if (this.bitmap == null)
                    Init();

                do
                {
                    // See if we should restart
                    if (!this.reader.DataAvailable)
                    {
                        // Restart
                        this.reader.Rewind();
                        dmxFrame = null;
                        this.masterClock.Reset();
                    }

                    if (this.dmxFrame == null)
                    {
                        this.dmxFrame = this.reader.ReadFrame();
                        timestampOffset = (long)this.dmxFrame.TimestampMS;
                    }

                    this.masterClock.Start();

                    while (!ins.IsCancellationRequested)
                    {
                        // Calculate when the next stop is
                        this.nextStop = (long)this.dmxFrame.TimestampMS - timestampOffset;

                        long msLeft = this.nextStop - this.masterClock.ElapsedMilliseconds;
                        if (msLeft <= 0)
                        {
                            // Output
                            OutputData();

                            // Read next frame
                            this.dmxFrame = this.reader.ReadFrame();
                            if (this.dmxFrame == null)
                                break;
                            continue;
                        }
                        else if (msLeft < 16)
                        {
                            SpinWait.SpinUntil(() => this.masterClock.ElapsedMilliseconds >= this.nextStop);
                            continue;
                        }

                        Thread.Sleep(1);
                    }
                } while (!ins.IsCancellationRequested && this.loop);

                this.masterClock.Stop();
            });
        }

        private void OutputData()
        {
            if (this.dmxFrame.Data != null && this.pixelMapping.TryGetValue(this.dmxFrame.Universe, out int[] mapping))
            {
                BitmapData bitmapData = this.bitmap.LockBits(this.bitmapRect, ImageLockMode.WriteOnly, this.bitmap.PixelFormat);

                int maxLen = Math.Min(this.dmxFrame.Data.Length, mapping.Length * 3);
                for (int pos = 0; pos < maxLen; pos++)
                {
                    int bytePos = mapping[pos];
                    if (bytePos < 0)
                        continue;

                    this.rgbValues[bytePos] = this.dmxFrame.Data[pos];
                }

                System.Runtime.InteropServices.Marshal.Copy(this.rgbValues, 0, bitmapData.Scan0, this.rgbValues.Length);
                bitmap.UnlockBits(bitmapData);
            }

            if (this.dmxFrame.Universe == this.triggerSyncOnUniverse)
                this.device.PushOutput(this.channel, this.sub.Token);
        }

        private void Init()
        {
            this.bitmap = (Bitmap)this.device.GetFrameBuffer(channel, this.sub.Token, this.device)[DataElements.PixelBitmap];
            if (this.bitmap.Width != this.pixelWidth || this.bitmap.Height != this.pixelHeight)
                throw new ArgumentException("Invalid bitmap size");

            this.bitmapRect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);

            int bytesPerPixel = Bitmap.GetPixelFormatSize(this.bitmap.PixelFormat) / 8;
            int stride = 4 * ((this.bitmap.Width * bytesPerPixel + 3) / 4);
            int byteCount = stride * this.bitmap.Height;
            this.rgbValues = new byte[byteCount];

            this.dmxFrame = null;
            this.masterClock.Reset();
        }

        public void SetOutput(IPixel1D2 device, Dictionary<int, Utility.PixelMap[]> pixelMapping, int startUniverse)
        {
            if (this.device != null)
                throw new ArgumentException("Can only control one device");

            this.device = device;
            this.pixelWidth = device.Pixels;
            this.pixelHeight = 1;

            UpdatePixelMapping(pixelMapping, startUniverse);

            this.sub.LockWhenRunning(device);
        }

        public void SetOutput(IPixel2D2 device, Dictionary<int, Utility.PixelMap[]> pixelMapping, int startUniverse)
        {
            if (this.device != null)
                throw new ArgumentException("Can only control one device");

            this.device = device;
            this.pixelWidth = device.PixelWidth;
            this.pixelHeight = device.PixelHeight;

            UpdatePixelMapping(pixelMapping, startUniverse);

            this.sub.LockWhenRunning(device);
        }

        private void UpdatePixelMapping(Dictionary<int, Utility.PixelMap[]> pixelMapping, int startUniverse)
        {
            int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
            int stride = 4 * ((this.pixelWidth * bytesPerPixel + 3) / 4);

            foreach (var kvp in pixelMapping)
            {
                if (!this.pixelMapping.TryGetValue(kvp.Key + startUniverse, out int[] mapping))
                {
                    mapping = new int[512];
                    for (int i = 0; i < mapping.Length; i++)
                        mapping[i] = -1;

                    this.pixelMapping.Add(kvp.Key + startUniverse, mapping);
                }

                for (int pos = 0; pos < Math.Min(kvp.Value.Length, mapping.Length); pos++)
                {
                    var map = kvp.Value[pos];

                    if (map.X >= this.pixelWidth || map.Y >= this.pixelHeight)
                        continue;

                    int rgbOffset;
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

                        default:
                            continue;
                    }

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

        public void Load(IFileReader reader, int triggerSyncOnUniverse)
        {
            Stop();

            this.triggerSyncOnUniverse = triggerSyncOnUniverse;
            this.reader = reader;
            this.reader.Rewind();

            this.bitmap = null;
        }

        public void Load(IFileReader2 reader)
        {
            Stop();

            this.triggerSyncOnUniverse = reader.TriggerUniverseId;
            this.reader = reader;
            this.reader.Rewind();

            this.bitmap = null;
        }

        public void Dispose()
        {
        }

        public Task Run(bool? loop = null)
        {
            if (loop.HasValue)
                Loop = loop.Value;

            Task task;
            Executor.Current.Execute(this, out task);

            return task;
        }

        public Task Run(out System.Threading.CancellationTokenSource cts, bool? loop = null)
        {
            if (loop.HasValue)
                Loop = loop.Value;

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
