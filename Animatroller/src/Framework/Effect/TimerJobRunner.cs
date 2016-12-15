using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reactive.Subjects;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Effect2
{
    public class TimerJobRunner
    {
        protected abstract class TimerJob
        {
            private long lastDuration;
            private DateTime lastRun;

            public TimeSpan SinceLastRun
            {
                get { return DateTime.Now - this.lastRun; }
            }

            public bool Running { get; private set; }

            public long? DurationMs { get; private set; }

            public long? EndDurationMs { get; private set; }

            public long StartDurationMs { get; private set; }

            private CancellationTokenSource cancelSource;

            protected CancellationTokenSource Init(long? durationMs, long startDurationMs)
            {
                this.DurationMs = durationMs;

                this.StartDurationMs = startDurationMs;
                if (durationMs.HasValue)
                    this.EndDurationMs = startDurationMs + durationMs;

                this.lastDuration = -1;
                this.Running = true;
                this.lastRun = DateTime.Now;

                this.cancelSource = new CancellationTokenSource();

                // Start
                Update(startDurationMs);

                return this.cancelSource;
            }

            public void Update(long elapsedMs)
            {
                long currentElapsedMs = elapsedMs - this.StartDurationMs;

                if (!this.DurationMs.HasValue || currentElapsedMs <= this.DurationMs)
                {
                    ObserverNext(currentElapsedMs);

                    this.lastDuration = currentElapsedMs;
                }
                else
                    ObserverNext(this.DurationMs.Value);

                if (elapsedMs >= EndDurationMs || this.cancelSource.IsCancellationRequested)
                {
                    this.Running = false;

                    ObserverCompleted();

                    return;
                }
            }

            protected abstract void ObserverNext(long elapsedMs);

            protected abstract void ObserverCompleted();
        }

        protected class TimerJobMs : TimerJob
        {
            private IObserver<long> observer;

            public CancellationTokenSource Init(IObserver<long> observer, long? durationMs, long startDurationMs)
            {
                this.observer = observer;

                return base.Init(durationMs, startDurationMs);
            }

            protected override void ObserverNext(long elapsedMs)
            {
                this.observer.OnNext(elapsedMs);
            }

            protected override void ObserverCompleted()
            {
                this.observer.OnCompleted();
            }
        }

        protected class TimerJobPos : TimerJob
        {
            private IObserver<double> observer;

            public CancellationTokenSource Init(IObserver<double> observer, long durationMs, long startDurationMs)
            {
                this.observer = observer;

                return base.Init(durationMs, startDurationMs);
            }

            protected override void ObserverNext(long elapsedMs)
            {
                double pos;

                if (this.DurationMs == 0)
                    pos = 1.0;
                else
                    pos = (double)elapsedMs / (double)this.DurationMs;

                this.observer.OnNext(pos);
            }

            protected override void ObserverCompleted()
            {
                this.observer.OnCompleted();
            }
        }

        protected class TimerJobCounter : TimerJob
        {
            private IObserver<int> observer;
            private int mainCounter;
            private int skips;
            private int skipCounter;

            public CancellationTokenSource Init(IObserver<int> observer, long? durationMs, long startDurationMs, int skips)
            {
                this.observer = observer;
                this.skips = skips;

                return base.Init(durationMs, startDurationMs);
            }

            protected override void ObserverNext(long elapsedMs)
            {
                if (++this.skipCounter == this.skips)
                {
                    this.skipCounter = 0;

                    this.observer.OnNext(this.mainCounter);

                    this.mainCounter++;
                }
            }

            protected override void ObserverCompleted()
            {
                this.observer.OnCompleted();
            }
        }

        private object lockObject = new object();
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private List<TimerJob> timerJobs = new List<TimerJob>();
        private Animatroller.Framework.Controller.HighPrecisionTimer2 timer;
        private Timer monitorTimer;

        public TimerJobRunner(Animatroller.Framework.Controller.HighPrecisionTimer2 timer)
        {
            this.timer = timer;

            this.timer.Output.Subscribe(x =>
                {
                    lock (this.lockObject)
                    {
                        foreach (var timerJob in this.timerJobs)
                        {
                            if (timerJob.Running)
                                timerJob.Update(this.timer.ElapsedMs);
                        }
                    }
                });

            this.monitorTimer = new Timer(state =>
            {
                var timeout = TimeSpan.FromMinutes(5);

                int runningTimers = 0;
                int totalTimers = 0;
                int deletedTimers = 0;

                lock (this.lockObject)
                {
                    var deleteList = new List<TimerJob>();

                    foreach (var timerJob in this.timerJobs)
                    {
                        totalTimers++;

                        if (timerJob.Running)
                            runningTimers++;
                        else if (timerJob.SinceLastRun > timeout)
                        {
                            deleteList.Add(timerJob);
                            deletedTimers++;
                        }
                    }

                    foreach (var deleteJob in deleteList)
                        this.timerJobs.Remove(deleteJob);
                }

                log.Debug("{0} running jobs, out of {1}. {2} deleted", runningTimers, totalTimers, deletedTimers);
            }, null, 1000, 30000);
        }

        private CancellationTokenSource AddTimerJob<T>(
            long? durationMs,
            Action finishAction,
            Func<T, long?, long, CancellationTokenSource> initAction) where T : TimerJob, new()
        {
            if (durationMs <= 5)
            {
                // No need to spin up a job, just set the destination

                finishAction();

                return new CancellationTokenSource();
            }

            lock (this.lockObject)
            {
                T timerJob = default(T);

                foreach (var existingTimerJob in this.timerJobs)
                {
                    if (!existingTimerJob.Running && existingTimerJob is T)
                    {
                        // Reuse
                        timerJob = (T)existingTimerJob;
                        break;
                    }
                }

                if (timerJob == null)
                {
                    timerJob = new T();
                    this.timerJobs.Add(timerJob);

                    log.Debug("Total {0} timer jobs", this.timerJobs.Count);
                }

                return initAction(timerJob, durationMs, this.timer.ElapsedMs);
            }
        }

        public CancellationTokenSource AddTimerJobMs(IObserver<long> observer, long? durationMs)
        {
            return AddTimerJob<TimerJobMs>(
                durationMs: durationMs,
                finishAction: new Action(() =>
                    {
                        observer.OnCompleted();
                    }),
                initAction: (job, dMs, startDurationMs) => job.Init(observer, dMs, startDurationMs));
        }

        public CancellationTokenSource AddTimerJobPos(IObserver<double> observer, long durationMs)
        {
            return AddTimerJob<TimerJobPos>(
                durationMs: durationMs,
                finishAction: new Action(() =>
                {
                    observer.OnNext(1.0);
                    observer.OnCompleted();
                }),
                initAction: (job, dMs, startDurationMs) => job.Init(observer, dMs.Value, startDurationMs));
        }

        public CancellationTokenSource AddTimerJobCounter(IObserver<int> observer, int skips = 0)
        {
            return AddTimerJob<TimerJobCounter>(
                durationMs: null,
                finishAction: observer.OnCompleted,
                initAction: (job, dMs, startDurationMs) => job.Init(observer, dMs, startDurationMs, skips));
        }
    }
}
