using NLog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Framework.Controller
{
    public class HighPrecisionTimer : IDisposable
    {
        public class TickEventArgs : EventArgs
        {
            public TimeSpan Duration { get; private set; }
            public long TotalTicks { get; private set; }

            public TickEventArgs(TimeSpan totalDuration, long totalTicks)
            {
                this.Duration = totalDuration;
                this.TotalTicks = totalTicks;
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        public event EventHandler<TickEventArgs> Tick;
        protected CircularBuffer.CircularBuffer<int> tickTiming;
        protected CircularBuffer.CircularBuffer<long> execTiming;
        protected CancellationTokenSource cancelSource;

        public HighPrecisionTimer(int intervalMs)
        {
            log.Info("Starting HighPrecisionTimer with {0} ms interval", intervalMs);

            if (intervalMs < 1)
                throw new ArgumentOutOfRangeException();
            System.Diagnostics.Trace.Assert(intervalMs >= 10, "Not reliable/tested, may use too much CPU");

            this.IntervalMs = intervalMs;
            cancelSource = new CancellationTokenSource();

#if PROFILE
            // Used to report timing accuracy for 1 sec, running total
            tickTiming = new CircularBuffer.CircularBuffer<int>(1000 / intervalMs, true);
            execTiming = new CircularBuffer.CircularBuffer<long>(1000 / intervalMs, true);
#endif

            var watch = System.Diagnostics.Stopwatch.StartNew();
            long durationMs = 0;
            long totalTicks = 0;
            long nextStop = intervalMs;
#if PROFILE
            long lastReport = 0;
#endif

            var task = new Task(() =>
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        long msLeft = nextStop - watch.ElapsedMilliseconds;
                        if (msLeft <= 0)
                        {
                            durationMs = watch.ElapsedMilliseconds;
                            totalTicks = durationMs / intervalMs;

#if PROFILE
                            var execWatch = System.Diagnostics.Stopwatch.StartNew();
#endif
                            var handler = Tick;
                            if (handler != null)
                                handler(this, new TickEventArgs(TimeSpan.FromMilliseconds(durationMs), totalTicks));
#if PROFILE
                            execWatch.Stop();
                            execTiming.Put(execWatch.ElapsedTicks);
                            tickTiming.Put((int)(durationMs - nextStop));

                            if (durationMs - lastReport >= 1000)
                            {
                                // Report
                                log.Debug("HighPTimer  avg: {0:F1}  best: {1}  worst: {2}   MaxExec: {3:N1}ms",
                                    tickTiming.Average(), tickTiming.Min(), tickTiming.Max(),
                                    TimeSpan.FromTicks(execTiming.Max()).TotalMilliseconds);

                                lastReport = durationMs;
                            }
#endif

                            // Calculate when the next stop is. If we're too slow on the trigger then we'll skip ticks
                            nextStop = intervalMs * (watch.ElapsedMilliseconds / intervalMs + 1);
                        }
                        else if (msLeft < 16)
                        {
                            System.Threading.SpinWait.SpinUntil(() => watch.ElapsedMilliseconds >= nextStop);
                            continue;
                        }

                        System.Threading.Thread.Sleep(1);
                    }
                }, cancelSource.Token, TaskCreationOptions.LongRunning);

            task.Start();
        }

        public void Dispose()
        {
            this.cancelSource.Cancel();
        }

        public int IntervalMs { get; private set; }
    }
}
