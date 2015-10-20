using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class MovingHead : StrobeColorDimmer3, IReceivesPanTilt
    {
        protected ControlSubject<double, IControlToken> pan;
        protected ControlSubject<double, IControlToken> tilt;

        public MovingHead([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.pan = new ControlSubject<double, IControlToken>(0, HasControl);
            this.tilt = new ControlSubject<double, IControlToken>(0, HasControl);
        }

        public ControlledObserver<double> GetPanObserver(IControlToken token = null)
        {
            return new ControlledObserver<double>(token ?? GetCurrentOrNewToken(), this.pan);
        }

        public ControlledObserver<double> GetTiltObserver(IControlToken token = null)
        {
            return new ControlledObserver<double>(token ?? GetCurrentOrNewToken(), this.tilt);
        }

        public double Pan
        {
            get
            {
                return this.pan.Value;
            }
            set
            {
                this.pan.OnNext(value, Executor.Current.GetControlToken(this));
            }
        }

        public IObservable<double> OutputPan
        {
            get
            {
                return this.pan.DistinctUntilChanged();
            }
        }

        public IObservable<double> OutputTilt
        {
            get
            {
                return this.tilt.DistinctUntilChanged();
            }
        }

        public double Tilt
        {
            get
            {
                return this.tilt.Value;
            }
            set
            {
                this.tilt.OnNext(value, Executor.Current.GetControlToken(this));
            }
        }

        protected override void UpdateOutput()
        {
            this.pan.OnNext(this.pan.Value);
            this.tilt.OnNext(this.tilt.Value);
        }
    }
}
