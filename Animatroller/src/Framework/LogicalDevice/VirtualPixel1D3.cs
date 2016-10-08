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
                object value;
                if (x.TryGetValue(DataElements.Color, out value))
                {
                    Color allColor = (Color)value;

                    if (allColor != Color.Transparent)
                    {
                        var bitmap = (Bitmap)x[DataElements.PixelBitmap];
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.FillRectangle(new SolidBrush(allColor), 0, 0, bitmap.Width, bitmap.Height);
                        }
                    }
                }

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
                var bitmap = (Bitmap)this.currentData[DataElements.PixelBitmap];
                float brightness = (float)(double)this.currentData[DataElements.Brightness];

                float whiteout = (float)Executor.Current.Whiteout.Value;
                float blackout = (float)Executor.Current.Blackout.Value;

                //TODO: Optimize
                this.brightnessMatrix.Matrix40 = whiteout;
                this.brightnessMatrix.Matrix41 = whiteout;
                this.brightnessMatrix.Matrix42 = whiteout;

                using (var imageAttributes = new ImageAttributes())
                {
                    imageAttributes.SetColorMatrix(this.brightnessMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    this.output.DrawImage(bitmap,
                        this.outputRectangle,
                        0,
                        0,
                        bitmap.Width,
                        bitmap.Height,
                        GraphicsUnit.Pixel,
                        imageAttributes);
                }

                this.brightnessMatrix.Matrix40 = -1 + brightness - blackout;
                this.brightnessMatrix.Matrix41 = -1 + brightness - blackout;
                this.brightnessMatrix.Matrix42 = -1 + brightness - blackout;

                using (var imageAttributes = new ImageAttributes())
                {
                    imageAttributes.SetColorMatrix(this.brightnessMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    this.output.DrawImage(this.outputBitmap,
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
            get { return (double)this.currentData[DataElements.Brightness]; }
        }

        public void SetBrightness(double brightness, IControlToken token = null)
        {
            SetData(token, Utils.AdditionalData(DataElements.Brightness, brightness));
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

        public void SetColor(Color color, double? brightness = 1.0, IControlToken token = null)
        {
            SetColorRange(color, brightness, 0, null, token);
        }

        public void SetColorRange(
            Color color,
            double? brightness = 1.0,
            int startPosition = 0,
            int? length = null,
            IControlToken token = null)
        {
            IData data = GetFrameBuffer(token, this);

            Color injectColor;
            if (brightness.GetValueOrDefault(1.0) < 1.0)
                injectColor = GetColorFromColorAndBrightness(color, brightness.Value);
            else
                injectColor = color;

            if (!length.HasValue)
                length = this.pixelCount;
            else
            {
                if (length.Value > this.pixelCount)
                    length = this.pixelCount;
            }

            var bitmap = (Bitmap)data[DataElements.PixelBitmap];
            for (int i = 0; i < length.Value; i++)
                bitmap.SetPixel(startPosition + i, 0, injectColor);

            PushOutput(token);
        }

        public void Inject(Color color, double brightness = 1.0, IControlToken token = null)
        {
            IData data = GetFrameBuffer(token, this);
            var bitmap = (Bitmap)data[DataElements.PixelBitmap];

            Color injectColor = GetColorFromColorAndBrightness(color, brightness);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImageUnscaled(bitmap, 1, 0);

                bitmap.SetPixel(0, 0, injectColor);
            }

            PushOutput(token);
        }

        public void InjectRev(Color color, double brightness, IControlToken token = null)
        {
            IData data = GetFrameBuffer(token, this);
            var bitmap = (Bitmap)data[DataElements.PixelBitmap];

            Color injectColor = GetColorFromColorAndBrightness(color, brightness);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImageUnscaled(bitmap, -1, 0);

                bitmap.SetPixel(bitmap.Width - 1, 0, injectColor);
            }

            PushOutput(token);
        }
    }
}
