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
using System.Threading;

namespace Animatroller.Framework.LogicalDevice
{
    public class DigitalInput2 : BaseDevice, ISupportsPersistence, ILogicalOutputDevice<bool>, ILogicalControlDevice<bool>, IInputHardware
    {
        protected bool currentValue;
        protected ISubject<bool> controlValue;
        protected ISubject<bool> outputValue;
        protected ISubject<bool> outputHeld;
        protected TimeSpan? holdTimeout;
        private Timer holdTimer;

        public DigitalInput2([System.Runtime.CompilerServices.CallerMemberName] string name = "",
            bool persistState = false,
            TimeSpan? autoResetDelay = null,
            TimeSpan? holdTimeout = null)
            : base(name, persistState)
        {
            this.outputValue = new Subject<bool>();
            this.outputHeld = new Subject<bool>();
            this.controlValue = new Subject<bool>();
            this.holdTimeout = holdTimeout;

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

                    if (this.holdTimeout.HasValue)
                    {
                        if (x)
                            this.holdTimer.Change(this.holdTimeout.Value, TimeSpan.FromMilliseconds(-1));
                        else
                            this.outputHeld.OnNext(false);
                    }
                }
            });

            this.holdTimer = new Timer(state =>
            {
                try
                {
                    this.holdTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    this.outputHeld.OnNext(true);
                }
                catch (Exception ex)
                {
                    Executor.Current.LogInfo("Error in hold timer callback: " + ex.Message);
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

        public IObservable<bool> IsHeld
        {
            get
            {
                return this.outputHeld.AsObservable();
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
