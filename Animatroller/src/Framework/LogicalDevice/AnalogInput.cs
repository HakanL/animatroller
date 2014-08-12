using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class AnalogInput : ILogicalDevice
    {
        protected string name;
        protected double value;
        protected string instanceKey;

        public event EventHandler<BrightnessChangedEventArgs> ValueChanged;

        public AnalogInput(string name, bool persistState = false)
        {
            this.name = name;
            Executor.Current.Register(this);

            if (persistState)
                instanceKey = name.GetHashCode().ToString() + "_";
            else
                instanceKey = null;

            if(instanceKey != null)
                double.TryParse(Executor.Current.GetKey(instanceKey + "input", "0.0"), out value);
        }

        protected virtual void RaiseValueChanged()
        {
            var handler = ValueChanged;
            if (handler != null)
            {
                var task = new Task(() =>
                    {
                        handler.Invoke(this, new BrightnessChangedEventArgs(this.Value));
                    });
                task.Start();
            }
        }

        public double Value
        {
            get { return this.value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;

                    RaiseValueChanged();

                    if (instanceKey != null)
                        Executor.Current.SetKey(this.instanceKey + "input", this.value.ToString());
                }
            }
        }

        public void StartDevice()
        {
            RaiseValueChanged();
        }

        public string Name
        {
            get { return this.name; }
        }
    }
}
