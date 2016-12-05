using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class DigitalOutput2 : SingleOwnerOutputDevice, ILogicalOutputDevice<bool>, ILogicalControlDevice<bool>
    {
        private bool currentValue;
        protected ISubject<bool> controlValue;
        protected ISubject<bool> outputValue;
        private bool initialValue;

        public DigitalOutput2([System.Runtime.CompilerServices.CallerMemberName] string name = "", bool initial = false, TimeSpan? autoResetDelay = null)
            : base(name)
        {
            this.currentValue = initial;
            this.initialValue = initial;

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

            OutputChanged.Subscribe(x =>
            {
                bool? power = x.GetValue<bool>(DataElements.Power);
                if (power.HasValue)
                    this.outputValue.OnNext(power.Value && MasterPower);
            });
        }

        public override void BuildDefaultData(IData data)
        {
            data[DataElements.Power] = this.initialValue;
        }

        //public Switch Follow(OperatingHours source)
        //{
        //    source.OpenHoursChanged += (o, e) =>
        //    {
        //        if (e.IsOpenNow)
        //            this.SetPower(true);
        //        else
        //            this.TurnOff();
        //    };

        //    return this;
        //}

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
            get { return (bool)this.currentData[DataElements.Power]; }
        }

        public void SetValue(bool value, IControlToken token = null)
        {
            SetData(token, Utils.AdditionalData(DataElements.Power, value));
        }
    }
}
