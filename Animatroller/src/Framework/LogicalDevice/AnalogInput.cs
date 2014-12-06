using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class AnalogInput : ILogicalDevice
    {
        protected string name;
        protected DoubleZeroToOne value;
        protected string instanceKey;
        protected ISubject<DoubleZeroToOne> value2;

        public event EventHandler<BrightnessChangedEventArgs> ValueChanged;

        public AnalogInput(string name, bool persistState = false)
        {
            this.value = new DoubleZeroToOne();

            this.name = name;
            Executor.Current.Register(this);

            if (persistState)
                instanceKey = name.GetHashCode().ToString() + "_";
            else
                instanceKey = null;

            if (instanceKey != null)
            {
                double doubleValue;
                double.TryParse(Executor.Current.GetKey(instanceKey + "input", "0.0"), out doubleValue);
                this.value.Value = doubleValue;
            }

            this.value2 = new Subject<DoubleZeroToOne>();
        }

        public IObservable<DoubleZeroToOne> Subscribe()
        {
            return this.value2;
        }

        public void ConnectTo(ISubject<DoubleZeroToOne> component)
        {
            this.value2.Subscribe(x => component.OnNext(x));
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
            get { return this.value.Value; }
            set
            {
                if (this.value.Value != value)
                {
                    this.value.Value = value;

                    RaiseValueChanged();

                    this.value2.OnNext(this.value);

                    if (instanceKey != null)
                        Executor.Current.SetKey(this.instanceKey + "input", this.value.Value.ToString());
                }
            }
        }

        public void SetInitialState()
        {
            RaiseValueChanged();
        }

        public string Name
        {
            get { return this.name; }
        }
    }
}
