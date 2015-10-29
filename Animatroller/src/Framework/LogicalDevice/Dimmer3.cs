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
    public class Dimmer3 : SingleOwnerDevice, IReceivesBrightness//, ISendsBrightness
    {
//        protected ControlSubject<double, IControlToken> brightness;
        private ISubject<double> outputFilter;
        private IObservable<double> output;
        private IDisposable outputFilterSubscription;

        public Dimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.currentData[DataElements.Brightness] = 0.0;

            this.output =
            /*this.brightness =*/ new ControlSubject<double, IControlToken>(0, HasControl);

//            this.releaseActions.Add(() => this.brightness.OnNext(0));
        }

        //public ControlledObserver<double> GetBrightnessObserver(IControlToken token = null)
        //{
        //    // This will return an observer that will work on any thread
        //    return new ControlledObserver<double>(token ?? GetCurrentOrNewToken(), this.brightness);
        //}

        public void SetOutputFilter(ISubject<double> outputFilter)
        {
            if (this.outputFilterSubscription != null)
            {
                // Remove existing
//FIXME                this.output = this.brightness;
                this.outputFilterSubscription.Dispose();
                this.outputFilterSubscription = null;
            }

            if (outputFilter != null)
            {
//FIXME                this.outputFilterSubscription = this.brightness.Subscribe(outputFilter);
                this.output = outputFilter;
            }

            this.outputFilter = outputFilter;
        }

        //public IObservable<double> OutputBrightness
        //{
        //    get
        //    {
        //        return this.output.DistinctUntilChanged();
        //    }
        //}

        public double Brightness
        {
            get
            {
                return (double)this.currentData[DataElements.Brightness];
            }
            set
            {
                // Note that this will only match the token when called on the same thread as
                // where control was taken (TakeControl)
//                this.brightness.OnNext(value, Executor.Current.GetControlToken(this));
                //                this.outputData.OnNext(this.currentData, )
                PushData(DataElements.Brightness, value);
            }
        }
    }
}
