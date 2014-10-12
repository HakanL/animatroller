using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reactive.Subjects;
using NLog;

namespace Animatroller.Framework.Effect
{
    public abstract class BaseSweeperEffect<T> : IEffect
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected int priority;
        protected string name;
        protected object lockObject = new object();
        protected Sweeper sweeper;
        protected List<T> devices;
        protected List<ISubject<DoubleZeroToOne>> devices2;
        protected bool isRunning;
        protected ISubject<bool> inputRun;

        public BaseSweeperEffect(string name, TimeSpan sweepDuration, int dataPoints, bool startRunning)
        {
            this.name = name;
            Executor.Current.Register(this);

            this.inputRun = new Subject<bool>();

            this.inputRun.Subscribe(x =>
            {
                if (this.isRunning != x)
                {
                    if (x)
                        Start();
                    else
                        Stop();
                }
            });

            this.devices = new List<T>();
            this.devices2 = new List<ISubject<DoubleZeroToOne>>();
            this.sweeper = new Sweeper(sweepDuration, dataPoints, startRunning);

            this.sweeper.RegisterJob((zeroToOne, negativeOneToOne, oneToZeroToOne, forced, totalTicks, final) =>
                {
                    bool isUnlocked = false;
                    if (forced)
                    {
                        Monitor.Enter(lockObject);
                        isUnlocked = true;
                    }
                    else
                        isUnlocked = Monitor.TryEnter(lockObject);

                    if (isUnlocked)
                    {
                        try
                        {
                            var totalWatch = System.Diagnostics.Stopwatch.StartNew();

                            var watches = new System.Diagnostics.Stopwatch[this.devices.Count];
                            for (int i = 0; i < this.devices.Count; i++)
                            {
                                watches[i] = System.Diagnostics.Stopwatch.StartNew();

                                ExecutePerDevice(this.devices[i], zeroToOne, negativeOneToOne, oneToZeroToOne, final);

                                watches[i].Stop();
                            }
                            totalWatch.Stop();

                            for (int i = 0; i < this.devices2.Count; i++)
                            {
                                ExecutePerDevice2(
                                    this.devices2[i],
                                    zeroToOne,
                                    negativeOneToOne,
                                    oneToZeroToOne,
                                    final);
                            }

                            if (watches.Any())
                            {
                                double max = watches.Select(x => x.ElapsedMilliseconds).Max();
                                double avg = watches.Select(x => x.ElapsedMilliseconds).Average();

                                if (totalWatch.ElapsedMilliseconds > 25)
                                {
                                    log.Info(string.Format("Devices {0}   Max: {1:N1}   Avg: {2:N1}   Total: {3:N1}",
                                        this.devices.Count, max, avg, totalWatch.ElapsedMilliseconds));
                                }
                            }
                        }
                        catch
                        {
                        }
                        finally
                        {
                            Monitor.Exit(lockObject);
                        }
                    }
                    else
                        log.Warn("Missed Job in BaseSweepEffect   Name: " + Name);
                });
        }

        // Generate sweeper with 50 ms interval
        public BaseSweeperEffect(string name, TimeSpan sweepDuration, bool startRunning)
            : this(name, sweepDuration, (int)(
            sweepDuration.TotalMilliseconds > 500 ? sweepDuration.TotalMilliseconds / 50 : sweepDuration.TotalMilliseconds / 25
            ), startRunning)
        {
        }

        public ISubject<bool> InputRun
        {
            get
            {
                return this.inputRun;
            }
        }

        protected abstract void ExecutePerDevice(T device, double zeroToOne, double negativeOneToOne, double oneToZeroToOne, bool final);

        protected abstract void ExecutePerDevice2(ISubject<DoubleZeroToOne> device, double zeroToOne, double negativeOneToOne, double oneToZeroToOne, bool final);

        public BaseSweeperEffect<T> SetPriority(int priority)
        {
            this.priority = priority;

            return this;
        }

        public IEffect Start()
        {
            this.sweeper.Resume();
            this.isRunning = true;

            return this;
        }

        public IEffect Restart()
        {
            this.sweeper.Reset();

            return this;
        }

        public IEffect Stop()
        {
            this.sweeper.Pause();

            this.sweeper.ForceValue(0, 0, 0, 0);
            this.isRunning = false;

            return this;
        }

        public string Name
        {
            get { return this.name; }
        }

        public int Priority
        {
            get { return this.priority; }
        }

        public BaseSweeperEffect<T> ConnectTo(ISubject<DoubleZeroToOne> device)
        {
            lock (lockObject)
            {
                this.devices2.Add(device);
            }

            return this;
        }

        public BaseSweeperEffect<T> AddDevice(T device)
        {
            lock (lockObject)
            {
                this.devices.Add(device);
            }

            return this;
        }

        public BaseSweeperEffect<T> RemoveDevice(T device)
        {
            lock (lockObject)
            {
                if (this.devices.Contains(device))
                    this.devices.Remove(device);
            }
            return this;
        }

        public BaseSweeperEffect<T> Disconnect(ISubject<DoubleZeroToOne> device)
        {
            lock (lockObject)
            {
                if (this.devices2.Contains(device))
                    this.devices2.Remove(device);
            }
            return this;
        }
    }
}
