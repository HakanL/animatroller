using System;
using System.Buffers;
using System.Collections.Concurrent;
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
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Import
{
    public class DmxPlayback2 : ICanExecute, IDisposable
    {
        public readonly static double TicksPerMs = Stopwatch.Frequency / 1000.0;

        public class DeviceData
        {
            public IReceivesData Device { get; set; }

            public List<(int Address, int OutputPos)> Mapping { get; set; }

            public int BufferSize { get; set; }

            public Action<byte[]> Writer;
        }

        private string name;
        private Subroutine sub;
        private IFileReader3 reader;
        private Stopwatch masterClock;
        private bool loop;
        private IChannel channel;
        private List<DeviceData> devices;
        private (int UniverseId, int FSeqChannel)[] layout;
        private List<(IReceivesData Device, int StartUniverseInFile, int StartChannelInFile, Dictionary<int, Utility.PixelMap[]> RawMapping, int Width, int Height)> rawDeviceMapping;
        private bool initializeCalled;
        private ConcurrentQueue<(long, Dictionary<DeviceData, byte[]>)> frameQueue;
        private ManualResetEvent frameConsumed;
        private ArrayPool<byte> arrayPool;

        public DmxPlayback2(IChannel channel = null, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;
            this.channel = channel;
            this.sub = new Subroutine("SUB_" + this.name);
            this.masterClock = new Stopwatch();
            this.devices = new List<DeviceData>();
            this.rawDeviceMapping = new List<(IReceivesData Device, int StartUniverseInFile, int StartChannelInFile, Dictionary<int, Utility.PixelMap[]> RawMapping, int Width, int Height)>();
            this.frameQueue = new ConcurrentQueue<(long, Dictionary<DeviceData, byte[]>)>();
            this.frameConsumed = new ManualResetEvent(false);
            this.arrayPool = ArrayPool<byte>.Shared;

            Executor.Current.MasterTimer25ms.Output.Subscribe(_ =>
            {
                long elapsedMs = this.masterClock.ElapsedMilliseconds;

                if (!this.frameQueue.TryPeek(out (long Timestamp, Dictionary<DeviceData, byte[]> Devices) value))
                    return;

                if (value.Timestamp > elapsedMs)
                    return;

                if (!this.frameQueue.TryDequeue(out value))
                    return;
                if (value.Devices == null && value.Timestamp == -1)
                {
                    // Restart, Loop
                    this.masterClock.Restart();
                    if (!this.frameQueue.TryDequeue(out value))
                        return;
                }

                this.frameConsumed.Set();
                OutputData(value.Devices);
            });

            this.sub.RunAction(ins =>
            {
                if (!this.initializeCalled)
                    Init();

                this.masterClock.Start();
                bool finished = false;

                do
                {
                    while (!finished && this.frameQueue.Count < 5)
                    {
                        var frame = this.reader.ReadFullFrame(out long timestamp);
                        if (frame != null)
                        {
                            // Build rgb values

                            var frameData = new Dictionary<DeviceData, byte[]>();
                            foreach (var deviceData in devices)
                            {
                                if (deviceData.Writer == null || deviceData.BufferSize == 0)
                                    continue;

                                var frameBuffer = this.arrayPool.Rent(deviceData.BufferSize);

                                foreach (var (inputPos, outputPos) in deviceData.Mapping)
                                {
                                    frameBuffer[outputPos] = frame[inputPos];
                                }

                                frameData.Add(deviceData, frameBuffer);
                            }

                            this.frameQueue.Enqueue((timestamp, frameData));
                        }
                        else
                        {
                            if (this.loop)
                            {
                                this.reader.Rewind();
                                this.frameQueue.Enqueue((-1, null));
                            }
                            else
                            {
                                finished = true;
                                break;
                            }
                        }
                    }

                    this.frameConsumed.WaitOne();
                    this.frameConsumed.Reset();

                    if (finished && this.frameQueue.Count == 0)
                        // Done
                        break;

                } while (!ins.IsCancellationRequested);

                // Empty queue
                while (this.frameQueue.TryDequeue(out var dummy)) ;

                this.masterClock.Stop();
            });
        }

        private void OutputData(Dictionary<DeviceData, byte[]> renderDevices)
        {
            foreach (var kvp in renderDevices)
            {
                var deviceData = kvp.Key;

                deviceData.Writer(kvp.Value);

                this.arrayPool.Return(kvp.Value);

                deviceData.Device.PushOutput(this.channel, this.sub.Token);
            }
        }

        private void Init()
        {
            foreach (var deviceData in this.devices)
            {
                if (deviceData.Device is IPixel2)
                {
                    var bitmap = (Bitmap)deviceData.Device.GetFrameBuffer(this.channel, this.sub.Token, deviceData.Device)[DataElements.PixelBitmap];
                    var bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                    int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                    int stride = 4 * ((bitmap.Width * bytesPerPixel + 3) / 4);
                    int byteCount = stride * bitmap.Height;
                    deviceData.BufferSize = byteCount;

                    deviceData.Writer = buf =>
                    {
                        BitmapData bitmapData = bitmap.LockBits(bitmapRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

                        System.Runtime.InteropServices.Marshal.Copy(buf, 0, bitmapData.Scan0, deviceData.BufferSize);

                        bitmap.UnlockBits(bitmapData);
                    };
                }
                else if (deviceData.Device is IReceivesColor)
                {
                    deviceData.BufferSize = 3;

                    var pusher = deviceData.Device.GetDataObserver(this.channel, this.sub.Token);

                    deviceData.Writer = buf =>
                    {
                        var color = Color.FromArgb(buf[0], buf[1], buf[2]);
                        pusher.Data[DataElements.Brightness] = 1.0;
                        pusher.Data[DataElements.Color] = color;
                    };
                }
                else if (deviceData.Device is IReceivesBrightness)
                {
                    deviceData.BufferSize = 1;

                    var pusher = deviceData.Device.GetDataObserver(this.channel, this.sub.Token);

                    deviceData.Writer = buf =>
                    {
                        pusher.Data[DataElements.Brightness] = buf[0].GetDouble();
                    };
                }
                else
                    throw new ArgumentException("Unknown device type");
            }

            this.masterClock.Reset();

            this.initializeCalled = true;
        }

        public void SetOutput(IPixel1D2 device, Dictionary<int, Utility.PixelMap[]> mapping, int startUniverseInFile, int startChannelInFile)
        {
            this.rawDeviceMapping.Add((device, startUniverseInFile, startChannelInFile, mapping, device.Pixels, 1));

            this.sub.LockWhenRunning(device);
        }

        public void SetOutput(IPixel2D2 device, Dictionary<int, Utility.PixelMap[]> mapping, int startUniverseInFile, int startChannelInFile)
        {
            this.rawDeviceMapping.Add((device, startUniverseInFile, startChannelInFile, mapping, device.PixelWidth, device.PixelHeight));

            this.sub.LockWhenRunning(device);
        }

        public void SetOutput(IReceivesColor device, int startUniverseInFile, int startChannelInFile)
        {
            var mapping = new Dictionary<int, Utility.PixelMap[]>
                        {
                            {0, new Utility.PixelMap[] {
                                new Utility.PixelMap { ColorComponent = Utility.ColorComponent.B },
                                new Utility.PixelMap { ColorComponent = Utility.ColorComponent.G },
                                new Utility.PixelMap { ColorComponent = Utility.ColorComponent.R }
                            } }
                        };

            this.rawDeviceMapping.Add((device, startUniverseInFile, startChannelInFile, mapping, 1, 1));

            this.sub.LockWhenRunning(device);
        }

        public void SetOutput(IReceivesBrightness device, int startUniverseInFile, int startChannelInFile)
        {
            var mapping = new Dictionary<int, Utility.PixelMap[]>
                        {
                            {0, new Utility.PixelMap[] {
                                new Utility.PixelMap { ColorComponent = Utility.ColorComponent.Brightness }
                            } }
                        };

            this.rawDeviceMapping.Add((device, startUniverseInFile, startChannelInFile, mapping, 1, 1));

            this.sub.LockWhenRunning(device);
        }

        private void UpdatePixelMapping(Dictionary<int, Utility.PixelMap[]> inputMapping, int startUniverse, int startChannel, int width, int height, Dictionary<int, int[]> pixelMapping)
        {
            int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
            int stride = 4 * ((width * bytesPerPixel + 3) / 4);

            foreach (var kvp in inputMapping)
            {
                if (!pixelMapping.TryGetValue(kvp.Key + startUniverse, out int[] channelMapping))
                {
                    channelMapping = new int[512];
                    for (int i = 0; i < channelMapping.Length; i++)
                        channelMapping[i] = -1;

                    pixelMapping.Add(kvp.Key + startUniverse, channelMapping);
                }

                for (int pos = 0; pos < Math.Min(kvp.Value.Length, channelMapping.Length); pos++)
                {
                    var map = kvp.Value[pos];

                    if (map.X >= width || map.Y >= height)
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

                        case Utility.ColorComponent.Brightness:
                            rgbOffset = 0;
                            break;

                        default:
                            continue;
                    }

                    int bytePos = map.X * 3 + map.Y * stride + rgbOffset;

                    if (bytePos >= stride * height)
                        throw new ArgumentOutOfRangeException("Invalid pixel mapping");

                    channelMapping[pos + startChannel - 1] = bytePos;
                }
            }
        }

        public bool IsMultiInstance => false;

        public string Name => this.name;

        public bool Loop
        {
            get { return this.loop; }
            set { this.loop = value; }
        }

        public void Execute(CancellationToken cancelToken)
        {
            this.sub.Execute(cancelToken);
        }

        public void Load(IFileReader3 reader)
        {
            Stop();

            this.reader = reader;
            this.reader.Rewind();

            this.layout = reader.GetFrameLayout();
            this.devices.Clear();
            this.initializeCalled = false;

            if (this.layout.Length != this.reader.FrameSize)
                throw new ArgumentException("Invalid layout size, has to match frame size");

            foreach (var group in this.rawDeviceMapping.GroupBy(x => x.Device))
            {
                var pixelMapping = new Dictionary<int, int[]>();

                foreach (var rawDeviceData in group)
                {
                    UpdatePixelMapping(rawDeviceData.RawMapping, rawDeviceData.StartUniverseInFile, rawDeviceData.StartChannelInFile, rawDeviceData.Width, rawDeviceData.Height, pixelMapping);
                }

                var mappingList = new List<(int Address, int OutputPos)>();

                foreach (var (item, i) in this.layout.Select((v, i) => (v, i)))
                {
                    if (!pixelMapping.TryGetValue(item.UniverseId, out int[] channelMapping))
                        continue;

                    int outputPos = channelMapping[item.FSeqChannel];
                    if (outputPos >= 0)
                        mappingList.Add((i, outputPos));
                }

                if (mappingList.Any())
                    this.devices.Add(new DeviceData
                    {
                        Device = group.Key,
                        Mapping = mappingList
                    });
            }
        }

        public void Dispose()
        {
            // Trigger
            this.frameConsumed.Set();
        }

        public Task Run(bool? loop = null)
        {
            if (loop.HasValue)
                Loop = loop.Value;

            Executor.Current.Execute(this, out Task task);

            return task;
        }

        public Task Run(out CancellationTokenSource cts, bool? loop = null)
        {
            if (loop.HasValue)
                Loop = loop.Value;

            cts = Executor.Current.Execute(this, out Task task);

            return task;
        }

        public void Stop()
        {
            Executor.Current.Cancel(this);
        }
    }
}
