using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class VirtualPixel1D2 : SingleOwnerDevice, IPixel1D2, IApiVersion3, IReceivesBrightness, IReceivesColor
    {
        protected int pixelCount;
        protected List<PixelDevice> devices;

        public VirtualPixel1D2(int pixels, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            if (pixels <= 0)
                throw new ArgumentOutOfRangeException("pixels");

            this.pixelCount = pixels;

            this.devices = new List<PixelDevice>();

            this.currentData[DataElements.PixelBrightness] = InitArray(this.pixelCount, 0.0);
            this.currentData[DataElements.PixelColor] = InitArray(this.pixelCount, Color.White);

            this.outputData.Subscribe(x =>
            {
                Output();
            });

            Executor.Current.Blackout.Subscribe(_ => Output());
            Executor.Current.Whiteout.Subscribe(_ => Output());
        }

        private T[] InitArray<T>(int size, T initValue)
        {
            var data = new T[size];

            for (int i = 0; i < size; i++)
                data[i] = initValue;

            return data;
        }

        public VirtualPixel1D2 AddPixelDevice(int startVirtualPosition, int positions, Action<Color[]> pixelsChanged)
        {
            var newPixelDevice = new PixelDevice
            {
                PixelsChangedAction = pixelsChanged,
                StartPosition = startVirtualPosition,
                EndPosition = startVirtualPosition + positions - 1
            };

            this.devices.Add(newPixelDevice);

            return this;
        }

        public int Pixels
        {
            get { return pixelCount; }
        }

        //protected void RaisePixelChanged(int position)
        //{
        //    var handler = PixelChanged;
        //    if (handler != null)
        //        handler(this, new PixelChangedEventArgs(position, this.color[position], this.brightness[position]));

        //    foreach (var pixelDevice in this.devices)
        //    {
        //        if (pixelDevice.IsPositionForThisDevice(position))
        //        {
        //            handler = pixelDevice.PixelChanged;
        //            if (handler != null)
        //                handler(this, new PixelChangedEventArgs(position - pixelDevice.StartPosition, this.color[position], this.brightness[position]));
        //        }
        //    }
        //}

        //protected void RaiseMultiPixelChanged(int startPosition, int size)
        //{
        //    var handler = MultiPixelChanged;
        //    if (handler != null)
        //    {
        //        var newValues = new List<ColorBrightness>();
        //        for (int i = 0; i < size; i++)
        //        {
        //            int position = i + startPosition;
        //            newValues.Add(new ColorBrightness(this.color[position], this.brightness[position]));
        //        }
        //        handler(this, new MultiPixelChangedEventArgs(startPosition, newValues.ToArray()));
        //    }

        //    foreach (var pixelDevice in this.devices)
        //    {
        //        int? firstPosition = null;
        //        var newValues = new List<ColorBrightness>();
        //        for (int i = 0; i < size; i++)
        //        {
        //            int position = i + startPosition;
        //            if (pixelDevice.IsPositionForThisDevice(position))
        //            {
        //                if (!firstPosition.HasValue)
        //                    firstPosition = position - pixelDevice.StartPosition;

        //                newValues.Add(new ColorBrightness(this.color[position], this.brightness[position]));
        //            }
        //        }

        //        if (newValues.Any())
        //        {
        //            handler = pixelDevice.MultiPixelChanged;
        //            if (handler != null)
        //                handler(this, new MultiPixelChangedEventArgs(firstPosition.Value, newValues.ToArray()));
        //        }
        //    }
        //}

        protected void CheckBounds(int position)
        {
            if (position < 0 || position >= Pixels)
                throw new ArgumentOutOfRangeException("position");
        }

        private Color[] ColorArray
        {
            get { return (Color[])this.currentData[DataElements.PixelColor]; }
        }

        private double[] BrightnessArray
        {
            get { return (double[])this.currentData[DataElements.PixelBrightness]; }
        }

        public virtual VirtualPixel1D2 SetColor(int position, Color color, double brightness)
        {
            CheckBounds(position);

            ColorArray[position] = color;
            BrightnessArray[position] = brightness;

            UpdateOutput();

            return this;
        }

        public virtual ColorBrightness GetColorBrightness(int position)
        {
            CheckBounds(position);

            return new ColorBrightness(ColorArray[position], BrightnessArray[position]);
        }

        public double Brightness
        {
            get { return double.NaN; }
        }

        public virtual VirtualPixel1D2 SetBrightness(int position, double brightness)
        {
            CheckBounds(position);

            BrightnessArray[position] = brightness;

            UpdateOutput();

            return this;
        }

        public virtual VirtualPixel1D2 SetBrightness(int startPosition, int length, double brightness)
        {
            CheckBounds(startPosition);
            CheckBounds(startPosition + length - 1);

            for (int i = 0; i < length; i++)
                BrightnessArray[startPosition + i] = brightness;

            UpdateOutput();

            return this;
        }

        public virtual VirtualPixel1D2 SetBrightness(int startPosition, int length, Color color, double? brightness = 1.0)
        {
            CheckBounds(startPosition);
            CheckBounds(startPosition + length - 1);

            for (int i = 0; i < length; i++)
            {
                ColorArray[startPosition + i] = color;
                if (brightness.HasValue)
                    BrightnessArray[startPosition + i] = brightness.Value;
            }

            UpdateOutput();

            return this;
        }

        public virtual VirtualPixel1D2 SetColor(int position, Color color, double? brightness = 1.0)
        {
            CheckBounds(position);

            ColorArray[position] = color;
            if (brightness.HasValue)
                BrightnessArray[position] = brightness.Value;

            UpdateOutput();

            return this;
        }

        protected void Output()
        {
            var colorArray = ColorArray;
            var brightnessArray = BrightnessArray;

            foreach (var pixelDevice in this.devices)
            {
                int? firstPosition = null;
                var newValues = new List<Color>();
                for (int i = 0; i < this.pixelCount; i++)
                {
                    if (pixelDevice.IsPositionForThisDevice(i))
                    {
                        if (!firstPosition.HasValue)
                            firstPosition = i - pixelDevice.StartPosition;

                        newValues.Add(PhysicalDevice.BaseLight.GetColorFromColorBrightness(colorArray[i], brightnessArray[i]));
                    }
                }

                if (newValues.Any())
                {
                    pixelDevice.PixelsChangedAction(newValues.ToArray());
                }
            }
        }

        //public virtual VirtualPixel1D2 SetColors(int startPosition, ColorBrightness[] colorBrightness)
        //{
        //    int? firstPosition = null;
        //    int lastPosition = 0;
        //    for (int i = 0; i < colorBrightness.Length; i++)
        //    {
        //        if (i + startPosition < 0)
        //            continue;
        //        if (i + startPosition >= Pixels)
        //            continue;

        //        if (!firstPosition.HasValue)
        //            firstPosition = i + startPosition;
        //        lastPosition = i + startPosition;

        //        this.color[i + startPosition] = colorBrightness[i].Color;
        //        this.brightness[i + startPosition] = colorBrightness[i].Brightness;
        //    }

        //    //if (firstPosition.HasValue)
        //    //    RaiseMultiPixelChanged(firstPosition.Value, lastPosition - firstPosition.Value + 1);

        //    return this;
        //}

        //public VirtualPixel1D2 Inject(ColorBrightness colorBrightness)
        //{
        //    Inject(colorBrightness.Color, colorBrightness.Brightness);

        //    return this;
        //}

        //public VirtualPixel1D2 Inject(Color color, double brightness)
        //{
        //    for (int i = this.brightness.Length - 1; i > 0; i--)
        //    {
        //        this.brightness[i] = this.brightness[i - 1];
        //        this.color[i] = this.color[i - 1];
        //    }

        //    this.brightness[0] = brightness;
        //    this.color[0] = color;

        //    //RaiseMultiPixelChanged(0, this.brightness.Length);

        //    return this;
        //}

        //public VirtualPixel1D2 InjectRev(Color color, double brightness)
        //{
        //    for (int i = 0; i < this.brightness.Length - 1; i++)
        //    {
        //        this.brightness[i] = this.brightness[i + 1];
        //        this.color[i] = this.color[i + 1];
        //    }

        //    this.brightness[this.brightness.Length - 1] = brightness;
        //    this.color[this.brightness.Length - 1] = color;

        //    //RaiseMultiPixelChanged(0, this.brightness.Length);

        //    return this;
        //}

        protected override IData PreprocessPushData(IData data)
        {
            data = base.PreprocessPushData(data);

            var newData = new Data();

            foreach (var kvp in data)
            {
                switch (kvp.Key)
                {
                    case DataElements.Brightness:
                        // Set pixel brightness instead
                        var pixelBrightness = new double[this.pixelCount];
                        for (int i = 0; i < pixelBrightness.Length; i++)
                            pixelBrightness[i] = (double)kvp.Value;

                        newData[DataElements.PixelBrightness] = pixelBrightness;
                        break;

                    case DataElements.Color:
                        // Set pixel color instead
                        var pixelColor = new Color[this.pixelCount];
                        for (int i = 0; i < pixelColor.Length; i++)
                            pixelColor[i] = (Color)kvp.Value;

                        newData[DataElements.PixelColor] = pixelColor;
                        break;

                    default:
                        newData[kvp.Key] = kvp.Value;
                        break;
                }
            }

            return newData;
        }

        public void SetBrightness(double brightness, IControlToken token = null)
        {
            var pixelBrightness = new double[this.pixelCount];
            for (int i = 0; i < pixelBrightness.Length; i++)
                pixelBrightness[i] = brightness;

            PushData(DataElements.PixelBrightness, pixelBrightness, token);
        }

        protected class PixelDevice
        {
            public Action<Color[]> PixelsChangedAction { get; set; }

            public int StartPosition { get; set; }

            public int EndPosition { get; set; }

            public bool IsPositionForThisDevice(int position)
            {
                return position >= this.StartPosition && position <= this.EndPosition;
            }
        }

        public Color Color
        {
            get
            {
                return Color.Transparent;
            }
        }

        //public void SetRGB(byte[] array, int arrayOffset, int arrayLength, int pixelOffset, bool raiseChangeEvent = true)
        //{
        //    int pixel = pixelOffset;

        //    for (int i = 0; i < arrayLength; i += 3)
        //    {
        //        if (pixel >= this.color.Length)
        //            break;

        //        if (arrayOffset + i + 2 >= array.Length)
        //            break;

        //        byte r = array[arrayOffset + i];
        //        byte g = array[arrayOffset + i + 1];
        //        byte b = array[arrayOffset + i + 2];

        //        this.brightness[pixel] = 1.0;
        //        this.color[pixel] = Color.FromArgb(r, g, b);

        //        pixel++;
        //    }

        //    //if (raiseChangeEvent)
        //    //    RaiseMultiPixelChanged(pixelOffset, pixel - pixelOffset);
        //}

        public void SetColor(Color color, double? brightness = 1.0, IControlToken token = null)
        {
            var data = new Data();

            var pixelColor = new Color[this.pixelCount];
            for (int i = 0; i < pixelColor.Length; i++)
                pixelColor[i] = color;
            data.Add(DataElements.PixelColor, pixelColor);

            if (brightness.HasValue)
            {
                var pixelBrightness = new double[this.pixelCount];
                for (int i = 0; i < pixelBrightness.Length; i++)
                    pixelBrightness[i] = brightness.Value;

                data.Add(DataElements.PixelBrightness, pixelBrightness);
            }

            PushData(token, data);
        }
    }
}
