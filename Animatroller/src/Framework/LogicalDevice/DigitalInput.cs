using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class DigitalInput : ILogicalDevice
    {
        protected string name;
        protected bool active;

        public event EventHandler<StateChangedEventArgs> ActiveChanged;

        public DigitalInput(string name)
        {
            this.name = name;
            Executor.Current.Register(this);
        }

        protected virtual void RaiseActiveChanged()
        {
            var handler = ActiveChanged;
            if (handler != null)
            {
                var task = Task.Run(() =>
                    {
                        handler.Invoke(this, new StateChangedEventArgs(this.Active));
                    });
            }
        }

        public void Trigger(bool value)
        {
            this.Active = value;
        }

        public bool Active
        {
            get { return this.active; }
            private set
            {
                if (this.active != value)
                {
                    this.active = value;

                    RaiseActiveChanged();
                }
            }
        }

        public void StartDevice()
        {
            RaiseActiveChanged();
        }

        public string Name
        {
            get { return this.name; }
        }
    }
}
