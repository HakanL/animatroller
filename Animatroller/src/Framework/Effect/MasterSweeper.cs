using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Effect
{
    public class MasterSweeper
    {
        public class Job
        {
            private object lockObject = new object();
            private EffectAction.Action action { get; set; }
            private long ticks;
            private readonly int offset1;
            private readonly int offset2;
            private readonly int offset3;
            private readonly int intervalMs;
            private double index;
            private bool running;
            private int? iterationCounter;
            private double step;
            private double value1;
            private double value2;
            private double value3;
            private long valueTicks;
            private ManualResetEvent iterationsComplete;

            internal Job(EffectAction.Action action, TimeSpan oneSweepDuration, int intervalMs, int? iterations)
            {
                this.action = action;
                this.intervalMs = intervalMs;
                this.iterationCounter = iterations;
                this.offset1 = 0;
                this.offset2 = SweeperTables.DataPoints / 4;
                this.offset3 = SweeperTables.DataPoints / 2;

                this.iterationsComplete = new ManualResetEvent(false);

                SetDuration(oneSweepDuration);
            }

            public Job Reset(EffectAction.Action action, TimeSpan oneSweepDuration, int? iterations)
            {
                lock (lockObject)
                {
                    this.action = action;
                    this.step = (double)SweeperTables.DataPoints / (1000 / (double)intervalMs) / oneSweepDuration.TotalSeconds;
                    this.iterationCounter = iterations;
                }

                return this;
            }

            public WaitHandle IterationsCompleteWaitHandle
            {
                get { return this.iterationsComplete; }
            }

            public Job SetDuration(TimeSpan oneSweepDuration)
            {
                lock (lockObject)
                {
                    this.step = (double)SweeperTables.DataPoints / (1000 / (double)intervalMs) / oneSweepDuration.TotalSeconds;
                }

                return this;
            }

            public Job ChangeAction(EffectAction.Action action)
            {
                lock (lockObject)
                {
                    this.action = action;
                }

                return this;
            }

            public Job Restart(TimeSpan oneSweepDuration)
            {
                lock (lockObject)
                {
                    this.running = false;
                    this.index = 0;
                    this.ticks = 0;
                }

                SetDuration(oneSweepDuration);

                Resume();

                return this;
            }

            public Job Restart()
            {
                lock (lockObject)
                {
                    this.index = 0;
                    this.ticks = 0;
                }

                Resume();

                return this;
            }

            public Job Pause()
            {
                lock (lockObject)
                {
                    this.running = false;
                }

                return this;
            }

            public Job Resume()
            {
                lock (lockObject)
                {
                    this.iterationsComplete.Reset();
                    this.running = true;
                }

                return this;
            }

            public Job Stop()
            {
                lock (lockObject)
                {
                    Pause();

                    this.action.Invoke(this.value1, this.value2, this.value3, true, this.valueTicks, true);
                }

                return this;
            }

            public Job Wait()
            {
                // Wait for iterations to be completed
                this.IterationsCompleteWaitHandle.WaitOne();

                return this;
            }

            public Job SetIterations(int? iterations)
            {
                if (iterations.GetValueOrDefault() < 1)
                    throw new ArgumentOutOfRangeException();

                lock (lockObject)
                {
                    this.iterationCounter = iterations;
                }

                return this;
            }

            private void SetValues()
            {
                int index1 = ((int)this.index + this.offset1) % SweeperTables.DataPoints;
                int index2 = ((int)this.index + this.offset2) % SweeperTables.DataPoints;
                int index3 = ((int)this.index + this.offset3) % SweeperTables.DataPoints;

                this.value1 = SweeperTables.DataValues1[index1];
                this.value2 = SweeperTables.DataValues2[index2];
                this.value3 = SweeperTables.DataValues3[index3];

                this.valueTicks = this.ticks;
            }

            internal void Tick()
            {
                if (!this.running)
                    return;

                lock (lockObject)
                {
                    SetValues();

                    this.index += this.step;
                    this.ticks++;

                    if ((int)this.index >= SweeperTables.DataPoints)
                    {
                        this.index = 0;
                        this.ticks = 0;

                        if (this.iterationCounter.HasValue)
                        {
                            this.iterationCounter = this.iterationCounter.Value - 1;
                            if(this.iterationCounter.Value <= 0)
                            {
                                // Set to last position
                                this.index = SweeperTables.DataPoints - 1;
                                SetValues();

                                Stop();

                                this.iterationsComplete.Set();
                            }
                        }
                    }
                }
            }

            internal void Execute()
            {
                if (!this.running)
                    return;

                lock (lockObject)
                {
                    this.action.Invoke(this.value1, this.value2, this.value3, false, this.valueTicks, false);
                }
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();

        private object lockTicks = new object();
        private object lockJobs = new object();
        private List<Job> jobs;
        private readonly int intervalMs;

        public MasterSweeper(Controller.HighPrecisionTimer timer)
        {
            this.intervalMs = timer.IntervalMs;
            this.jobs = new List<Job>();

            // Make sure we don't add to the Tick handler until we're ready to execute
            timer.Tick += timer_Tick;
        }

        private void timer_Tick(object sender, Controller.HighPrecisionTimer.TickEventArgs e)
        {
            lock (lockTicks)
            {
                foreach (var job in jobs)
                    job.Tick();
            }

            if (Monitor.TryEnter(lockJobs))
            {
                try
                {
                    foreach (var job in jobs)
                        job.Execute();
                }
                catch (Exception ex)
                {
                    log.Error("Exception in MasterSweeper job", ex);
                }
                finally
                {
                    Monitor.Exit(lockJobs);
                }
            }
            else
                log.Warn("Missed execute task in MasterSweeper job");
        }

        public MasterSweeper.Job RegisterJob(EffectAction.Action action, TimeSpan oneSweepDuration, int? iterations)
        {
            var job = new MasterSweeper.Job(action, oneSweepDuration, this.intervalMs, iterations);

            lock (lockTicks)
            {
                lock (lockJobs)
                {
                    jobs.Add(job);
                }
            }

            return job;
        }
    }
}
