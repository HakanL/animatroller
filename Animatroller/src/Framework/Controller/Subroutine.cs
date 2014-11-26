using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Controller
{
    public class Subroutine : ICanExecute, ISequenceInstance2
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        private string name;
        private List<IOwnedDevice> handleLocks;
        private object lockObject = new object();
        private string id;
        protected Action setUpAction;
        protected Action tearDownAction;
        protected Action<ISequenceInstance> mainAction;
        protected System.Threading.CancellationToken cancelToken;

        public System.Threading.CancellationToken CancelToken
        {
            get { return this.cancelToken; }
        }

        public bool IsCancellationRequested
        {
            get { return this.cancelToken.IsCancellationRequested; }
        }

        public void SetUp(Action action)
        {
            this.setUpAction = action;
        }

        public void RunAction(Action<ISequenceInstance> action)
        {
            this.mainAction = action;
        }

        public void TearDown(Action action)
        {
            this.tearDownAction = action;
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

        public void Execute(System.Threading.CancellationToken cancelToken)
        {
            // Can only execute one at a time
            lock (lockObject)
            {
                log.Info("Starting SequenceJob {0}", this.name);

                this.cancelToken = cancelToken;

                if (this.setUpAction != null)
                    this.setUpAction.Invoke();


                var heldLocks = new Dictionary<IOwnedDevice, IControlToken>();
                foreach (var handleLock in this.handleLocks)
                {
                    var control = handleLock.TakeControl(LockPriority, Name);

                    Executor.Current.SetControlToken(handleLock, control);

                    heldLocks.Add(handleLock, control);
                }

                if (this.mainAction != null)
                    this.mainAction(this);

                foreach (var kvp in heldLocks)
                {
                    Executor.Current.RemoveControlToken(kvp.Key);
                    kvp.Value.Dispose();
                }

                if (this.tearDownAction != null)
                    this.tearDownAction.Invoke();

                if (cancelToken.IsCancellationRequested)
                    log.Info("SequenceJob {0} canceled and stopped", this.name);
                else
                    log.Info("SequenceJob {0} completed", this.name);
            }
        }

        public string Id
        {
            get { return this.id; }
        }


        public Subroutine([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;

            this.handleLocks = new List<IOwnedDevice>();
            this.id = Guid.NewGuid().GetHashCode().ToString();

            // Default
            this.LockPriority = 1;
        }

        public string Name
        {
            get { return this.name; }
        }

        public int LockPriority { get; set; }

        public bool IsMultiInstance { get; set; }

        public void Run()
        {
            Executor.Current.Execute(this);
        }

        public void LockWhenRunning(params IOwnedDevice[] devices)
        {
            this.handleLocks.AddRange(devices);
        }
    }
}
