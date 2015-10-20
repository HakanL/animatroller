using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class StrobeDimmer3 : Dimmer3, ISendsStrobeSpeed
    {
        protected ReplaySubject<double> strobeSpeed;

        public StrobeDimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.strobeSpeed = new ReplaySubject<double>(1);
        }

        public ControlledObserver<double> GetStrobeSpeedObserver(IControlToken controlToken)
        {
            throw new NotImplementedException();
            //FIXME
            //            return new ControlledObserver<double>(controlToken, this, this.strobeSpeed);
        }

        public IObservable<double> OutputStrobeSpeed
        {
            get
            {
                return this.strobeSpeed.DistinctUntilChanged();
            }
        }

        public double StrobeSpeed
        {
            get { return this.strobeSpeed.GetLatestValue(); }
            set
            {
                if (HasControl(null))
                    // Only allow if nobody is controlling us
                    this.strobeSpeed.OnNext(value);
            }
        }

        protected override void UpdateOutput()
        {
            base.UpdateOutput();

            this.strobeSpeed.OnNext(this.strobeSpeed.GetLatestValue());
        }
    }
}
