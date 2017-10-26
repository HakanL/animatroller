using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Effect;
using Serilog;
using System.Threading;

namespace Animatroller.Framework.Controller
{
    public class Sequence : ISequence, ICanExecute
    {
        protected ILogger log;

        public class SequenceJob : IRunnableState, ISequenceInstance
        {
            protected ILogger log;
            private object lockObject = new object();
            private string id;
            protected string name;
            protected Action<ISequenceInstance> setUpAction;
            protected Action<ISequenceInstance> tearDownAction;
            protected List<Action<ISequenceInstance>> actions;
            protected List<Tuple<int, Effect.IEffect>> effects;
            protected System.Threading.CancellationToken cancelToken;
            protected bool cancelRequested;

            public SequenceJob(ILogger logger, string name)
            {
                this.log = logger;
                this.name = name;
                this.actions = new List<Action<ISequenceInstance>>();
                this.effects = new List<Tuple<int, IEffect>>();
                this.id = Guid.NewGuid().GetHashCode().ToString();
            }

            public System.Threading.CancellationToken CancelToken
            {
                get { return this.cancelToken; }
            }

            public bool IsCancellationRequested
            {
                get { return this.cancelToken.IsCancellationRequested; }
            }

            public IRunnableState SetUp(Action<ISequenceInstance> action)
            {
                this.setUpAction = action;

                return this;
            }

            public IRunnableState Execute(Action<ISequenceInstance> action)
            {
                this.actions.Add(action);

                return this;
            }

            public IRunnableState TearDown(Action<ISequenceInstance> action)
            {
                this.tearDownAction = action;

                return this;
            }

            public ISequenceInstance WaitFor(TimeSpan value, bool abortImmediatelyIfCanceled)
            {
                if (abortImmediatelyIfCanceled)
                {
                    this.cancelToken.WaitHandle.WaitOne(value);
                    this.cancelToken.ThrowIfCancellationRequested();
                }
                else
                {
                    Thread.Sleep(value);
                }

                return this;
            }

            public ISequenceInstance WaitUntilCancel(bool throwExceptionIfCanceled = true)
            {
                this.cancelToken.WaitHandle.WaitOne();

                if (throwExceptionIfCanceled)
                    this.cancelToken.ThrowIfCancellationRequested();

                return this;
            }

            internal void Run(System.Threading.CancellationToken cancelToken, bool loop)
            {
                // Can only execute one at a time
                lock (lockObject)
                {
                    this.log.Information("Starting SequenceJob {0}", this.name);

                    this.cancelToken = cancelToken;
                    this.cancelRequested = false;

                    if (this.setUpAction != null)
                        this.setUpAction.Invoke(this);

                    foreach (var effect in this.effects)
                        effect.Item2.Start(effect.Item1);

                    do
                    {
                        if (!this.actions.Any())
                            WaitUntilCancel();
                        else
                        {
                            try
                            {
                                foreach (var action in this.actions)
                                    action.Invoke(this);
                            }
                            catch (OperationCanceledException)
                            {
                            }
                        }

                    } while (loop && !cancelToken.IsCancellationRequested && !this.cancelRequested);

                    foreach (var effect in this.effects)
                        effect.Item2.Stop();

                    if (this.tearDownAction != null)
                        this.tearDownAction.Invoke(this);

                    if (cancelToken.IsCancellationRequested)
                        this.log.Information("SequenceJob {0} canceled and stopped", this.name);
                    else
                        this.log.Information("SequenceJob {0} completed", this.name);
                }
            }

            public IRunnableState Controls(int priority = 1, params IEffect[] effects)
            {
                foreach (var effect in effects)
                    this.effects.Add(Tuple.Create(priority, effect));

                return this;
            }

            public void Stop()
            {
                this.cancelRequested = true;
            }

            public void AbortIfCanceled()
            {
                if (this.cancelRequested)
                    throw new OperationCanceledException();
                this.cancelToken.ThrowIfCancellationRequested();
            }

            public string Id
            {
                get { return this.id; }
            }

            public IControlToken Token
            {
                get
                {
                    return null;
                }
            }

            public TimeSpan Runtime
            {
                get => TimeSpan.FromMilliseconds(-1);
            }

            public int IterationCounter
            {
                get => 0;
            }
        }

        private string name;
        private SequenceJob sequenceJob;

        public bool IsMultiInstance { get; private set; }
        public bool IsLoop { get; private set; }

        public Sequence([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.log = Log.Logger;
            this.name = name;
        }

        public string Name
        {
            get { return this.name; }
        }

        public ISequence MultiInstance
        {
            get
            {
                this.IsMultiInstance = true;

                return this;
            }
        }

        public ISequence Loop
        {
            get
            {
                this.IsLoop = true;

                return this;
            }
        }

        public void Execute(System.Threading.CancellationToken cancelToken)
        {
            if (this.sequenceJob == null)
                return;

            this.sequenceJob.Run(cancelToken, this.IsLoop);
        }

        public IRunnableState WhenExecuted
        {
            get
            {
                if (this.sequenceJob == null)
                    this.sequenceJob = new SequenceJob(this.log, this.name);

                return this.sequenceJob;
            }
        }
    }
}
