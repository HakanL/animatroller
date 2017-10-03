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
using Serilog;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledObserverRGB : IDisposable
    {
        private IObserver<double> observerR;
        private IObserver<double> observerG;
        private IObserver<double> observerB;

        private IControlToken controlToken;

        public ControlledObserverRGB(IControlToken controlToken, ControlSubject<Color, IControlToken> control)
        {
            this.controlToken = controlToken;

            this.observerR = Observer.Create<double>(x =>
                {
                    Color newColor = Color.FromArgb(x.GetByteScale(), control.Value.G, control.Value.B);

                    control.OnNext(newColor, this.controlToken);
                });

            this.observerG = Observer.Create<double>(x =>
                {
                    Color newColor = Color.FromArgb(control.Value.R, x.GetByteScale(), control.Value.B);

                    control.OnNext(newColor, this.controlToken);
                });

            this.observerB = Observer.Create<double>(x =>
                {
                    Color newColor = Color.FromArgb(control.Value.R, control.Value.G, x.GetByteScale());

                    control.OnNext(newColor, this.controlToken);
                });
        }

        public IObserver<double> R
        {
            get { return this.observerR; }
        }

        public IObserver<double> G
        {
            get { return this.observerG; }
        }

        public IObserver<double> B
        {
            get { return this.observerB; }
        }

        public void Dispose()
        {
            if (this.controlToken != null)
                this.controlToken.Dispose();
        }
    }
}
