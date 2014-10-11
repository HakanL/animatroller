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
    public class MovingHead : StrobeColorDimmer2
    {
        protected double currentPan;
        protected double currentTilt;
        protected ISubject<DoubleZeroToOne> inputPan;
        protected ISubject<DoubleZeroToOne> inputTilt;

        public MovingHead([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputPan = new Subject<DoubleZeroToOne>();
            this.inputTilt = new Subject<DoubleZeroToOne>();

            this.inputPan.Subscribe(x =>
                {
                    if (this.currentPan != x.Value)
                    {
#if DEBUG
                        if (!x.IsValid())
                            throw new ArgumentOutOfRangeException("Value is out of range");
#endif

                        this.currentPan = x.Value.Limit(0, 1);
                    }
                });

            this.inputTilt.Subscribe(x =>
            {
                if (this.currentTilt != x.Value)
                {
#if DEBUG
                    if (!x.IsValid())
                        throw new ArgumentOutOfRangeException("Value is out of range");
#endif

                    this.currentTilt = x.Value.Limit(0, 1);
                }
            });
        }

        public override void StartDevice()
        {
            base.StartDevice();

            inputPan.OnNext(DoubleZeroToOne.Zero);
            inputTilt.OnNext(DoubleZeroToOne.Zero);
        }

        public ISubject<DoubleZeroToOne> InputPan
        {
            get
            {
                return this.inputPan;
            }
        }

        public ISubject<DoubleZeroToOne> InputTilt
        {
            get
            {
                return this.inputTilt;
            }
        }

        public double Pan
        {
            get { return this.currentPan; }
            set
            {
                this.inputPan.OnNext(new DoubleZeroToOne(value));
            }
        }

        public double Tilt
        {
            get { return this.currentTilt; }
            set
            {
                this.inputTilt.OnNext(new DoubleZeroToOne(value));
            }
        }

        public override void TurnOff()
        {
            base.TurnOff();

            Pan = 0;
            Tilt = 0;
        }
    }
}
