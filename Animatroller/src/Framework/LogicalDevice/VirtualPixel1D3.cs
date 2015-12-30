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


                /*
                                BitmapData bitmapData = this.outputBitmap.LockBits(this.outputRectangle, ImageLockMode.ReadWrite, this.outputBitmap.PixelFormat);
                                int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(this.outputBitmap.PixelFormat) / 8;
                                int byteCount = bitmapData.Stride * this.outputBitmap.Height;
                                byte[] pixels = new byte[byteCount];
                                IntPtr ptrFirstPixel = bitmapData.Scan0;
                                System.Runtime.InteropServices.Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
                                this.outputBitmap.UnlockBits(bitmapData);

                                int heightInPixels = bitmapData.Height;
                                int widthInBytes = bitmapData.Width * bytesPerPixel;

                                foreach (var pixelDevice in this.devices)
                                {
                                    for (int y = 0; y < heightInPixels; y++)
                                    {
                                        int currentLine = y * bitmapData.Stride;
                                        for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                                        {
                                            int mappedPos = pixelDevice.PosMap[x];
                                            if (mappedPos > -1)
                                            {

                                            }

                                            int OldBlue = pixels[currentLine + x];
                                            int OldGreen = pixels[currentLine + x + 1];
                                            int OldRed = pixels[currentLine + x + 2];
                                            //// Transform blue and clip to 255:
                                            //pixels[CurrentLine + x] = (byte)((OldBlue + BlueMagnitudeToAdd > 255) ? 255 : OldBlue + BlueMagnitudeToAdd);
                                            //// Transform green and clip to 255:
                                            //pixels[CurrentLine + x + 1] = (byte)((OldGreen + GreenMagnitudeToAdd > 255) ? 255 : OldGreen + GreenMagnitudeToAdd);
                                            //// Transform red and clip to 255:
                                            //pixels[CurrentLine + x + 2] = (byte)((OldRed + RedMagnitudeToAdd > 255) ? 255 : OldRed + RedMagnitudeToAdd);
                                        }
                                    }
                                }*/

                //Color[] currentColorArray = (Color[])this.currentData[DataElements.PixelColor];
                //double[] currentBrightnessArray = (double[])this.currentData[DataElements.PixelBrightness];

                //foreach (var pixelDevice in this.devices)
                //{
                //    int? firstPosition = null;
                //    var newValues = new List<Color>();
                //    for (int i = 0; i < this.pixelCount; i++)
                //    {
                //        if (pixelDevice.IsPositionForThisDevice(i))
                //        {
                //            if (!firstPosition.HasValue)
                //                firstPosition = i - pixelDevice.StartPosition;

                //            newValues.Add(PhysicalDevice.BaseLight.GetColorFromColorBrightness(currentColorArray[i], currentBrightnessArray[i]));
                //        }
                //    }

                //    if (newValues.Any())
                //    {
                //        pixelDevice.PixelsChangedAction(newValues.ToArray());
                //    }
                //}
            }
        }

        public VirtualPixel1D3 AddPixelDevice(int startVirtualPosition, int positions, Action<byte[], int> pixelsChanged)
        {
            var newPixelDevice = new PixelDevice(this.pixelCount, startVirtualPosition, startVirtualPosition + positions - 1, pixelsChanged);

            this.devices.Add(newPixelDevice);

            return this;
        }

        public VirtualPixel1D3 AddImageDevice(Action<Bitmap> imageChangedAction)
        {
            this.devices.Add(new PixelDevice(imageChangedAction));

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

            //public int[] PosMap { get; set; }

            private Action<byte[], int> pixelsChangedAction;

            private Action<Bitmap> imageChangedAction;

            public int StartPosition { get; }

            //public int EndPosition { get; }

            public PixelDevice(int pixelCount, int startPosition, int endPosition, Action<byte[], int> pixelsChangedAction)
            {
                this.pixelsChangedAction = pixelsChangedAction;
                //                RgbMap = new byte[pixelCount * 3];

                //PosMap = new int[pixelCount * 3];
                //for (int i = 0; i < pixelCount * 3; i++)
                //    PosMap[i] = -1;

                //for (int i = startPosition, j = 0; i < endPosition; i++)
                //{
                //    if (i < 0 || i >= pixelCount)
                //        continue;

                //    PosMap[i * 3 + 0] = j++;
                //    PosMap[i * 3 + 1] = j++;
                //    PosMap[i * 3 + 2] = j++;
                //}

                this.outputBitmap = new Bitmap(endPosition - startPosition + 1, 1, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                this.outputGraphics = Graphics.FromImage(this.outputBitmap);
                this.outputRectangle = new Rectangle(0, 0, endPosition - startPosition + 1, 1);


                int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(this.outputBitmap.PixelFormat) / 8;
                int stride = 4 * ((this.outputBitmap.Width * bytesPerPixel + 3) / 4);
                int byteCount = stride * this.outputBitmap.Height;
                this.pixels = new byte[byteCount];
            }

            public PixelDevice(Action<Bitmap> imageChangedAction)
            {
                this.imageChangedAction = imageChangedAction;
            }

            public void DrawImage(Bitmap bitmap)
            {
                if (this.pixelsChangedAction != null)
                {
                    lock (this)
                    {
                        this.outputGraphics.DrawImageUnscaled(bitmap, StartPosition, 0);

                        BitmapData bitmapData = this.outputBitmap.LockBits(this.outputRectangle, ImageLockMode.ReadWrite, this.outputBitmap.PixelFormat);
                        System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
                        this.outputBitmap.UnlockBits(bitmapData);
                    }

                    this.pixelsChangedAction(this.pixels, this.outputBitmap.Width * 3);
                }

                if (this.imageChangedAction != null)
                    this.imageChangedAction(bitmap);
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
