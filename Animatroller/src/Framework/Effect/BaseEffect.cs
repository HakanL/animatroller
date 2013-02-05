using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public BaseSweeperEffect(string name, TimeSpan sweepDuration, int dataPoints, bool startRunning)
        {
            this.name = name;
            Executor.Current.Register(this);

            this.devices = new List<T>();
            this.sweeper = new Sweeper(sweepDuration, dataPoints, startRunning);

            this.sweeper.RegisterJob((zeroToOne, negativeOneToOne, oneToZeroToOne, forced, totalTicks) =>
                {
                    if (forced)
                    {
                        lock (lockObject)
                        {
                            foreach (var device in this.devices)
                                StopDevice(device);
                        }
                    }
                    else
                    {
                        if (Monitor.TryEnter(lockObject))
                        {
                            try
                            {
                                var totalWatch = System.Diagnostics.Stopwatch.StartNew();

                                var watches = new System.Diagnostics.Stopwatch[this.devices.Count];
                                for(int i = 0; i < this.devices.Count; i++)
                                {
                                    watches[i] = System.Diagnostics.Stopwatch.StartNew();

                                    ExecutePerDevice(this.devices[i], zeroToOne, negativeOneToOne, oneToZeroToOne);

                                    watches[i].Stop();
                                }
                                totalWatch.Stop();

                                double max = watches.Select(x => x.ElapsedMilliseconds).Max();
                                double avg = watches.Select(x => x.ElapsedMilliseconds).Average();

                                if (totalWatch.ElapsedMilliseconds > 25)
                                {
                                    log.Info(string.Format("Devices {0}   Max: {1:N1}   Avg: {2:N1}   Total: {3:N1}",
                                        this.devices.Count, max, avg, totalWatch.ElapsedMilliseconds));
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
                            log.Info("Missed Job in BaseSweepEffect   Name: " + Name);
                    }

                });
        }

        // Generate sweeper with 50 ms interval
        public BaseSweeperEffect(string name, TimeSpan sweepDuration, bool startRunning)
            : this(name, sweepDuration, (int)(
            sweepDuration.TotalMilliseconds > 500 ? sweepDuration.TotalMilliseconds / 50 : sweepDuration.TotalMilliseconds / 25
            ), startRunning)
        {
        }

        protected abstract void ExecutePerDevice(T device, double zeroToOne, double negativeOneToOne, double oneToZeroToOne);
        protected abstract void StopDevice(T device);

        public BaseSweeperEffect<T> SetPriority(int priority)
        {
            this.priority = priority;

            return this;
        }

        public IEffect Start()
        {
            this.sweeper.Resume();

            return this;
        }

        public IEffect Stop()
        {
            this.sweeper.Pause();

            this.sweeper.ForceValue(0, 0, 0, 0);

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
    }

    public abstract class BaseEffect<T> : IEffect
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected int priority;
        protected string name;
        protected object lockObject = new object();
        protected Timer timer;
        protected List<T> devices;
        private TimeSpan interval;

        public BaseEffect(string name, TimeSpan interval)
        {
            this.name = name;
            Executor.Current.Register(this);

            this.interval = interval;
            this.devices = new List<T>();
            this.timer = new Timer(new TimerCallback(TimerCallback));

            Start();
        }

        private void TimerCallback(object state)
        {
            if (Monitor.TryEnter(lockObject))
            {
                try
                {
                    foreach (var device in this.devices)
                        ExecutePerDevice(device);
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
                log.Error("Missed ExecutePerDevice in BaseEffect");
        }

        protected abstract void ExecutePerDevice(T device);

        public IEffect Start()
        {
            this.timer.Change(TimeSpan.FromMilliseconds(0), this.interval);

            return this;
        }

        public IEffect Stop()
        {
            this.timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            return this;
        }

        public BaseEffect<T> SetPriority(int priority)
        {
            this.priority = priority;

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

        public BaseEffect<T> AddDevice(T device)
        {
            lock (lockObject)
            {
                this.devices.Add(device);
            }

            return this;
        }

        public BaseEffect<T> RemoveDevice(T device)
        {
            lock (lockObject)
            {
                if (this.devices.Contains(device))
                    this.devices.Remove(device);
            }
            return this;
        }
    }
}
