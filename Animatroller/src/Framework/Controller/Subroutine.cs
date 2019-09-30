using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice;
using Serilog;
using System.Threading;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Animatroller.Framework.Controller
{
    public class Subroutine : ICanExecute, ISequenceInstance2
    {
        public enum LifeCycles
        {
            Setup,
            Running,
            RunningLoop,
            IterationCompleted,
            Completed,
            CancelRequested,
            MaxIterationsReached,
            MaxRuntimeReached,
            CancelPending,
            Teardown,
            Stopped
        }

        protected ILogger log;

        private object lockObject = new object();
        private string id;
        private string name;
        protected Action<ISequenceInstance> setUpAction;
        protected Action<ISequenceInstance> tearDownAction;
        protected Action<ISequenceInstance> mainAction;
        protected System.Threading.CancellationToken cancelToken;
        private HashSet<IOwnedDevice> handleLocks;
        protected GroupControlToken groupControlToken;
        private IControlToken externalControlToken;
        private int lockPriority;
        private bool autoAddDevices;
        private bool cancelRequested;
        private Stopwatch runtime;
        private Subject<LifeCycles> lifecycle;
        private int iterationCounter;

        public Subroutine([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.log = Log.Logger;
            this.name = name;
            this.id = Guid.NewGuid().GetHashCode().ToString();
            this.handleLocks = new HashSet<IOwnedDevice>();
            this.lifecycle = new Subject<LifeCycles>();

            this.lifecycle.Subscribe(lc =>
            {
                switch (lc)
                {
                    case LifeCycles.IterationCompleted:
                    case LifeCycles.RunningLoop:
                        this.log.Verbose("Sub {Name} is in lifecycle {LifeCycle} ({IterationCount} iterations)", Name, lc, IterationCounter);
                        break;

                    default:
                        this.log.Verbose("Sub {Name} is in lifecycle {LifeCycle}", Name, lc);
                        break;
                }
            });
        }

        public IObservable<LifeCycles> Lifecycle
        {
            get { return this.lifecycle.AsObservable(); }
        }

        public System.Threading.CancellationToken CancelToken
        {
            get { return this.cancelToken; }
        }

        public bool IsCancellationRequested
        {
            get { return this.cancelRequested || this.cancelToken.IsCancellationRequested; }
        }

        public Subroutine SetUp(Action<ISequenceInstance> action)
        {
            this.setUpAction = action;

            return this;
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

        public void RequestCancel()
        {
            this.cancelRequested = true;
        }

        public bool Loop { get; set; }

        public TimeSpan? MaxRuntime { get; set; }
        public int? MaxIterations { get; set; }

        public Subroutine SetLoop(bool value)
        {
            Loop = value;

            return this;
        }

        public Subroutine SetMaxRuntime(TimeSpan? value)
        {
            MaxRuntime = value;

            return this;
        }

        public Subroutine SetMaxIterations(int? value)
        {
            MaxIterations = value;

            return this;
        }

        public void AbortIfCanceled()
        {
            if (this.cancelRequested)
                throw new OperationCanceledException();
            this.cancelToken.ThrowIfCancellationRequested();
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

        private void Lock(IChannel channel = null)
        {
            var heldLocks = new Dictionary<IOwnedDevice, IControlToken>();
            foreach (var handleLock in this.handleLocks)
            {
                var control = handleLock.TakeControl(channel: channel, priority: this.lockPriority, name: Name);

                heldLocks.Add(handleLock, control);
            }

            if (this.externalControlToken == null)
            {
                this.groupControlToken = new GroupControlToken(heldLocks, disposeLocks: true, priority: this.lockPriority, name: $"Lock in {Name}");
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
            this.runtime = Stopwatch.StartNew();

            // Can only execute one at a time
            lock (lockObject)
            {
                this.log.Information("Starting Subroutine {0}", Name);

                this.cancelToken = cancelToken;
                this.cancelRequested = false;
                this.iterationCounter = 0;

                this.lifecycle.OnNext(LifeCycles.Setup);
                this.setUpAction?.Invoke(this);

                Lock();

                Executor.AsyncLocalTokens.Value = this.externalControlToken ?? this.groupControlToken;
                //CallContext.LogicalSetData("TOKEN", this.externalControlToken ?? this.groupControlToken);

                try
                {
                    this.lifecycle.OnNext(LifeCycles.Running);

                    if (!this.cancelRequested)
                    {
                        this.iterationCounter++;
                        this.mainAction?.Invoke(this);

                        if (!this.cancelRequested)
                            this.lifecycle.OnNext(LifeCycles.IterationCompleted);
                    }

                    while (Loop && !this.cancelRequested && this.mainAction != null)
                    {
                        this.iterationCounter++;
                        this.lifecycle.OnNext(LifeCycles.RunningLoop);

                        if (this.cancelRequested)
                            break;

                        if (this.MaxIterations.HasValue && this.iterationCounter > this.MaxIterations.Value)
                        {
                            this.lifecycle.OnNext(LifeCycles.MaxIterationsReached);
                            break;
                        }

                        if (this.MaxRuntime.HasValue && this.runtime.Elapsed > this.MaxRuntime.Value)
                        {
                            this.lifecycle.OnNext(LifeCycles.MaxRuntimeReached);
                            break;
                        }

                        this.mainAction?.Invoke(this);

                        if (!this.cancelRequested)
                            this.lifecycle.OnNext(LifeCycles.IterationCompleted);
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is OperationCanceledException))
                        log.Debug(ex, "Exception when executing subroutine/mainAction");
                }

                if (this.cancelRequested)
                    this.lifecycle.OnNext(LifeCycles.CancelRequested);
                else
                    this.lifecycle.OnNext(LifeCycles.Completed);

                this.lifecycle.OnNext(LifeCycles.CancelPending);
                Executor.Current.WaitForManagedTasks(true);

                this.lifecycle.OnNext(LifeCycles.Teardown);
                this.tearDownAction?.Invoke(this);

                Executor.AsyncLocalTokens.Value = null;
                //CallContext.LogicalSetData("TOKEN", null);
                Release();

                this.lifecycle.OnNext(LifeCycles.Stopped);

                if (cancelToken.IsCancellationRequested)
                    this.log.Information("SequenceJob {0} canceled and stopped", Name);
                else
                    this.log.Information("SequenceJob {0} completed", Name);
            }
        }

        public TimeSpan Runtime
        {
            get
            {
                return this.runtime.Elapsed;
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

        public int IterationCounter
        {
            get => this.iterationCounter;
        }

        public Task Run()
        {
            Task task;
            Executor.Current.Execute(this, out task);

            return task;
        }

        public Task Run(out System.Threading.CancellationTokenSource cts)
        {
            cts = Executor.Current.Execute(this, out Task task);

            return task;
        }

        public void Stop()
        {
            this.cancelRequested = true;
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
