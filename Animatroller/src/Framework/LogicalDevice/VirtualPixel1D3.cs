using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;
using PhysicalDevice = Animatroller.Framework.PhysicalDevice;
using System.Drawing.Imaging;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Animatroller.Framework.LogicalDevice
{
    public class VirtualPixel1D3 : SingleOwnerDevice, IPixel1D2, IApiVersion3, IReceivesBrightness, IReceivesColor
    {
        protected int pixelCount;
        protected List<PixelDevice> devices;
        private Bitmap outputBitmap;
        private Graphics output;
        private Rectangle outputRectangle;
        private ColorMatrix brightnessMatrix;
        private Subject<Bitmap> imageChanged;

        public VirtualPixel1D3(int pixels, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            if (pixels <= 0)
                throw new ArgumentOutOfRangeException("pixels");

            this.pixelCount = pixels;

            this.devices = new List<PixelDevice>();

            this.outputData.Subscribe(x =>
            {
                Output();
            });

            Executor.Current.Blackout.Subscribe(_ => Output());
            Executor.Current.Whiteout.Subscribe(_ => Output());

            this.imageChanged = new Subject<Bitmap>();
            this.outputBitmap = GetBitmap();
            this.output = Graphics.FromImage(this.outputBitmap);
            this.outputRectangle = new Rectangle(0, 0, this.outputBitmap.Width, this.outputBitmap.Height);

            this.brightnessMatrix = new ColorMatrix(new float[][]{
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0.5f, 0.5f, 0.5f, 1, 1}
                });
        }

        protected override IData PreprocessPushData(IData data)
        {
            object value;
            if (data.TryGetValue(DataElements.Color, out value))
            {
                Color allColor = (Color)value;

                if (allColor != Color.Transparent)
                {
                    var bitmap = (Bitmap)data[DataElements.PixelBitmap];
                    using (var g = Graphics.FromImage(bitmap))
                    using (var allColorBrush = new SolidBrush(allColor))
                    {
                        g.FillRectangle(allColorBrush, 0, 0, bitmap.Width, bitmap.Height);
                    }
                }

                data.Remove(DataElements.Color);
            }

            return base.PreprocessPushData(data);
        }

        public int Pixels
        {
            get { return this.pixelCount; }
        }

        private Bitmap GetBitmap()
        {
            return new Bitmap(this.pixelCount, 1, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        }

        public IObservable<Bitmap> ImageChanged
        {
            get { return this.imageChanged.AsObservable(); }
        }

        public override void BuildDefaultData(IData data)
        {
            data[DataElements.PixelBitmap] = GetBitmap();
            data[DataElements.Brightness] = 1.0;
        }

        protected void Output()
        {
            lock (this.lockObject)
            {
                var bitmap = GetCurrentData<Bitmap>(DataElements.PixelBitmap);
                float brightness = (float)GetCurrentData<double>(DataElements.Brightness);

                float whiteout = (float)Executor.Current.Whiteout.Value;
                float blackout = (float)Executor.Current.Blackout.Value;

                //TODO: Optimize
                this.brightnessMatrix.Matrix00 = 1;
                this.brightnessMatrix.Matrix11 = 1;
                this.brightnessMatrix.Matrix22 = 1;
                this.brightnessMatrix.Matrix40 = whiteout;
                this.brightnessMatrix.Matrix41 = whiteout;
                this.brightnessMatrix.Matrix42 = whiteout;

                using (var imageAttributes = new ImageAttributes())
                {
                    imageAttributes.SetColorMatrix(this.brightnessMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    this.output.DrawImage(
                        bitmap,
                        this.outputRectangle,
                        0,
                        0,
                        bitmap.Width,
                        bitmap.Height,
                        GraphicsUnit.Pixel,
                        imageAttributes);
                }

                this.brightnessMatrix.Matrix40 = 0;
                this.brightnessMatrix.Matrix41 = 0;
                this.brightnessMatrix.Matrix42 = 0;
                this.brightnessMatrix.Matrix00 = brightness - blackout;
                this.brightnessMatrix.Matrix11 = brightness - blackout;
                this.brightnessMatrix.Matrix22 = brightness - blackout;

                using (var imageAttributes = new ImageAttributes())
                {
                    imageAttributes.SetColorMatrix(this.brightnessMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    this.output.DrawImage(
                        this.outputBitmap,
                        this.outputRectangle,
                        0,
                        0,
                        bitmap.Width,
                        bitmap.Height,
                        GraphicsUnit.Pixel,
                        imageAttributes);
                }

                foreach (var pixelDevice in this.devices)
                {
                    pixelDevice.DrawImage(this.outputBitmap);
                }

                this.imageChanged.OnNext(this.outputBitmap);
            }
        }

        public VirtualPixel1D3 AddPixelDevice(int startVirtualPosition, int positions, bool reverse, Action<byte[]> pixelsChanged)
        {
            var newPixelDevice = new PixelDevice(this.pixelCount, startVirtualPosition, startVirtualPosition + positions - 1, reverse, pixelsChanged);

            this.devices.Add(newPixelDevice);

            return this;
        }

        public double Brightness
        {
            get { return GetCurrentData<double>(DataElements.Brightness); }
        }

        public void SetBrightness(double brightness, int channel = 0, IControlToken token = null)
        {
            this.SetData(channel, token, Utils.Data(DataElements.Brightness, brightness));
        }

        private Color GetColorFromColorAndBrightness(Color input, double brightness)
        {
            var hsv = new HSV(input);

            // Adjust brightness
            double adjustedValue = hsv.Value * brightness;

            hsv.Value = adjustedValue.Limit(0, 1);

            return hsv.Color;
        }

        protected class PixelDevice
        {
            private Bitmap outputBitmap;

            private Graphics outputGraphics;

            private Rectangle outputRectangle;

            private byte[] pixels;

            private Action<byte[]> pixelsChangedAction;

            private bool reverse;

            public int StartPosition { get; }

            public PixelDevice(int pixelCount, int startPosition, int endPosition, bool reverse, Action<byte[]> pixelsChangedAction)
            {
                this.reverse = reverse;
                this.pixelsChangedAction = pixelsChangedAction;

                StartPosition = startPosition;

                this.outputBitmap = new Bitmap(endPosition - startPosition + 1, 1, PixelFormat.Format24bppRgb);
                this.outputGraphics = Graphics.FromImage(this.outputBitmap);
                this.outputRectangle = new Rectangle(0, 0, endPosition - startPosition + 1, 1);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(this.outputBitmap.PixelFormat) / 8;
                this.pixels = new byte[this.outputRectangle.Width * bytesPerPixel];
            }

            public void DrawImage(Bitmap bitmap)
            {
                if (this.pixelsChangedAction != null)
                {
                    int stride;
                    lock (this)
                    {
                        this.outputGraphics.DrawImageUnscaled(bitmap, -StartPosition, 0);
                        if (this.reverse)
                            this.outputBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

                        BitmapData bitmapData = this.outputBitmap.LockBits(this.outputRectangle, ImageLockMode.ReadOnly, this.outputBitmap.PixelFormat);
                        stride = bitmapData.Stride;
                        System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
                        this.outputBitmap.UnlockBits(bitmapData);
                    }

                    // Shift little/big-endian
                    if (BitConverter.IsLittleEndian)
                    {
                        for (int y = 0; y < this.outputRectangle.Height; y++)
                        {
                            for (int x = 0; x < this.outputRectangle.Width; x++)
                            {
                                int bytePos = y * stride + x * 3;
                                byte b1 = this.pixels[bytePos + 2];
                                this.pixels[bytePos + 2] = this.pixels[bytePos];
                                this.pixels[bytePos] = b1;
                            }
                        }
                    }

                    this.pixelsChangedAction(this.pixels);
                }
            }
        }

        public Color Color
        {
            get
            {
                return Color.Transparent;
            }
        }

        public void SetColor(Color color, double? brightness = 1.0, int channel = 0, IControlToken token = null)
        {
            SetColorRange(color, brightness, 0, null, channel, token);
        }

        public void SetColorRange(
            Color color,
            double? brightness = 1.0,
            int startPosition = 0,
            int? length = null,
            int channel = 0,
            IControlToken token = null)
        {
            IData data = GetFrameBuffer(channel, token, this);

            Color injectColor;
            if (brightness.GetValueOrDefault(1.0) < 1.0)
                injectColor = GetColorFromColorAndBrightness(color, brightness.Value);
            else
                injectColor = color;

            if (!length.HasValue)
                length = this.pixelCount;
            else
            {
                if (startPosition + length.Value > this.pixelCount)
                    length = this.pixelCount - startPosition;
            }

            var bitmap = (Bitmap)data[DataElements.PixelBitmap];

            lock (this.lockObject)
            {
                using (var g = Graphics.FromImage(bitmap))
                using (var b = new SolidBrush(injectColor))
                {
                    g.FillRectangle(b, startPosition, 0, length.Value, 1);
                }
            }

            PushOutput(channel, token);
        }

        public void Inject(Color color, double brightness = 1.0, int channel = 0, IControlToken token = null)
        {
            IData data = GetFrameBuffer(channel, token, this);
            var bitmap = (Bitmap)data[DataElements.PixelBitmap];

            Color injectColor = GetColorFromColorAndBrightness(color, brightness);

            lock (this.lockObject)
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.DrawImageUnscaled(bitmap, 1, 0);

                    bitmap.SetPixel(0, 0, injectColor);
                }
            }

            PushOutput(channel, token);
        }

        public void InjectRev(Color color, double brightness, int channel = 0, IControlToken token = null)
        {
            IData data = GetFrameBuffer(channel, token, this);
            var bitmap = (Bitmap)data[DataElements.PixelBitmap];

            Color injectColor = GetColorFromColorAndBrightness(color, brightness);

            lock (this.lockObject)
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.DrawImageUnscaled(bitmap, -1, 0);

                    bitmap.SetPixel(bitmap.Width - 1, 0, injectColor);
                }
            }

            PushOutput(channel, token);
        }

        public Task Chaser(IData[] data, int speed, int channel = 0, IControlToken token = null)
        {
            return Executor.Current.MasterEffect.CustomJob(
                jobAction: pos =>
                {
                    var current = data[pos % data.Length];

                    double brightness = current.GetValue<double>(DataElements.Brightness) ?? 1.0;
                    Color color = current.GetValue<Color>(DataElements.Color) ?? Color.White;

                    Inject(color, brightness, channel, token);
                },
                jobStopped: () =>
                {
                    SetBrightness(0, channel, token);
                },
                speed: speed);
        }
    }
}
