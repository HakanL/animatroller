using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NLog;

namespace Animatroller.Framework
{
    public class Executor
    {
        internal const int MasterTimerIntervalMs = 25;

        protected static Logger log = LogManager.GetCurrentClassLogger();

        public class ExecuteInstance
        {
            public Task Task;
            public ICanExecute Instance;
            public CancellationTokenSource CancelSource;
        }

        private object lockObject = new object();
        public static readonly Executor Current = new Executor();
        private Dictionary<ICanExecute, Task> singleInstanceTasks;
        private List<IDevice> devices;
        private List<IRunnable> runnable;
        private List<IScene> scenes;
        private List<Animatroller.Framework.Effect.IEffect> effects;
        private List<ExecuteInstance> executingTasks;
        private Dictionary<Guid, Tuple<CancellationTokenSource, Task, string>> cancellable;
        private Controller.HighPrecisionTimer masterTimer;
        private Effect.MasterSweeper masterSweeper;

        public Executor()
        {
            this.singleInstanceTasks = new Dictionary<ICanExecute, Task>();
            this.devices = new List<IDevice>();
            this.runnable = new List<IRunnable>();
            this.effects = new List<Effect.IEffect>();
            this.scenes = new List<IScene>();
            this.executingTasks = new List<ExecuteInstance>();
            this.cancellable = new Dictionary<Guid, Tuple<CancellationTokenSource, Task, string>>();
            // Create timer for 25 ms interval (40 hz) for fades, effects, etc
            this.masterTimer = new Controller.HighPrecisionTimer(MasterTimerIntervalMs);
            this.masterSweeper = new Effect.MasterSweeper(this.masterTimer);
        }

        public Executor Register(IDevice device)
        {
            if (this.devices.Contains(device))
                throw new ArgumentException("Already registered");

            this.devices.Add(device);

            return this;
        }

        public Effect.MasterSweeper.Job RegisterSweeperJob(Effect.EffectAction.Action action, TimeSpan oneSweepDuration, int? iterations)
        {
            return this.masterSweeper.RegisterJob(action, oneSweepDuration, iterations);
        }

        public Executor Register(Animatroller.Framework.Effect.IEffect device)
        {
            if (this.effects.Contains(device))
                throw new ArgumentException("Already registered");

            this.effects.Add(device);

            return this;
        }

        public Executor Register(IRunnable runnable)
        {
            if (this.runnable.Contains(runnable))
                throw new ArgumentException("Already registered");

            this.runnable.Add(runnable);

            return this;
        }

        public Executor Register(IScene scene)
        {
            if (this.scenes.Contains(scene))
                throw new ArgumentException("Already registered");

            this.scenes.Add(scene);

            return this;
        }

        public Executor Start()
        {
            foreach (var runnable in this.runnable)
                runnable.Start();

            foreach (var scene in this.scenes)
                scene.Start();

            return this;
        }

        public Executor Run()
        {
            foreach (var device in this.devices)
                device.StartDevice();

            foreach (var scene in this.scenes)
                scene.Run();

            return this;
        }

        public Executor Stop()
        {
            foreach (var effect in this.effects)
                effect.Stop();

            lock (lockObject)
            {
                foreach (var cancel in this.cancellable.Values)
                    cancel.Item1.Cancel();
            }

            foreach (var scene in this.scenes)
                scene.Stop();

            foreach (var runnable in this.runnable)
                runnable.Stop();

            return this;
        }

        public bool EverythingStopped()
        {
            lock (lockObject)
            {
                foreach (var cancel in this.cancellable.Values)
                    if (!cancel.Item2.IsCompleted)
                        return false;
            }

            return true;
        }

        public Executor WaitToStop(int milliseconds)
        {
            var waitTasks = new List<Task>();
            lock (lockObject)
            {
                foreach (var cancel in this.cancellable.Values)
                {
                    waitTasks.Add(cancel.Item2);
                }
            }

            if (!Task.WaitAll(waitTasks.ToArray(), milliseconds))
                log.Error("At least one job failed to complete in time when asked to cancel");

            return this;
        }

        internal CancellationTokenSource Execute(Action<CancellationToken> action, string name, out Task task)
        {
            var tokenSource = new CancellationTokenSource();
            var cancelToken = tokenSource.Token;
            Guid taskId = Guid.NewGuid();
            task = Task.Factory.StartNew(x => action.Invoke(cancelToken), null, cancelToken, TaskCreationOptions.LongRunning,
                System.Threading.Tasks.TaskScheduler.Current);

            lock (lockObject)
            {
                this.cancellable.Add(taskId, new Tuple<CancellationTokenSource, Task, string>(tokenSource, task, name));
            }

            task.ContinueWith(x =>
                {
                    lock (lockObject)
                    {
                        this.cancellable.Remove(taskId);
                    }
                });

            return tokenSource;
        }

        private void RemoveExecutingTask(ICanExecute value)
        {
            lock (executingTasks)
            {
                foreach (var execInstance in this.executingTasks.ToList())
                {
                    if (execInstance.Instance == value)
                    {
                        this.executingTasks.Remove(execInstance);
                    }
                }
            }
        }

        public void Cancel(ICanExecute jobToCancel)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var waitTasks = new List<Task>();
            lock (executingTasks)
            {
                foreach (var execInstance in this.executingTasks)
                {
                    if (execInstance.Instance == jobToCancel)
                    {
                        waitTasks.Add(execInstance.Task);
                        execInstance.CancelSource.Cancel();
                    }
                }
            }
            try
            {
                Task.WaitAll(waitTasks.ToArray());
            }
            catch
            {
            }
            watch.Stop();
            log.Info("Waited {1:N1}ms for job {0} to stop from cancel", jobToCancel.Name, watch.Elapsed.TotalMilliseconds);
        }

        private void CleanupCompletedTasks()
        {
            lock (singleInstanceTasks)
            {
                var tasksToDelete = new List<ICanExecute>();
                foreach (var kvp in singleInstanceTasks)
                {
                    if (kvp.Value.IsCanceled || kvp.Value.IsCompleted || kvp.Value.IsFaulted)
                        tasksToDelete.Add(kvp.Key);
                }

                foreach (var obj in tasksToDelete)
                    singleInstanceTasks.Remove(obj);
            }
        }

        public CancellationTokenSource Execute(ICanExecute value)
        {
            CleanupCompletedTasks();
            
            CancellationTokenSource cancelSource;
            Task task;

            if (!value.IsMultiInstance)
            {
                lock (singleInstanceTasks)
                {
                    if (singleInstanceTasks.ContainsKey(value))
                    {
                        // Already running
                        log.Info("Single instance already running, skipping");
                        return null;
                    }

                    cancelSource = Execute(cancelToken =>
                    {
                        value.Execute(cancelToken);

                        RemoveExecutingTask(value);
                    }, value.Name, out task);

                    singleInstanceTasks.Add(value, task);
                }
            }
            else
            {
                cancelSource = Execute(cancelToken =>
                {
                    value.Execute(cancelToken);

                    RemoveExecutingTask(value);
                }, value.Name, out task);
            }

            lock (executingTasks)
            {
                executingTasks.Add(new ExecuteInstance
                {
                    CancelSource = cancelSource,
                    Instance = value,
                    Task = task
                });
            }

            return cancelSource;
        }
    }
}
