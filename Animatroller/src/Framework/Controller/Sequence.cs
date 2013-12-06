using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Controller
{
    public class Sequence : ISequence, ICanExecute
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        public class SequenceJob : IRunnableState, ISequenceInstance
        {
            private object lockObject = new object();
            protected string name;
            protected Action setUpAction;
            protected Action tearDownAction;
            protected List<Action<ISequenceInstance>> actions;
            protected System.Threading.CancellationToken cancelToken;

            public SequenceJob(string name)
            {
                this.name = name;
                this.actions = new List<Action<ISequenceInstance>>();
            }

            public System.Threading.CancellationToken CancelToken
            {
                get { return this.cancelToken; }
            }

            public bool IsCancellationRequested
            {
                get { return this.cancelToken.IsCancellationRequested; }
            }

            public IRunnableState SetUp(Action action)
            {
                this.setUpAction = action;

                return this;
            }

            public IRunnableState Execute(Action<ISequenceInstance> action)
            {
                this.actions.Add(action);

                return this;
            }

            public IRunnableState TearDown(Action action)
            {
                this.tearDownAction = action;

                return this;
            }

            public ISequenceInstance WaitFor(TimeSpan value)
            {
                return WaitFor(value, true);
            }

            public ISequenceInstance WaitFor(TimeSpan value, bool throwExceptionIfCanceled)
            {
                this.cancelToken.WaitHandle.WaitOne(value);

                if (throwExceptionIfCanceled)
                    this.cancelToken.ThrowIfCancellationRequested();

                return this;
            }

            public ISequenceInstance WaitUntilCancel()
            {
                this.cancelToken.WaitHandle.WaitOne();

                return this;
            }

            public ISequenceInstance WaitUntilCancel(bool throwExceptionIfCanceled)
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
                    log.Info("Starting SequenceJob {0}", this.name);

                    this.cancelToken = cancelToken;

                    if (this.setUpAction != null)
                        this.setUpAction.Invoke();

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

                    } while (loop && !cancelToken.IsCancellationRequested);

                    if (this.tearDownAction != null)
                        this.tearDownAction.Invoke();

                    if (cancelToken.IsCancellationRequested)
                        log.Info("SequenceJob {0} canceled and stopped", this.name);
                    else
                        log.Info("SequenceJob {0} completed", this.name);
                }
            }
        }

        private string name;
        private SequenceJob sequenceJob;

        public bool IsMultiInstance { get; private set; }
        public bool IsLoop { get; private set; }

        public Sequence(string name)
        {
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
                    this.sequenceJob = new SequenceJob(this.name);

                return this.sequenceJob;
            }
        }
    }
}
