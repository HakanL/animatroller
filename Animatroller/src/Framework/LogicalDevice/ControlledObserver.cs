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
    public class ControlledObserver<T> : IDisposableObserver<T>
    {
        private IControlToken controlToken;
        private ControlSubject<T, IControlToken> control;

        public ControlledObserver(IControlToken controlToken, ControlSubject<T, IControlToken> control)
        {
            this.controlToken = controlToken;
            this.control = control;
        }

        public void OnCompleted()
        {
            this.control.OnCompleted();
        }

        public void OnError(Exception error)
        {
            this.control.OnError(error);
        }

        public void OnNext(T value)
        {
            this.control.OnNext(value, this.controlToken);
        }

        public void Dispose()
        {
            if (this.controlToken != null)
                this.controlToken.Dispose();
        }
    }
}
