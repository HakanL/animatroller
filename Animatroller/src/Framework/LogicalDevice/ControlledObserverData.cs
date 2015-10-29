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
    public class ControlledObserverData : IDisposableObserver<IData>
    {
        private IControlToken controlToken;
        private ControlSubject<IData, IControlToken> control;
        private IData data;

        public ControlledObserverData(IControlToken controlToken, ControlSubject<IData, IControlToken> control)
        {
            this.controlToken = controlToken;
            this.control = control;
            this.data = new Data();
        }

        public void OnCompleted()
        {
            this.control.OnCompleted();
        }

        public void OnError(Exception error)
        {
            this.control.OnError(error);
        }

        public void OnNext(IData value)
        {
            foreach (var kvp in value)
                this.data[kvp.Key] = kvp.Value;

            this.control.OnNext(value, this.controlToken);
        }

        public void Dispose()
        {
            if (this.controlToken != null)
                this.controlToken.Dispose();
        }
    }
}
