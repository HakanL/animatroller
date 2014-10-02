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
    public class AnalogInput2 : BaseDevice, ISupportsPersistence
    {
        protected double currentValue;
        protected string instanceKey;
        protected ISubject<DoubleZeroToOne> control;
        protected ISubject<DoubleZeroToOne> value;

        public AnalogInput2(string name, bool persistState = false)
            : base(name, persistState)
        {
            if (persistState)
                instanceKey = name.GetHashCode().ToString() + "_";
            else
                instanceKey = null;

            if (instanceKey != null)
            {
                double doubleValue;
                double.TryParse(Executor.Current.GetKey(instanceKey + "input", "0.0"), out doubleValue);
                this.currentValue = doubleValue;
            }

            this.value = new Subject<DoubleZeroToOne>();
            this.control = new Subject<DoubleZeroToOne>();

            this.control.Subscribe(x =>
                {
                    if (this.currentValue != x.Value)
                    {
#if DEBUG
                        if (!x.IsValid())
                            throw new ArgumentOutOfRangeException();
#endif

                        this.currentValue = x.Value;

                        this.value.OnNext(x);
                    }
                });
        }

        public void SetValueFromPersistence(Func<string, string, string> getKeyFunc)
        {
            double.TryParse(getKeyFunc("input", "0.0"), out this.currentValue);
        }

        public void SaveValueToPersistence(Action<string, string> setKeyFunc)
        {
            setKeyFunc("input", this.currentValue.ToString());
        }

        public bool PersistState
        {
            get { return this.persistState; }
        }

        public ISubject<DoubleZeroToOne> Control
        {
            get
            {
                return this.control;
            }
        }

        public IObservable<DoubleZeroToOne> Output
        {
            get
            {
                return this.value;
            }
        }

        public void ConnectTo(ISubject<DoubleZeroToOne> component)
        {
            this.value.Subscribe(component);
        }

        public double Value
        {
            get { return this.currentValue; }
            set
            {
                this.control.OnNext(new DoubleZeroToOne(value));
            }
        }

        public override void StartDevice()
        {
            this.value.OnNext(new DoubleZeroToOne(this.currentValue));
        }
    }
}
