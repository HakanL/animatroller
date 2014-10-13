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
        protected ISubject<double> inputPan;
        protected ISubject<double> inputTilt;

        public MovingHead([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputPan = new Subject<double>();
            this.inputTilt = new Subject<double>();

            this.inputPan.Subscribe(x =>
                {
                    if (this.currentPan != x)
                    {
                        this.currentPan = x;
                    }
                });

            this.inputTilt.Subscribe(x =>
            {
                if (this.currentTilt != x)
                {
                    this.currentTilt = x;
                }
            });
        }

        public override void StartDevice()
        {
            base.StartDevice();

            inputPan.OnNext(0);
            inputTilt.OnNext(0);
        }

        public ISubject<double> InputPan
        {
            get
            {
                return this.inputPan;
            }
        }

        public ISubject<double> InputTilt
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
                this.inputPan.OnNext(value);
            }
        }

        public double Tilt
        {
            get { return this.currentTilt; }
            set
            {
                this.inputTilt.OnNext(value);
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
