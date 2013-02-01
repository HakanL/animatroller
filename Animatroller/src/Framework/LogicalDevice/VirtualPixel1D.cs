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
    public class VirtualPixel1D : IPixel1D, IOutput, ILogicalDevice, IHasBrightnessControl, IOwner
    {
        protected object lockObject = new object();
        protected string name;
        protected IOwner owner;
        protected int pixelCount;

        protected List<PixelDevice> devices;
        protected double[] brightness;
        protected Color[] color;
        protected Effect.MasterSweeper.Job effectJob;

        // Master events, primarily used by simulator
        public event EventHandler<PixelChangedEventArgs> PixelChanged;
        public event EventHandler<MultiPixelChangedEventArgs> MultiPixelChanged;


        public VirtualPixel1D(string name, int pixels)
        {
            this.name = name;
            if (pixels <= 0)
                throw new ArgumentOutOfRangeException("pixels");

            this.pixelCount = pixels;

            this.devices = new List<PixelDevice>();
            this.brightness = new double[pixels];
            this.color = new Color[pixels];

            Executor.Current.Register(this);
        }

        public VirtualPixel1D AddPixelDevice(int startVirtualPosition, int positions, EventHandler<PixelChangedEventArgs> pixelChanged,
            EventHandler<MultiPixelChangedEventArgs> multiPixelChanged)
        {
            var newPixelDevice = new PixelDevice
            {
                PixelChanged = pixelChanged,
                MultiPixelChanged = multiPixelChanged,
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

        protected void RaisePixelChanged(int position)
        {
            var handler = PixelChanged;
            if(handler != null)
                handler(this, new PixelChangedEventArgs(position, this.color[position], this.brightness[position]));

            foreach (var pixelDevice in this.devices)
            {
                if (pixelDevice.IsPositionForThisDevice(position))
                {
                    handler = pixelDevice.PixelChanged;
                    if (handler != null)
                        handler(this, new PixelChangedEventArgs(position - pixelDevice.StartPosition, this.color[position], this.brightness[position]));
                }
            }
        }

        protected void RaiseMultiPixelChanged(int startPosition, int size)
        {
            var handler = MultiPixelChanged;
            if (handler != null)
            {
                var newValues = new List<ColorBrightness>();
                for (int i = 0; i < size; i++)
                {
                    int position = i + startPosition;
                    newValues.Add(new ColorBrightness(this.color[position], this.brightness[position]));
                }
                handler(this, new MultiPixelChangedEventArgs(startPosition, newValues.ToArray()));
            }

            foreach (var pixelDevice in this.devices)
            {
                int? firstPosition = null;
                var newValues = new List<ColorBrightness>();
                for (int i = 0; i < size; i++)
                {
                    int position = i + startPosition;
                    if (pixelDevice.IsPositionForThisDevice(position))
                    {
                        if(!firstPosition.HasValue)
                            firstPosition = position - pixelDevice.StartPosition;

                        newValues.Add(new ColorBrightness(this.color[position], this.brightness[position]));
                    }
                }

                if (newValues.Any())
                {
                    handler = pixelDevice.MultiPixelChanged;
                    if (handler != null)
                        handler(this, new MultiPixelChangedEventArgs(firstPosition.Value, newValues.ToArray()));
                }
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

        protected void CheckBounds(int position)
        {
            if (position < 0 || position >= Pixels)
                throw new ArgumentOutOfRangeException("position");
        }

        public virtual VirtualPixel1D SetColor(int position, Color color, double brightness)
        {
            CheckBounds(position);

            this.color[position] = color;
            this.brightness[position] = brightness;

            RaisePixelChanged(position);

            return this;
        }

        public virtual VirtualPixel1D SetColor(int position, Color color)
        {
            CheckBounds(position);

            this.color[position] = color;
            this.brightness[position] = 1.0;

            RaisePixelChanged(position);

            return this;
        }

        public virtual VirtualPixel1D SetColors(int startPosition, ColorBrightness[] colorBrightness)
        {
            int? firstPosition = null;
            int lastPosition = 0;
            for (int i = 0; i < colorBrightness.Length; i++)
            {
                if (i + startPosition < 0)
                    continue;
                if (i + startPosition >= Pixels)
                    continue;

                if (!firstPosition.HasValue)
                    firstPosition = i + startPosition;
                lastPosition = i + startPosition;

                this.color[i + startPosition] = colorBrightness[i].Color;
                this.brightness[i + startPosition] = colorBrightness[i].Brightness;
            }

            if (firstPosition.HasValue)
                RaiseMultiPixelChanged(firstPosition.Value, lastPosition - firstPosition.Value + 1);

            return this;
        }

        public VirtualPixel1D FadeToUsingHSV(int position, Color color, double brightness, TimeSpan duration)
        {
            if (position < 0 || position >= this.pixelCount)
                throw new ArgumentOutOfRangeException();

            if (this.color[position].GetBrightness() == 0)
            {
                this.color[position] = color;
                this.brightness[position] = 0;
            }

            if (color.GetBrightness() == 0)
            {
                color = this.color[position];
                brightness = 0;
            }

            var startHSV = new HSV(this.color[position]);
            var endHSV = new HSV(color);
            double startBrightness = this.brightness[position];

            // 10 steps per second
            int steps = (int)(duration.TotalMilliseconds / 100);

            double fadePosition = 0;
            for (int i = 0; i < steps; i++)
            {
                double newBrightness = startBrightness + (brightness - startBrightness) * fadePosition;

                double hue = startHSV.Hue + (endHSV.Hue - startHSV.Hue) * fadePosition;
                double sat = startHSV.Saturation + (endHSV.Saturation - startHSV.Saturation) * fadePosition;
                double val = startHSV.Value + (endHSV.Value - startHSV.Value) * fadePosition;
                Color newColor = HSV.ColorFromHSV(hue, sat, val);

                SetColor(position, newColor, newBrightness);

                System.Threading.Thread.Sleep(100);

                fadePosition += 1.0 / (steps - 1);
            }

            return this;
        }

        public VirtualPixel1D FadeTo(int position, Color color, double brightness, TimeSpan duration)
        {
            if (position < 0 || position >= this.pixelCount)
                throw new ArgumentOutOfRangeException();

            if (this.color[position].GetBrightness() == 0)
            {
                this.color[position] = color;
                this.brightness[position] = 0;
            }

            if (color.GetBrightness() == 0)
            {
                color = this.color[position];
                brightness = 0;
            }

            var startColor = this.color[position];
            var endColor = color;

            double startBrightness = this.brightness[position];

            // 10 steps per second
            int steps = (int)(duration.TotalMilliseconds / 100);

            double fadePosition = 0;
            for (int i = 0; i < steps; i++)
            {
                double newBrightness = startBrightness + (brightness - startBrightness) * fadePosition;

                int r = (int)(startColor.R + (endColor.R - startColor.R) * fadePosition);
                int g = (int)(startColor.G + (endColor.G - startColor.G) * fadePosition);
                int b = (int)(startColor.B + (endColor.B - startColor.B) * fadePosition);
                Color newColor = Color.FromArgb(r, g, b);

                SetColor(position, newColor, newBrightness);

                System.Threading.Thread.Sleep(100);

                fadePosition += 1.0 / (steps - 1);
            }

            return this;
        }

        public VirtualPixel1D FadeTo(ColorBrightness[] values, TimeSpan duration)
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
                double fadePosition = 1.0 * i / (steps - 1);

                for (int chn = 0; chn < values.Length; chn++)
                {
                    double newBrightness = startBrightness[chn] + (values[chn].Brightness - startBrightness[chn]) * fadePosition;

                    int r = (int)(startColors[chn].R + (values[chn].Color.R - startColors[chn].R) * fadePosition);
                    int g = (int)(startColors[chn].G + (values[chn].Color.G - startColors[chn].G) * fadePosition);
                    int b = (int)(startColors[chn].B + (values[chn].Color.B - startColors[chn].B) * fadePosition);
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

        public VirtualPixel1D Inject(Color color, double brightness)
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

        public VirtualPixel1D InjectRev(Color color, double brightness)
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

        public VirtualPixel1D InjectWithFade(Color color, double brightness, TimeSpan duration)
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

        public VirtualPixel1D TurnOff()
        {
            for (int i = 0; i < this.brightness.Length; i++)
            {
                this.brightness[i] = 0;
            }

            RaiseMultiPixelChanged(0, this.brightness.Length);

            return this;
        }

        public VirtualPixel1D SetAll(Color color, double brightness)
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

        public VirtualPixel1D SetAllOnlyColor(Color color)
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

        protected class PixelDevice
        {
            public EventHandler<PixelChangedEventArgs> PixelChanged { get; set; }
            public EventHandler<MultiPixelChangedEventArgs> MultiPixelChanged { get; set; }
            public int StartPosition { get; set; }
            public int EndPosition { get; set; }

            public bool IsPositionForThisDevice(int position)
            {
                return position >= this.StartPosition && position <= this.EndPosition;
            }
        }

        public Effect.MasterSweeper.Job RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration)
        {
            var effectAction = effect.GetEffectAction(brightness =>
            {
                this.SetBrightness(brightness, this);
            });

            lock (this.lockObject)
            {
                if (this.effectJob == null)
                {
                    // Create new
                    this.effectJob = Executor.Current.RegisterSweeperJob(effectAction, oneSweepDuration, effect.OneShot);
                }
                else
                {
                    this.effectJob.Reset(effectAction, oneSweepDuration, effect.OneShot);
                }
                this.effectJob.Restart();
            }

            return this.effectJob;
        }

        public void StopEffect()
        {
            if (this.effectJob != null)
                this.effectJob.Stop();
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
