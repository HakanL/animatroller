using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Utility
{
    public class ReactiveOr : IObservable<bool>
    {
        private class CheckerHolder
        {
            public IObservable<bool> Checker { get; set; }
            public bool LastValue { get; set; }
        }

        private Subject<bool> outputSubject;
        private List<CheckerHolder> checkers;

        public ReactiveOr(params IObservable<bool>[] inputs)
        {
            this.outputSubject = new Subject<bool>();
            this.checkers = new List<CheckerHolder>();

            foreach (var input in inputs)
                Add(input);
        }

        private void Check()
        {
            foreach (var checker in this.checkers)
            {
                if (checker.LastValue)
                {
                    this.outputSubject.OnNext(true);
                    return;
                }
            }

            this.outputSubject.OnNext(false);
        }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            return this.outputSubject
                .DistinctUntilChanged()
                .Subscribe(observer);
        }

        public ReactiveOr Add(IObservable<bool> input, bool initialValue = false)
        {
            var checker = new CheckerHolder
            {
                Checker = input,
                LastValue = initialValue
            };
            this.checkers.Add(checker);

            input.Subscribe(x =>
            {
                checker.LastValue = x;
                Check();
            });

            return this;
        }
    }
}
