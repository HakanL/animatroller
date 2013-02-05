using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Effect
{
    public class Sweeper
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        private object lockObject = new object();
        private object lockJobs = new object();
        private Timer timer;
        private int index1;
        private int index2;
        private int index3;
        private int positions;
        private List<EffectAction.Action> jobs;
        private TimeSpan interval;
        private bool oneShot;
        private int hitCounter;
        private long ticks;

        public Sweeper(TimeSpan duration, int dataPoints, bool startRunning)
        {
            if (dataPoints < 2)
                throw new ArgumentOutOfRangeException("dataPoints");

            this.positions = dataPoints;
            InternalReset();
            this.jobs = new List<EffectAction.Action>();
            this.timer = new Timer(new TimerCallback(TimerCallback));

            this.interval = new TimeSpan(duration.Ticks / dataPoints);
            log.Debug("Interval {0:N1} ms", this.interval.TotalMilliseconds);

            if(startRunning)
                Resume();
        }

        public Sweeper OneShot()
        {
            this.oneShot = true;

            return this;
        }

        public Sweeper Pause()
        {
            this.timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            return this;
        }

        public Sweeper Resume()
        {
            this.timer.Change(TimeSpan.FromMilliseconds(0), this.interval);

            return this;
        }

        private void InternalReset()
        {
            this.hitCounter = 0;
            this.index1 = 0;
            this.index2 = positions / 4;
            this.index3 = positions / 2;
            this.ticks = 0;
        }

        public Sweeper Reset()
        {
            InternalReset();

            Resume();

            return this;
        }

        public Sweeper ForceValue(double zeroToOne, double negativeOneToOne, double oneToZeroToOne, long totalTicks)
        {
            lock (lockJobs)
            {
                foreach (var job in jobs)
                    job(zeroToOne, negativeOneToOne, oneToZeroToOne, true, totalTicks);
            }

            return this;
        }

        public Sweeper RegisterJob(EffectAction.Action job)
        {
            lock (lockJobs)
            {
                jobs.Add(job);
            }

            return this;
        }

        private void TimerCallback(object state)
        {
            double value1;
            double value2;
            double value3;
            long valueTicks;

            lock (lockObject)
            {
                value1 = SweeperTables.DataValues1[SweeperTables.GetScaledIndex(index1, positions + 1)];
                value2 = SweeperTables.DataValues2[SweeperTables.GetScaledIndex(index2, positions + 1)];
                value3 = SweeperTables.DataValues3[SweeperTables.GetScaledIndex(index3, positions + 1)];
                valueTicks = ticks;

                if (++index1 >= positions)
                    index1 = 0;
                if (++index2 >= positions)
                    index2 = 0;
                if (++index3 >= positions)
                    index3 = 0;

                ticks++;

                if (++hitCounter >= positions && this.oneShot)
                    Pause();
            }

            if (Monitor.TryEnter(lockJobs))
            {
                try
                {
                    foreach (var job in jobs)
                        job(value1, value2, value3, false, valueTicks);
                }
                catch (Exception ex)
                {
                    log.Error("Exception in Sweeper job" + ex.ToString());
                }
                finally
                {
                    Monitor.Exit(lockJobs);
                }
            }
            else
                log.Warn("Missed execute task in Sweeper job");
        }
    }
}
