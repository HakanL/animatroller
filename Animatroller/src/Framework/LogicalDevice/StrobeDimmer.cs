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
    public class StrobeDimmer : Dimmer, IStrobe
    {
        protected double strobeSpeed;

        public event EventHandler<StrobeSpeedChangedEventArgs> StrobeSpeedChanged;

        public StrobeDimmer(string name)
            : base(name)
        {
        }

        protected virtual void RaiseStrobeSpeedChanged()
        {
            var handler = StrobeSpeedChanged;
            if (handler != null)
                handler(this, new StrobeSpeedChangedEventArgs(this.StrobeSpeed));
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

        public virtual IStrobe SetStrobe(double speed)
        {
            return SetStrobe(speed, 1.0);
        }

        public virtual IStrobe SetStrobe(double speed, double brightness)
        {
            this.StrobeSpeed = speed;
            this.Brightness = brightness;

            return this;
        }

        public override Dimmer TurnOff()
        {
            StopStrobe();

            return base.TurnOff();
        }

        public virtual IStrobe StopStrobe()
        {
            this.StrobeSpeed = 0;
            
            return this;
        }

        public override void SetInitialState()
        {
            base.SetInitialState();
            RaiseStrobeSpeedChanged();
        }
    }
}
