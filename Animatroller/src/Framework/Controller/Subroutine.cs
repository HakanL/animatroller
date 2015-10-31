using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice;
using NLog;

namespace Animatroller.Framework.Controller
{
    public class Subroutine : ICanExecute, ISequenceInstance2
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        private object lockObject = new object();
        private string id;
        private string name;
        protected Action setUpAction;
        protected Action tearDownAction;
        protected Action<ISequenceInstance> mainAction;
        protected System.Threading.CancellationToken cancelToken;
        private HashSet<IOwnedDevice> handleLocks;
        protected GroupControlToken groupControlToken;
        private int lockPriority;

        public Subroutine([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;
            this.id = Guid.NewGuid().GetHashCode().ToString();
            this.handleLocks = new HashSet<IOwnedDevice>();
        }

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

        public Subroutine RunAction(Action<ISequenceInstance> action)
        {
            this.mainAction = action;

            return this;
        }

        public void TearDown(Action action)
        {
            this.tearDownAction = action;
        }

        public IControlToken Token
        {
            get
            {
                return this.groupControlToken;
            }
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

        private void Lock()
        {
            var heldLocks = new Dictionary<IOwnedDevice, IControlToken>();
            foreach (var handleLock in this.handleLocks)
            {
                var control = handleLock.TakeControl(priority: this.lockPriority, name: Name);

                heldLocks.Add(handleLock, control);
            }

            this.groupControlToken = new GroupControlToken(heldLocks, disposeLocks: true, priority: this.lockPriority);
        }

        private void Release()
        {
            if (this.groupControlToken != null)
            {
                this.groupControlToken.Dispose();
                this.groupControlToken = null;
            }
        }

        public void Execute(System.Threading.CancellationToken cancelToken)
        {
            // Can only execute one at a time
            lock (lockObject)
            {
                log.Info("Starting Subroutine {0}", Name);

                this.cancelToken = cancelToken;

                if (this.setUpAction != null)
                    this.setUpAction.Invoke();

                Lock();

                if (this.mainAction != null)
                    this.mainAction(this);

                Executor.Current.WaitForManagedTasks(true);

                Release();

                if (this.tearDownAction != null)
                    this.tearDownAction.Invoke();

                if (cancelToken.IsCancellationRequested)
                    log.Info("SequenceJob {0} canceled and stopped", Name);
                else
                    log.Info("SequenceJob {0} completed", Name);
            }
        }

        public string Id
        {
            get { return this.id; }
        }

        public bool IsMultiInstance { get; set; }

        public string Name
        {
            get { return this.name; }
        }

        public void Run()
        {
            Executor.Current.Execute(this);
        }

        public Subroutine LockWhenRunning(int lockPriority = 1, params IOwnedDevice[] devices)
        {
            this.lockPriority = lockPriority;

            foreach (var device in devices)
                this.handleLocks.Add(device);

            return this;
        }

        public Subroutine LockWhenRunning(params IOwnedDevice[] devices)
        {
            return LockWhenRunning(1, devices);
        }
    }
}
