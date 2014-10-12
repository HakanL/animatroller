using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reactive.Subjects;
using NLog;

namespace Animatroller.Framework.Effect
{
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
                log.Warn("Missed ExecutePerDevice in BaseEffect");
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
