using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Subjects;

namespace Animatroller.Framework.Effect
{
    public class Blackout : ISubject<double>
    {
        private double lastValue;
        private double blackout;
        private Subject<double> subject;

        public Blackout()
        {
            this.subject = new Subject<double>();
            Executor.Current.Blackout.Subscribe(x =>
                {
                    this.blackout = x;

                    PushValue();
                });
        }

        private void PushValue()
        {
            this.subject.OnNext(this.lastValue * (1.0 - this.blackout));
        }

        public void OnCompleted()
        {
            this.subject.OnCompleted();
        }

        public void OnError(Exception error)
        {
            this.subject.OnError(error);
        }

        public void OnNext(double value)
        {
            this.lastValue = value;

            PushValue();
        }

        public IDisposable Subscribe(IObserver<double> observer)
        {
            return this.subject.Subscribe(observer);
        }
    }
}
