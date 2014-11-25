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
    public class ControlledObserver<T> : IObserver<T>
    {
        private IObserver<T> observer;

        public ControlledObserver(IControlToken controlToken, IOwnedDevice device, IObserver<T> control)
        {
            this.observer = Observer.Create<T>(onNext =>
                {
                    if (device.HasControl(controlToken))
                        control.OnNext(onNext);
                });
        }

        public void OnCompleted()
        {
            this.observer.OnCompleted();
        }

        public void OnError(Exception error)
        {
            this.observer.OnError(error);
        }

        public void OnNext(T value)
        {
            this.observer.OnNext(value);
        }
    }
}
