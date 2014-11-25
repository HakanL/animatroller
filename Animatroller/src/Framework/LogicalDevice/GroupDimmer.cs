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
    public class GroupDimmer : Group<IReceivesBrightness>, IReceivesBrightness
    {
        protected ReplaySubject<double> brightness;

        public GroupDimmer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.brightness = new ReplaySubject<double>(1);
        }

        public ControlledObserver<double> GetBrightnessObserver(IControlToken controlToken)
        {
            var observers = new List<ControlledObserver<double>>();
            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    IControlToken memberControlToken;
                    if (!this.memberControlTokens.TryGetValue(member, out memberControlToken))
                        // No lock/control token
                        continue;

                    observers.Add(member.GetBrightnessObserver(memberControlToken));
                }
            }

            var groupObserver = Observer.Create<double>(
                onNext: x =>
                {
                    foreach (var observer in observers)
                        observer.OnNext(x);
                });
            return new ControlledObserver<double>(controlToken, this, groupObserver);
        }

        public double Brightness
        {
            get
            {
                return this.brightness.GetLatestValue();
            }
            set
            {
                if (HasControl(null))
                    // Only allow if nobody is controlling us
                    this.brightness.OnNext(value);
            }
        }
    }
}
