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
    public class DigitalOutput2 : SingleOwnerOutputDevice
    {
        private bool currentValue;
        protected IObserver<bool> controlValue;
        protected ISubject<bool> outputValue;

        public DigitalOutput2([System.Runtime.CompilerServices.CallerMemberName] string name = "", bool initial = false)
            : base(name)
        {
            this.currentValue = initial;

            this.controlValue = Observer.Create<bool>(x =>
                {
                    if (this.currentValue != x)
                    {
                        this.currentValue = x;

                        UpdateOutput();
                    }
                });

            this.outputValue = new Subject<bool>();
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

        public IObserver<bool> ControlValue
        {
            get
            {
                return this.controlValue;
            }
        }

        public IObservable<bool> Output
        {
            get
            {
                return this.outputValue;
            }
        }

        public bool Power
        {
            get { return this.currentValue; }
            set
            {
                if (this.currentValue != value)
                {
                    this.currentValue = value;

                    UpdateOutput();
                }
            }
        }

        protected override void UpdateOutput()
        {
            this.outputValue.OnNext(this.currentValue && this.MasterPower);
        }

        //public virtual Switch SetPower(bool value)
        //{
        //    this.Power = value;

        //    return this;
        //}

        //public virtual Switch TurnOff()
        //{
        //    this.Power = false;

        //    return this;
        //}
    }
}
