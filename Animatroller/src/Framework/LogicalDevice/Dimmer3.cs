using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class Dimmer3 : SingleOwnerDevice, IReceivesBrightness, ISendsBrightness
    {
        protected BehaviorSubject<double> brightness;

        public Dimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.brightness = new BehaviorSubject<double>(0.0);
        }

        public ControlledObserver<double> GetBrightnessObserver(IControlToken controlToken)
        {
            return new ControlledObserver<double>(controlToken, this, this.brightness);
        }

        public IObservable<double> OutputBrightness
        {
            get
            {
                return this.brightness.DistinctUntilChanged();
            }
        }

        public double Brightness
        {
            get
            {
                return this.brightness.Value;
            }
            set
            {
                if(HasControl(null))
                    // Only allow if nobody is controlling us
                    this.brightness.OnNext(value);
            }
        }

        protected override void UpdateOutput()
        {
            this.brightness.OnNext(this.brightness.Value);
        }
    }
}
