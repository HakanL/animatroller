using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice;
using Serilog;
using System.Runtime.Remoting.Messaging;

namespace Animatroller.Framework.Controller
{
    public class Subroutine : ICanExecute, ISequenceInstance2
    {
        protected ILogger log;

        private object lockObject = new object();
        private string id;
        private string name;
        protected Action setUpAction;
        protected Action<ISequenceInstance> tearDownAction;
        protected Action<ISequenceInstance> mainAction;
        protected System.Threading.CancellationToken cancelToken;
        private HashSet<IOwnedDevice> handleLocks;
        protected GroupControlToken groupControlToken;
        private IControlToken externalControlToken;
        private int lockPriority;
        private bool autoAddDevices;

        public Subroutine([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.log = Log.Logger;
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

        public void TearDown(Action<ISequenceInstance> action)
        {
            this.tearDownAction = action;
        }

        public IControlToken Token
        {
            get
            {
                return this.externalControlToken ?? this.groupControlToken;
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

            if (this.externalControlToken == null)
            {
                this.groupControlToken = new GroupControlToken(heldLocks, disposeLocks: true, priority: this.lockPriority);
                this.groupControlToken.AutoAddDevices = this.autoAddDevices;
            }
        }

        private void Release()
        {
            this.groupControlToken?.Dispose();
            this.groupControlToken = null;
        }

        public void SetControlToken(IControlToken token)
        {
            this.externalControlToken = token;
        }

        public void Execute(System.Threading.CancellationToken cancelToken)
        {
            // Can only execute one at a time
            lock (lockObject)
            {
                this.log.Information("Starting Subroutine {0}", Name);

                this.cancelToken = cancelToken;

                if (this.setUpAction != null)
                    this.setUpAction.Invoke();

                Lock();

                CallContext.LogicalSetData("TOKEN", this.externalControlToken ?? this.groupControlToken);

                try
                {
                    if (this.mainAction != null)
                        this.mainAction(this);
                }
                catch (Exception ex)
                {
                    if (!(ex is OperationCanceledException))
                        log.Debug(ex, "Exception when executing subroutine/mainAction");
                }

                Executor.Current.WaitForManagedTasks(true);

                if (this.tearDownAction != null)
                    this.tearDownAction.Invoke(this);

                CallContext.LogicalSetData("TOKEN", null);
                Release();

                if (cancelToken.IsCancellationRequested)
                    this.log.Information("SequenceJob {0} canceled and stopped", Name);
                else
                    this.log.Information("SequenceJob {0} completed", Name);
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

        public Task Run()
        {
            Task task;
            Executor.Current.Execute(this, out task);

            return task;
        }

        public Task Run(out System.Threading.CancellationTokenSource cts)
        {
            Task task;
            cts = Executor.Current.Execute(this, out task);

            return task;
        }

        public void Stop()
        {
            log.Debug("Cancel 6");
            Executor.Current.Cancel(this);
        }

        public void RunAndWait()
        {
            Executor.Current.ExecuteAndWait(this);
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

        public Subroutine AutoAddDevices(int lockPriority = 1, bool enabled = true)
        {
            this.lockPriority = lockPriority;
            this.autoAddDevices = enabled;

            return this;
        }
    }
}
