using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class DigitalOutput2 : SingleOwnerDevice, IOutput
    {
        protected bool currentPower;
        protected ISubject<bool> inputPower;

        public DigitalOutput2([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputPower = new Subject<bool>();

            this.inputPower.Subscribe(x =>
            {
                if (this.currentPower != x)
                {
                    this.currentPower = x;
                }
            });
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

        public ISubject<bool> InputPower
        {
            get
            {
                return this.inputPower;
            }
        }

        public bool Power
        {
            get { return this.currentPower; }
            set
            {
                this.inputPower.OnNext(value);
            }
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

        public override void StartDevice()
        {
            base.StartDevice();

            InputPower.OnNext(false);
        }
    }
}
