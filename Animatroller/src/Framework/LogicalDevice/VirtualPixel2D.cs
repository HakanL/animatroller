using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class VirtualPixel2D : SingleOwnerDevice, IPixel2D
    {
        protected object lockObject = new object();
        protected int pixelWidth;
        protected int pixelHeight;

        protected Subject<Color[,]> showBuffer;
        protected double[,] brightness;
        protected Color[,] color;
        protected ControlSubject<double> globalBrightness;

        public VirtualPixel2D(int width, int height, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height");

            this.showBuffer = new Subject<Color[,]>();
            this.globalBrightness = new ControlSubject<double>(1.0);
            this.pixelWidth = width;
            this.pixelHeight = height;

            this.brightness = new double[width, height];
            this.color = new Color[width, height];
        }

        public IObserver<double> GlobalBrightnessControl
        {
            get { return this.globalBrightness.AsObserver(); }
        }

        public double GlobalBrightness
        {
            get { return this.globalBrightness.Value; }
            set
            {
                this.globalBrightness.OnNext(value);

                ShowBuffer();
            }
        }

        public void ShowBuffer()
        {
            var output = new Color[PixelWidth, PixelHeight];

            for (int x = 0; x < PixelWidth; x++)
                for (int y = 0; y < PixelHeight; y++)
                {
                    var hsv = new HSV(this.color[x, y]);
                    hsv.Value = hsv.Value * this.brightness[x, y] * this.globalBrightness.Value;

                    output[x, y] = hsv.Color;
                }

            this.showBuffer.OnNext(output);
        }

        public VirtualPixel2D SetAll(Color color, double brightness)
        {
            for (int x = 0; x < this.pixelWidth; x++)
                for (int y = 0; y < this.pixelHeight; y++)
                {
                    this.brightness[x, y] = brightness;
                    this.color[x, y] = color;
                }

            ShowBuffer();

            return this;
        }

        public IObservable<Color[,]> Output
        {
            get { return this.showBuffer.AsObservable(); }
        }

        public void SetRGB(byte[] array, int arrayOffset, int arrayLength, int pixelOffset, bool raiseChangeEvent = true)
        {
            int pixel = pixelOffset;

            for (int i = 0; i < arrayLength; i += 3)
            {
                if (pixel >= this.color.Length)
                    break;

                if (arrayOffset + i + 2 >= array.Length)
                    break;

                byte r = array[arrayOffset + i];
                byte g = array[arrayOffset + i + 1];
                byte b = array[arrayOffset + i + 2];

                //                this.brightness[pixel] = 1.0;
                //                this.color[pixel] = Color.FromArgb(r, g, b);

                pixel++;
            }

            if (raiseChangeEvent)
                ShowBuffer();
        }

        protected override void UpdateOutput()
        {
            ShowBuffer();
        }

        public int PixelWidth
        {
            get { return this.pixelWidth; }
        }

        public int PixelHeight
        {
            get { return this.pixelHeight; }
        }

        public void SetPixel(int x, int y, Color color, double brightness = 1.0)
        {
            this.color[x, y] = color;
            this.brightness[x, y] = brightness;
        }

        public override void SaveState(Dictionary<string, object> state)
        {
            //FIXME
            throw new NotImplementedException();
            var copyColor = new Color[this.color.Length];
            //            Buffer.BlockCopy(this.color, 0, copyColor, 0, this.color.Length * sizeof(Color));

            //            return Tuple.Create(Brightness, copyColor);
        }

        public override void RestoreState(Dictionary<string, object> state)
        {
//            var stateData = (Tuple<double, Color>)state;

//            SetColor(stateData.Item2, stateData.Item1);
        }
    }
}
