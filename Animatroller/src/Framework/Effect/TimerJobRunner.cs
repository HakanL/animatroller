using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Effect2
{
    public class TimerJobRunner
    {
        protected class TimerJob
        {
            private long lastDuration;

            public bool Running { get; private set; }

            public IObserver<long> Observer { get; private set; }

            public long DurationMs { get; private set; }

            public long EndDurationMs { get; private set; }

            public long StartDurationMs { get; private set; }

            public void Init(IObserver<long> observer, long durationMs, long startDurationMs)
            {
                this.Observer = observer;
                this.DurationMs = durationMs;

                this.StartDurationMs = startDurationMs;
                this.EndDurationMs = startDurationMs + durationMs;

                this.lastDuration = -1;
                this.Running = true;

                // Start
                Update(startDurationMs);
            }

            public void Update(long elapsedMs)
            {
                if (elapsedMs >= EndDurationMs)
                {
                    if (this.lastDuration != this.DurationMs)
                        this.Observer.OnNext(this.DurationMs);

                    this.Running = false;

                    this.Observer.OnCompleted();

                    return;
                }

                long currentElapsedMs = elapsedMs - this.StartDurationMs;

                this.Observer.OnNext(currentElapsedMs);
                this.lastDuration = currentElapsedMs;
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        private List<TimerJob> timerJobs = new List<TimerJob>();
        private Animatroller.Framework.Controller.HighPrecisionTimer2 timer;

        public TimerJobRunner(Animatroller.Framework.Controller.HighPrecisionTimer2 timer)
        {
            this.timer = timer;

            this.timer.Output.Subscribe(x =>
                {
                    lock (this.timerJobs)
                    {
                        foreach (var timerJob in this.timerJobs)
                        {
                            if (timerJob.Running)
                                timerJob.Update(this.timer.ElapsedMs);
                        }
                    }
                });
        }

        public void AddTimerJob(IObserver<long> observer, long durationMs)
        {
            lock (this.timerJobs)
            {
                TimerJob timerJob = null;

                foreach (var existingTimerJob in this.timerJobs)
                {
                    if (!existingTimerJob.Running)
                    {
                        // Reuse
                        timerJob = existingTimerJob;
                        break;
                    }
                }

                if (timerJob == null)
                {
                    timerJob = new TimerJob();
                    this.timerJobs.Add(timerJob);

                    log.Debug("Total {0} timer jobs", this.timerJobs.Count);
                }

                timerJob.Init(observer, durationMs, this.timer.ElapsedMs);
            }
        }
    }
}
