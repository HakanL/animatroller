using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class DigitalInput2 : BaseDevice, ISupportsPersistence
    {
        protected bool currentValue;
        protected ISubject<bool> control;
        protected ISubject<bool> outputValue;

        public DigitalInput2([System.Runtime.CompilerServices.CallerMemberName] string name = "", bool persistState = false)
            : base(name, persistState)
        {
            this.outputValue = new Subject<bool>();
            this.control = new Subject<bool>();

            this.control.Subscribe(x =>
            {
                if (this.currentValue != x)
                {
                    this.currentValue = x;

                    this.outputValue.OnNext(x);
                }
            });
        }

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
                return this.outputValue;
            }
        }

        public void ConnectTo(ISubject<bool> component)
        {
            this.outputValue.Subscribe(component);
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
            this.outputValue.OnNext(this.currentValue);
        }

        public void SetValueFromPersistence(Func<string, string, string> getKeyFunc)
        {
            bool.TryParse(getKeyFunc("input", false.ToString()), out this.currentValue);
        }

        public void SaveValueToPersistence(Action<string, string> setKeyFunc)
        {
            setKeyFunc("input", this.currentValue.ToString());
        }

        public bool PersistState
        {
            get { return this.persistState; }
        }
    }
}
