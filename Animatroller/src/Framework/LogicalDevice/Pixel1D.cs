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
    public class Pixel1D : IOutput, ILogicalDevice, IHasBrightnessControl
    {
        protected string name;
        protected IOwner owner;
        protected int pixelCount;

        protected double[] brightness;
        protected Color[] color;

        public event EventHandler<PixelChangedEventArgs> PixelChanged;
        public event EventHandler<MultiPixelChangedEventArgs> MultiPixelChanged;

        public Pixel1D(string name, int pixels)
        {
            this.name = name;
            if (pixels <= 0)
                throw new ArgumentOutOfRangeException("pixels");

            this.pixelCount = pixels;

            this.brightness = new double[pixels];
            this.color = new Color[pixels];

            Executor.Current.Register(this);
        }

        public int Pixels
        {
            get { return pixelCount; }
        }

        protected void RaisePixelChanged(int channel)
        {
            var handler = PixelChanged;
            if (handler != null)
                handler(this, new PixelChangedEventArgs(channel, this.color[channel], this.brightness[channel]));
        }

        protected void RaiseMultiPixelChanged(int startChannel, int size)
        {
            var handler = MultiPixelChanged;
            if (handler != null)
            {
                var newValues = new ColorBrightness[size];
                for (int i = 0; i < size; i++)
                {
                    newValues[i] = new ColorBrightness(this.color[i + startChannel], this.brightness[i + startChannel]);
                }

                handler(this, new MultiPixelChangedEventArgs(startChannel, newValues));
            }
        }

        public void StartDevice()
        {
            RaiseMultiPixelChanged(0, Pixels);
        }

        public string Name
        {
            get { return this.name; }
        }

        protected void CheckBounds(int channel)
        {
            if (channel < 0 || channel >= Pixels)
                throw new ArgumentOutOfRangeException("channel");
        }

        public virtual Pixel1D SetColor(int channel, Color color, double brightness)
        {
            CheckBounds(channel);

            this.color[channel] = color;
            this.brightness[channel] = brightness;

            RaisePixelChanged(channel);

            return this;
        }

        public virtual Pixel1D SetColor(int channel, Color color)
        {
            CheckBounds(channel);

            this.color[channel] = color;
            this.brightness[channel] = 1.0;

            RaisePixelChanged(channel);

            return this;
        }

        public virtual Pixel1D SetColors(int startChannel, ColorBrightness[] colorBrightness)
        {
            int? firstChannel = null;
            int lastChannel = 0;
            for (int i = 0; i < colorBrightness.Length; i++)
            {
                if (i + startChannel < 0)
                    continue;
                if (i + startChannel >= Pixels)
                    continue;

                if (!firstChannel.HasValue)
                    firstChannel = i + startChannel;
                lastChannel = i + startChannel;

                this.color[i + startChannel] = colorBrightness[i].Color;
                this.brightness[i + startChannel] = colorBrightness[i].Brightness;
            }

            if (firstChannel.HasValue)
                RaiseMultiPixelChanged(firstChannel.Value, lastChannel - firstChannel.Value + 1);

            return this;
        }

        public Pixel1D FadeToUsingHSV(int channel, Color color, double brightness, TimeSpan duration)
        {
            if (channel < 0 || channel >= this.pixelCount)
                throw new ArgumentOutOfRangeException();

            if (this.color[channel].GetBrightness() == 0)
            {
                this.color[channel] = color;
                this.brightness[channel] = 0;
            }

            if (color.GetBrightness() == 0)
            {
                color = this.color[channel];
                brightness = 0;
            }

            var startHSV = new HSV(this.color[channel]);
            var endHSV = new HSV(color);
            double startBrightness = this.brightness[channel];

            // 10 steps per second
            int steps = (int)(duration.TotalMilliseconds / 100);

            double position = 0;
            for (int i = 0; i < steps; i++)
            {
                double newBrightness = startBrightness + (brightness - startBrightness) * position;

                double hue = startHSV.Hue + (endHSV.Hue - startHSV.Hue) * position;
                double sat = startHSV.Saturation + (endHSV.Saturation - startHSV.Saturation) * position;
                double val = startHSV.Value + (endHSV.Value - startHSV.Value) * position;
                Color newColor = HSV.ColorFromHSV(hue, sat, val);

                SetColor(channel, newColor, newBrightness);

                System.Threading.Thread.Sleep(100);

                position += 1.0 / (steps - 1);
            }

            return this;
        }

        public Pixel1D FadeTo(int channel, Color color, double brightness, TimeSpan duration)
        {
            if (channel < 0 || channel >= this.pixelCount)
                throw new ArgumentOutOfRangeException();

            if (this.color[channel].GetBrightness() == 0)
            {
                this.color[channel] = color;
                this.brightness[channel] = 0;
            }

            if (color.GetBrightness() == 0)
            {
                color = this.color[channel];
                brightness = 0;
            }

            var startColor = this.color[channel];
            var endColor = color;

            double startBrightness = this.brightness[channel];

            // 10 steps per second
            int steps = (int)(duration.TotalMilliseconds / 100);

            double position = 0;
            for (int i = 0; i < steps; i++)
            {
                double newBrightness = startBrightness + (brightness - startBrightness) * position;

                int r = (int)(startColor.R + (endColor.R - startColor.R) * position);
                int g = (int)(startColor.G + (endColor.G - startColor.G) * position);
                int b = (int)(startColor.B + (endColor.B - startColor.B) * position);
                Color newColor = Color.FromArgb(r, g, b);

                SetColor(channel, newColor, newBrightness);

                System.Threading.Thread.Sleep(100);

                position += 1.0 / (steps - 1);
            }

            return this;
        }

        public Pixel1D FadeTo(ColorBrightness[] values, TimeSpan duration)
        {
            if (values.Length != this.pixelCount)
                throw new ArgumentOutOfRangeException("Invalid ColorBrightness length");

            var startColors = new Color[this.pixelCount];
            var startBrightness = new double[this.pixelCount];

            for (int chn = 0; chn < this.pixelCount; chn++)
            {
                if (this.color[chn].GetBrightness() == 0)
                {
                    this.color[chn] = values[chn].Color;
                    this.brightness[chn] = 0;
                }

                if (values[chn].Color.GetBrightness() == 0)
                {
                    values[chn].Color = this.color[chn];
                    values[chn].Brightness = 0;
                }

                startColors[chn] = this.color[chn];
                startBrightness[chn] = this.brightness[chn];
            }

            // 10 steps per second
            int steps = (int)(duration.TotalMilliseconds / 100);

            for (int i = 1; i < steps; i++)
            {
                double position = 1.0 * i / (steps - 1);

                for (int chn = 0; chn < values.Length; chn++)
                {
                    double newBrightness = startBrightness[chn] + (values[chn].Brightness - startBrightness[chn]) * position;

                    int r = (int)(startColors[chn].R + (values[chn].Color.R - startColors[chn].R) * position);
                    int g = (int)(startColors[chn].G + (values[chn].Color.G - startColors[chn].G) * position);
                    int b = (int)(startColors[chn].B + (values[chn].Color.B - startColors[chn].B) * position);
                    Color newColor = Color.FromArgb(r, g, b);

                    this.color[chn] = newColor;
                    this.brightness[chn] = newBrightness;
                }

                RaiseMultiPixelChanged(0, values.Length);

                System.Threading.Thread.Sleep(100);
            }

            return this;
        }

        //IDEA: Fade to new array of ColorBrightness

        public Pixel1D Inject(Color color, double brightness)
        {
            for (int i = this.brightness.Length - 1; i > 0; i--)
            {
                this.brightness[i] = this.brightness[i - 1];
                this.color[i] = this.color[i - 1];
            }

            this.brightness[0] = brightness;
            this.color[0] = color;

            RaiseMultiPixelChanged(0, this.brightness.Length);

            return this;
        }

        public Pixel1D InjectRev(Color color, double brightness)
        {
            for (int i = 0; i < this.brightness.Length - 1; i++)
            {
                this.brightness[i] = this.brightness[i + 1];
                this.color[i] = this.color[i + 1];
            }

            this.brightness[this.brightness.Length - 1] = brightness;
            this.color[this.brightness.Length - 1] = color;

            RaiseMultiPixelChanged(0, this.brightness.Length);

            return this;
        }

        public Pixel1D InjectWithFade(Color color, double brightness, TimeSpan duration)
        {
            var newValues = new ColorBrightness[this.pixelCount];
            for (int i = this.brightness.Length - 1; i > 0; i--)
            {
                newValues[i] = new ColorBrightness(this.color[i - 1], this.brightness[i - 1]);
            }

            newValues[0] = new ColorBrightness(color, brightness);

            FadeTo(newValues, duration);

            return this;
        }

        public Pixel1D TurnOff()
        {
            for (int i = 0; i < this.brightness.Length; i++)
            {
                this.brightness[i] = 0;
            }

            RaiseMultiPixelChanged(0, this.brightness.Length);

            return this;
        }

        public Pixel1D SetAll(Color color, double brightness)
        {
            for (int i = 0; i < this.brightness.Length; i++)
            {
                this.brightness[i] = brightness;
            }

            for (int i = 0; i < this.color.Length; i++)
            {
                this.color[i] = color;
            }

            RaiseMultiPixelChanged(0, this.brightness.Length);

            return this;
        }

        public Pixel1D SetAllOnlyColor(Color color)
        {
            for (int i = 0; i < this.color.Length; i++)
            {
                this.color[i] = color;
            }

            RaiseMultiPixelChanged(0, this.brightness.Length);

            return this;
        }

        public double Brightness
        {
            set
            {
                if (value == 0)
                    // Reset owner
                    owner = null;

                for (int i = 0; i < this.brightness.Length; i++)
                {
                    this.brightness[i] = value;
                }

                RaiseMultiPixelChanged(0, this.brightness.Length);
            }
        }

        public void SetBrightness(double value, IOwner owner)
        {
            if (this.owner != null && owner != this.owner)
            {
                if (owner != null)
                {
                    if (owner.Priority <= this.owner.Priority)
                        return;
                }
                else
                    return;
            }

            this.owner = owner;
            this.Brightness = value;
        }
    }
}
