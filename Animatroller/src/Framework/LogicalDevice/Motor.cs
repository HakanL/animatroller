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
    public class Motor : ILogicalDevice
    {
        protected string name;
        protected double speed;

        public event EventHandler<SpeedChangedEventArgs> SpeedChanged;

        public Motor(string name)
        {
            this.name = name;
            Executor.Current.Register(this);
        }

        protected virtual void RaiseSpeedChanged()
        {
            var handler = SpeedChanged;
            if (handler != null)
                handler(this, new SpeedChangedEventArgs(this.Speed));
        }

        public double Speed
        {
            get { return this.speed; }
            set
            {
                if (this.speed != value)
                {
                    this.speed = value.Limit(-1, 1);

                    RaiseSpeedChanged();
                }
            }
        }

        public virtual Motor SetSpeed(double value)
        {
            this.Speed = value;

            return this;
        }

        public virtual Motor TurnOff()
        {
            this.Speed = 0;
            
            return this;
        }

        public virtual void SetInitialState()
        {
            RaiseSpeedChanged();
        }

        public string Name
        {
            get { return this.name; }
        }
    }
}
