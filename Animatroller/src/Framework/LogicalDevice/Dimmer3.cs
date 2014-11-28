using System;
using System.Collections.Immutable;
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
        protected ControlSubject<double, IControlToken> brightness;
        private ISubject<double> outputFilter;
        private IObservable<double> output;
        private IDisposable outputFilterSubscription;

        public Dimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.output =
            this.brightness = new ControlSubject<double, IControlToken>(0, HasControl);
        }

        public ControlledObserver<double> GetBrightnessObserver(IControlToken controlToken)
        {
            return new ControlledObserver<double>(controlToken, this.brightness);
        }

        public ControlledObserver<double> GetBrightnessObserver()
        {
            var controlToken = Executor.Current.GetControlToken(this);

            return new ControlledObserver<double>(controlToken, this.brightness);
        }

        public void SetOutputFilter(ISubject<double> outputFilter)
        {
            if (this.outputFilterSubscription != null)
            {
                // Remove existing
                this.output = this.brightness;
                this.outputFilterSubscription.Dispose();
                this.outputFilterSubscription = null;
            }

            if (outputFilter != null)
            {
                this.outputFilterSubscription = this.brightness.Subscribe(outputFilter);
                this.output = outputFilter;
            }

            this.outputFilter = outputFilter;
        }

        public IObservable<double> OutputBrightness
        {
            get
            {
                return this.output.DistinctUntilChanged();
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
                if (HasControl())
                    // Only allow if nobody is controlling us
                    this.brightness.OnNext(value, Executor.Current.GetControlToken(this));
            }
        }

        protected override void UpdateOutput()
        {
            this.brightness.OnNext(this.brightness.Value);
        }
    }
}
