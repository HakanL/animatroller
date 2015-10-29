using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;

namespace Animatroller.Framework.LogicalDevice
{
    public class GroupDimmer : Group<IReceivesBrightness>, IReceivesBrightness
    {
        public GroupDimmer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        public ControlledObserverData GetDataObserver(IControlToken token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            var observers = new List<ControlledObserverData>();
            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    observers.Add(member.GetDataObserver(token));
                }
            }

            var groupObserver = new ControlSubject<IData, IControlToken>(null, HasControl);
            groupObserver.Subscribe(x =>
            {
                foreach (var observer in observers)
                    observer.OnNext(x);
            });

            return new ControlledObserverData(token, groupObserver);
        }

        public void SetBrightness(double brightness, IControlToken token = null)
        {
            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    //TODO: Should this be token here, or the group token?
                    member.SetBrightness(brightness, token);
                }
            }
        }

        public double Brightness
        {
            get
            {
                return double.NaN;
            }
        }
    }
}
