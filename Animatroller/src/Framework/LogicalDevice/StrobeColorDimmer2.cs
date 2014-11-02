using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class StrobeColorDimmer2 : ColorDimmer2
    {
        protected double currentStrobeSpeed;
        protected ISubject<DoubleZeroToOne> inputStrobeSpeed;

        public StrobeColorDimmer2([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputStrobeSpeed = new Subject<DoubleZeroToOne>();

            this.inputStrobeSpeed.Subscribe(x =>
                {
                    if (this.currentStrobeSpeed != x.Value)
                    {
#if DEBUG
                        if (!x.IsValid())
                            throw new ArgumentOutOfRangeException("Value is out of range");
#endif

                        this.currentStrobeSpeed = x.Value.Limit(0, 1);
                    }
                });
        }

        public override void StartDevice()
        {
            base.StartDevice();

            inputStrobeSpeed.OnNext(DoubleZeroToOne.Zero);
        }

        public ISubject<DoubleZeroToOne> InputStrobeSpeed
        {
            get
            {
                return this.inputStrobeSpeed;
            }
        }

        public double StrobeSpeed
        {
            get { return this.currentStrobeSpeed; }
            set
            {
                this.inputStrobeSpeed.OnNext(new DoubleZeroToOne(value));
            }
        }

        public override void TurnOff()
        {
            base.TurnOff();

            StrobeSpeed = 0;
        }
    }
}
