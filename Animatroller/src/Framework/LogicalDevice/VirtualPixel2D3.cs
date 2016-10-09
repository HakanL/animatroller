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
    public class VirtualPixel2D3 : SingleOwnerDevice, IPixel2D2, IApiVersion3, IReceivesBrightness
    {
        protected int pixelWidth;
        protected int pixelHeight;
        protected List<PixelDevice> devices;
        private Bitmap outputBitmap;
        private Graphics output;
        private Rectangle outputRectangle;
        private ColorMatrix brightnessMatrix;
        private Subject<Bitmap> imageChanged;
        private byte[] tempPixels;

        public VirtualPixel2D3(int pixelWidth, int pixelHeight, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            if (pixelWidth <= 0 || pixelHeight <= 0)
                throw new ArgumentOutOfRangeException("pixelWidth/pixelHeight");

            this.pixelWidth = pixelWidth;
            this.pixelHeight = pixelHeight;

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

            int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(this.outputBitmap.PixelFormat) / 8;
            int stride = 4 * ((this.outputBitmap.Width * bytesPerPixel + 3) / 4);
            int byteCount = stride * this.outputBitmap.Height;
            this.tempPixels = new byte[byteCount];

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
                    {
                        g.FillRectangle(new SolidBrush(allColor), 0, 0, bitmap.Width, bitmap.Height);
                    }
                }

                data.Remove(DataElements.Color);
            }

            return base.PreprocessPushData(data);
        }

        public int PixelWidth
        {
            get { return this.pixelWidth; }
        }

        public int PixelHeight
        {
            get { return this.pixelHeight; }
        }

        private Bitmap GetBitmap()
        {
            return new Bitmap(this.pixelWidth, this.pixelHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
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

                BitmapData bitmapData = this.outputBitmap.LockBits(this.outputRectangle, ImageLockMode.ReadOnly, this.outputBitmap.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, this.tempPixels, 0, this.tempPixels.Length);
                this.outputBitmap.UnlockBits(bitmapData);

                foreach (var pixelDevice in this.devices)
                {
                    pixelDevice.DrawImage(this.tempPixels);
                }

                this.imageChanged.OnNext(this.outputBitmap);
            }
        }

        public void AddPixelDevice(Dictionary<int, Utility.PixelMap[]> pixelMapping, Action<byte[]> pixelsChanged)
        {
            var newPixelDevice = new PixelDevice(this.pixelWidth, this.pixelHeight, pixelMapping, pixelsChanged);

            this.devices.Add(newPixelDevice);
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

            private int[] pixelMapping;

            private int stride;

            public PixelDevice(int pixelWidth, int pixelHeight, Dictionary<int, Utility.PixelMap[]> pixelMapping, Action<byte[]> pixelsChangedAction)
            {
                this.pixelsChangedAction = pixelsChangedAction;

                this.outputBitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format24bppRgb);
                this.outputGraphics = Graphics.FromImage(this.outputBitmap);
                this.outputRectangle = new Rectangle(0, 0, pixelWidth, pixelHeight);

                int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(this.outputBitmap.PixelFormat) / 8;
                this.stride = 4 * ((this.outputBitmap.Width * bytesPerPixel + 3) / 4);
                int byteCount = stride * this.outputBitmap.Height;
                this.pixels = new byte[bytesPerPixel * this.outputBitmap.Width * this.outputBitmap.Height];
                this.pixelMapping = new int[byteCount];

                UpdatePixelMapping(pixelMapping);
            }

            private void UpdatePixelMapping(Dictionary<int, Utility.PixelMap[]> input)
            {
                for (int i = 0; i < this.pixelMapping.Length; i++)
                    this.pixelMapping[i] = -1;

                if (!input.Any())
                    return;

                int minUniverse = input.Min(x => x.Key);
                int maxUniverse = input.Max(x => x.Key);

                for (int universe = minUniverse; universe <= maxUniverse; universe++)
                {
                    Utility.PixelMap[] mapping;
                    if (!input.TryGetValue(universe, out mapping))
                        continue;

                    for (int i = 0; i < mapping.Length; i++)
                    {
                        Utility.PixelMap map = mapping[i];

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

                        int sourcePos = map.Y * this.stride + map.X * 3 + rgbOffset;
                        if (sourcePos >= 0 && sourcePos <= this.pixelMapping.Length)
                            this.pixelMapping[sourcePos] = i + (universe - minUniverse) * 510;
                    }
                }
            }

            public void DrawImage(byte[] rawPixels)
            {
                if (this.pixelsChangedAction != null)
                {
                    if (this.pixelMapping.Length != rawPixels.Length)
                        // Incorrect pixel mapping
                        return;

                    for (int i = 0; i < rawPixels.Length; i++)
                    {
                        int pos = this.pixelMapping[i];
                        if (pos >= 0 && pos < this.pixels.Length)
                            this.pixels[pos] = rawPixels[i];
                    }

                    this.pixelsChangedAction(this.pixels);
                }
            }
        }

        public void SetColor(Color color, double? brightness = 1.0, IControlToken token = null)
        {
            SetColorRange(color, brightness, token: token);
        }

        public void SetColorRange(
            Color color,
            double? brightness = 1.0,
            int startX = 0,
            int startY = 0,
            int? width = null,
            int? height = null,
            IControlToken token = null)
        {
            IData data = GetFrameBuffer(token, this);

            Color injectColor;
            if (brightness.GetValueOrDefault(1.0) < 1.0)
                injectColor = GetColorFromColorAndBrightness(color, brightness.Value);
            else
                injectColor = color;

            if (!width.HasValue)
                width = this.pixelWidth;
            if (!height.HasValue)
                height = this.pixelHeight;

            var bitmap = (Bitmap)data[DataElements.PixelBitmap];

            using (var g = Graphics.FromImage(bitmap))
            using (var b = new SolidBrush(injectColor))
            {
                g.FillRectangle(b, startX, startY, width.Value, height.Value);
            }

            PushOutput(token);
        }

        [Obsolete("Just for testing, not a very useful function")]
        public void Inject(Color color, double brightness = 1.0, IControlToken token = null)
        {
            IData data = GetFrameBuffer(token, this);
            var bitmap = (Bitmap)data[DataElements.PixelBitmap];

            Color injectColor = GetColorFromColorAndBrightness(color, brightness);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImageUnscaled(bitmap, 1, 0);

                for (int y = 0; y < bitmap.Height; y++)
                    bitmap.SetPixel(0, y, injectColor);
            }

            PushOutput(token);
        }
    }
}
