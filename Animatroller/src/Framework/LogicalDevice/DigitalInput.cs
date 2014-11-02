using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class DigitalInput : BaseDevice
    {
        protected bool active;
        protected string instanceKey;

        public event EventHandler<StateChangedEventArgs> ActiveChanged;

        public DigitalInput([System.Runtime.CompilerServices.CallerMemberName] string name = "", bool persistState = false)
            : base(name)
        {
            if (persistState)
                instanceKey = name.GetHashCode().ToString() + "_";
            else
                instanceKey = null;

            if (instanceKey != null)
                bool.TryParse(Executor.Current.GetKey(instanceKey + "input", false.ToString()), out active);
        }

        protected virtual void RaiseActiveChanged()
        {
            var handler = ActiveChanged;
            if (handler != null)
            {
                var task = new Task(() =>
                    {
                        handler.Invoke(this, new StateChangedEventArgs(this.Active));
                    });
                task.Start();
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

                    UpdateOutput();

                    if (instanceKey != null)
                        Executor.Current.SetKey(this.instanceKey + "input", this.active.ToString());
                }
            }
        }

        protected override void UpdateOutput()
        {
            RaiseActiveChanged();
        }
    }
}
