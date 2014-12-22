using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reactive.Subjects;
using NLog;
using System.IO;

namespace Animatroller.Framework
{
    public class Executor
    {
        public class ThreadLocalStorage
        {
            public Dictionary<IOwnedDevice, IControlToken> ControlTokens { get; private set; }

            public List<Tuple<Task, CancellationTokenSource>> ManagedTasks { get; private set; }

            public ThreadLocalStorage()
            {
                this.ControlTokens = new Dictionary<IOwnedDevice, IControlToken>();
                this.ManagedTasks = new List<Tuple<Task, CancellationTokenSource>>();
            }
        }

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
        private List<IRunningDevice> devices;
        private List<IRunnable> runnable;
        private List<IScene> scenes;
        private List<Animatroller.Framework.Effect.IEffect> effects;
        private List<ExecuteInstance> executingTasks;
        private Dictionary<Guid, Tuple<CancellationTokenSource, Task, string>> cancellable;
        private Dictionary<Task, CancellationTokenSource> cancelSourceForManagedTask;
        private Controller.HighPrecisionTimer masterTimer;
        private Controller.HighPrecisionTimer2 masterTimer2;
        private Effect2.TimerJobRunner timerJobRunner;
        private Effect2.MasterEffect masterEffect;
        private Effect.MasterSweeper masterSweeper;
        private string keyStoragePath;
        private static ThreadLocal<ThreadLocalStorage> threadStorage;
        private ControlSubject<double> blackout;
        private ControlSubject<double> whiteout;

        private Executor()
        {
            this.singleInstanceTasks = new Dictionary<ICanExecute, Task>();
            this.devices = new List<IRunningDevice>();
            this.runnable = new List<IRunnable>();
            this.effects = new List<Effect.IEffect>();
            this.scenes = new List<IScene>();
            this.executingTasks = new List<ExecuteInstance>();
            this.cancellable = new Dictionary<Guid, Tuple<CancellationTokenSource, Task, string>>();
            this.cancelSourceForManagedTask = new Dictionary<Task, CancellationTokenSource>();
            // Create timer for 25 ms interval (40 hz) for fades, effects, etc
            this.masterTimer = new Controller.HighPrecisionTimer(MasterTimerIntervalMs);
            this.masterTimer2 = new Controller.HighPrecisionTimer2(MasterTimerIntervalMs);
            this.timerJobRunner = new Effect2.TimerJobRunner(this.masterTimer2);

            this.masterEffect = new Effect2.MasterEffect(this.timerJobRunner);

            this.masterSweeper = new Effect.MasterSweeper(this.masterTimer);
            this.keyStoragePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Animatroller");
            if (!System.IO.Directory.Exists(this.keyStoragePath))
                System.IO.Directory.CreateDirectory(this.keyStoragePath);

            this.blackout = new ControlSubject<double>(0.0);
            this.whiteout = new ControlSubject<double>(0.0);

            threadStorage = new ThreadLocal<ThreadLocalStorage>(() => new ThreadLocalStorage());
        }

        public ISubjectWithValue<double> Blackout
        {
            get { return this.blackout; }
        }

        public ISubjectWithValue<double> Whiteout
        {
            get { return this.whiteout; }
        }

        internal ThreadLocalStorage ThreadStorage { get { return threadStorage.Value; } }

        public IControlToken GetControlToken(IOwnedDevice device)
        {
            IControlToken token;
            ThreadStorage.ControlTokens.TryGetValue(device, out token);

            return token;
        }

        public void SetControlToken(IOwnedDevice device, IControlToken token)
        {
            if (token != null)
                ThreadStorage.ControlTokens[device] = token;
            else
                RemoveControlToken(device);
        }

        public void SetManagedTask(Task task, CancellationTokenSource cancelSource)
        {
            var tuple = Tuple.Create(task, cancelSource);
            ThreadStorage.ManagedTasks.Add(tuple);

            task.ContinueWith(x =>
            {
                ThreadStorage.ManagedTasks.Remove(tuple);
                lock (this.cancelSourceForManagedTask)
                {
                    this.cancelSourceForManagedTask.Remove(task);
                }
            });

            lock (this.cancelSourceForManagedTask)
            {
                this.cancelSourceForManagedTask[task] = cancelSource;
            }
        }

        public void WaitForManagedTasks(bool cancel)
        {
            log.Debug("WaitForManagedTasks...");

            if (cancel)
            {
                // Cancel
                ThreadStorage.ManagedTasks.ForEach(x => x.Item2.Cancel());
            }

            Task.WaitAll(ThreadStorage.ManagedTasks.Select(x => x.Item1).ToArray());

            log.Debug("WaitForManagedTasks...Done");
        }

        public void Sleep(TimeSpan value)
        {
            Thread.Sleep(value);
        }

        public void RemoveControlToken(IOwnedDevice device)
        {
            if (ThreadStorage.ControlTokens.ContainsKey(device))
                ThreadStorage.ControlTokens.Remove(device);
        }

        public Effect2.TimerJobRunner TimerJobRunner { get { return this.timerJobRunner; } }

        public Effect2.MasterEffect MasterEffect { get { return this.masterEffect; } }

        public string KeyStoragePrefix { get; set; }

        public void SetKey(string key, string value)
        {
            BinaryRage.DB.Insert<string>(KeyStoragePrefix + "." + key, value, this.keyStoragePath);
        }

        public string GetKey(string key, string defaultValue, bool storeDefaultIfMissing = false)
        {
            try
            {
                return BinaryRage.DB.Get<string>(KeyStoragePrefix + "." + key, this.keyStoragePath);
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                SetKey(key, defaultValue);

                return defaultValue;
            }
            catch (InvalidDataException)
            {
                SetKey(key, defaultValue);

                return defaultValue;
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                if (storeDefaultIfMissing)
                    SetKey(key, defaultValue);

                return defaultValue;
            }
        }

        internal string GetKey(object typeObject, string subKey, string defaultValue)
        {
            return GetKey(string.Format("{0}.{1}", typeObject.GetType().Name, subKey), defaultValue);
        }

        internal T GetSetKey<T>(object typeObject, string subKey, T defaultValue)
        {
            return GetSetKey(string.Format("{0}.{1}", typeObject.GetType().Name, subKey), defaultValue);
        }

        public T GetSetKey<T>(string key, T defaultValue)
        {
            string value = GetKey(key, defaultValue.ToString(), true);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public Executor Register(IRunningDevice device)
        {
            if (this.devices.Contains(device))
                throw new ArgumentException("Already registered");

            this.devices.Add(device);

            return this;
        }

        internal void LogInfo(string text)
        {
            log.Info(text);
        }

        internal void LogDebug(string text)
        {
            log.Debug(text);
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

            // Set field names
            SetFieldNamesForPersistence(scene);

            return this;
        }

        private void SetFieldNamesForPersistence(object scene)
        {
            var fields = scene.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var persistenceFields = new Dictionary<string, ISupportsPersistence>();

            foreach (var field in fields)
            {
                object fieldValue = field.GetValue(scene);
                if (fieldValue == null)
                    continue;

                ISupportsPersistence supportsPersistence = fieldValue as ISupportsPersistence;
                if (supportsPersistence != null && supportsPersistence.PersistState)
                {
                    string baseKey = field.Name;

                    var getKeyFunc = new Func<string, string, string>((subKey, defaultValue) =>
                        {
                            return GetKey(baseKey + subKey, defaultValue);
                        });

                    supportsPersistence.SetValueFromPersistence(getKeyFunc);

                    persistenceFields.Add(baseKey, supportsPersistence);
                }
            }

            if (persistenceFields.Any())
            {
                // Start task
                Task persistenceTask;
                Execute(x =>
                    {
                        while (!x.IsCancellationRequested)
                        {
                            try
                            {
                                foreach (var kvp in persistenceFields)
                                {
                                    var setKeyFunc = new Action<string, string>((subKey, value) =>
                                        {
                                            SetKey(kvp.Key + subKey, value);
                                        });

                                    kvp.Value.SaveValueToPersistence(setKeyFunc);
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Error("Error in persistence task", ex);
                            }

                            // Execute every second
                            x.WaitHandle.WaitOne(1000);
                        }
                    }, "PersistenceTask", out persistenceTask);
            }
        }

        public Executor Run()
        {
            // First start non-hardware outputs
            foreach (var runnable in this.runnable.Where(x => !(x is IOutputHardware)))
                runnable.Start();

            foreach (var device in this.devices)
                device.SetInitialState();

            // Then start hardware outputs, all devices should have their initial states now
            foreach (var runnable in this.runnable.Where(x => (x is IOutputHardware)))
                runnable.Start();

            foreach (var scene in this.scenes)
                scene.Start();

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

        internal void RegisterCancelSource(CancellationTokenSource tokenSource, Task task, string name)
        {
            lock (lockObject)
            {
                this.cancellable.Add(Guid.NewGuid(), new Tuple<CancellationTokenSource, Task, string>(tokenSource, task, name));
            }
        }

        internal void StopManagedTask(Task task)
        {
            CancellationTokenSource cancelSource;
            lock (this.cancelSourceForManagedTask)
            {
                if (this.cancelSourceForManagedTask.TryGetValue(task, out cancelSource))
                {
                    cancelSource.Cancel();
                }
            }
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
            Task task;

            return Execute(value, out task);
        }

        public void ExecuteAndWait(ICanExecute value)
        {
            Task task;

            Execute(value, out task);

            task.Wait();
        }

        public CancellationTokenSource Execute(ICanExecute value, out Task task)
        {
            CleanupCompletedTasks();

            CancellationTokenSource cancelSource;

            if (!value.IsMultiInstance)
            {
                lock (singleInstanceTasks)
                {
                    if (singleInstanceTasks.TryGetValue(value, out task))
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
