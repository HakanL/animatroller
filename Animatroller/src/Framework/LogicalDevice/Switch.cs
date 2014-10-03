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
    public class Switch : BaseDevice, IOutput
    {
        protected bool power;

        public event EventHandler<StateChangedEventArgs> PowerChanged;

        public Switch([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        protected virtual void RaisePowerChanged()
        {
            var handler = PowerChanged;
            if (handler != null)
                handler(this, new StateChangedEventArgs(this.Power));
        }

        public Switch Follow(OperatingHours source)
        {
            source.OpenHoursChanged += (o, e) =>
            {
                if (e.IsOpenNow)
                    this.SetPower(true);
                else
                    this.TurnOff();
            };

            return this;
        }

        public bool Power
        {
            get { return this.power; }
            set
            {
                if (this.power != value)
                {
                    this.power = value;

                    RaisePowerChanged();
                }
            }
        }

        public virtual Switch SetPower(bool value)
        {
            this.Power = value;

            return this;
        }

        public virtual Switch TurnOff()
        {
            this.Power = false;

            return this;
        }

        public override void StartDevice()
        {
            RaisePowerChanged();
        }
    }
}
