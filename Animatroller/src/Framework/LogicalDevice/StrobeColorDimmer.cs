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
    public class StrobeColorDimmer : ColorDimmer, IStrobe
    {
        protected double strobeSpeed;

        public event EventHandler<StrobeSpeedChangedEventArgs> StrobeSpeedChanged;

        public StrobeColorDimmer(string name)
            : base(name)
        {
        }

        protected virtual void RaiseStrobeSpeedChanged()
        {
            var handler = StrobeSpeedChanged;
            if (handler != null)
                handler(this, new StrobeSpeedChangedEventArgs(this.StrobeSpeed));
        }

        public override void StartDevice()
        {
            base.StartDevice();
            RaiseStrobeSpeedChanged();
        }

        public double StrobeSpeed
        {
            get { return this.strobeSpeed; }
            set
            {
                if (this.strobeSpeed != value)
                {
                    this.strobeSpeed = value.Limit(0, 1);

                    RaiseStrobeSpeedChanged();
                }
            }
        }

        public virtual IStrobe SetStrobe(double value)
        {
            this.StrobeSpeed = value;

            return this;
        }

        public virtual IStrobe SetStrobe(double speed, double brightness)
        {
            this.StrobeSpeed = speed;
            this.Brightness = brightness;

            return this;
        }

        public virtual IStrobe SetStrobe(double speed, Color color, double brightness)
        {
            this.StrobeSpeed = speed;
            this.Brightness = brightness;
            this.Color = color;

            return this;
        }

        public virtual IStrobe SetStrobe(double speed, Color color)
        {
            return SetStrobe(speed, color, 1.0);
        }

        public virtual IStrobe StopStrobe()
        {
            this.StrobeSpeed = 0;

            return this;
        }

        public override Dimmer TurnOff()
        {
            StopStrobe();

            return base.TurnOff();
        }

    }
}
