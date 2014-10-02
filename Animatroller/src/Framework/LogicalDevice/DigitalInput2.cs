using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class DigitalInput2 : BaseDevice
    {
        protected bool currentValue;
        protected string instanceKey;
        protected ISubject<bool> control;
        protected ISubject<bool> value;

        public DigitalInput2(string name, bool persistState = false)
            : base(name)
        {
            if (persistState)
                instanceKey = name.GetHashCode().ToString() + "_";
            else
                instanceKey = null;

            if (instanceKey != null)
                bool.TryParse(Executor.Current.GetKey(instanceKey + "input", false.ToString()), out this.currentValue);

            this.value = new Subject<bool>();
            this.control = new Subject<bool>();

            this.control.Subscribe(x =>
            {
                if (this.currentValue != x)
                {
                    this.currentValue = x;

                    this.value.OnNext(x);
                }
            });
        }

        //public void Trigger(bool value)
        //{
        //    this.Active = value;
        //}

        public ISubject<bool> Control
        {
            get
            {
                return this.control;
            }
        }

        public IObservable<bool> Output
        {
            get
            {
                return this.value;
            }
        }

        public void ConnectTo(ISubject<bool> component)
        {
            this.value.Subscribe(component);
        }

        public bool Value
        {
            get { return this.currentValue; }
            set
            {
                this.control.OnNext(value);
            }
        }

        public override void StartDevice()
        {
            this.value.OnNext(this.currentValue);
        }
    }
}
