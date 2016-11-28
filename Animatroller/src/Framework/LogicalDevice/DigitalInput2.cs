using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class DigitalInput2 : BaseDevice, ISupportsPersistence, ILogicalOutputDevice<bool>, ILogicalControlDevice<bool>, IInputHardware
    {
        protected bool currentValue;
        protected ISubject<bool> controlValue;
        protected ISubject<bool> outputValue;

        public DigitalInput2([System.Runtime.CompilerServices.CallerMemberName] string name = "",
            bool persistState = false,
            TimeSpan? autoResetDelay = null)
            : base(name, persistState)
        {
            this.outputValue = new Subject<bool>();
            this.controlValue = new Subject<bool>();

            IObservable<bool> control = this.controlValue;

            if (autoResetDelay.HasValue && autoResetDelay.Value.TotalMilliseconds > 0)
            {
                control = this.controlValue
                    .Buffer(autoResetDelay.Value, 1)
                    .Select(s => s.Count == 0 ? false : s.Single());
            }

            control.Subscribe(x =>
            {
                if (this.currentValue != x)
                {
                    this.currentValue = x;

                    UpdateOutput();
                }
            });
        }

        public IObserver<bool> Control
        {
            get
            {
                return this.controlValue.AsObserver();
            }
        }

        public IObservable<bool> Output
        {
            get
            {
                return this.outputValue.AsObservable();
            }
        }

        public bool Value
        {
            get { return this.currentValue; }
            set { this.controlValue.OnNext(value); }
        }

        public void SetValueFromPersistence(Func<string, string, string> getKeyFunc)
        {
            bool value;
            bool.TryParse(getKeyFunc("input", false.ToString()), out value);

            Value = value;
        }

        public void SaveValueToPersistence(Action<string, string> setKeyFunc)
        {
            setKeyFunc("input", this.currentValue.ToString());
        }

        public bool PersistState
        {
            get { return this.persistState; }
        }

        protected override void UpdateOutput()
        {
            this.outputValue.OnNext(this.currentValue);
        }
    }
}
