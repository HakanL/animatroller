//#define PROFILE

using NLog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Framework.Utility
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
        protected CancellationTokenSource cancelSource;

        public HighPrecisionTimer(int interval)
        {
            log.Info("Starting HighPrecisionTimer with {0} ms interval", interval);

            if (interval < 1)
                throw new ArgumentOutOfRangeException();
            System.Diagnostics.Trace.Assert(interval >= 10, "Not reliable/tested, may use too much CPU");

            cancelSource = new CancellationTokenSource();

            // Used to report timing accuracy for 1 sec, running total
            tickTiming = new CircularBuffer.CircularBuffer<int>(1000 / interval, true);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            long durationMs = 0;
            long totalTicks = 0;
            long nextStop = interval;
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
                            totalTicks = durationMs / interval;

                            tickTiming.Put((int)(durationMs - nextStop));

#if PROFILE
                            if (durationMs - lastReport >= 1000)
                            {
                                // Report
                                log.Debug("Last second - avg: {0:F1}  best: {1}  worst: {2}",
                                    tickTiming.Average(), tickTiming.Min(), tickTiming.Max());

                                lastReport = durationMs;
                            }
#endif

                            var handler = Tick;
                            if (handler != null)
                                handler(this, new TickEventArgs(TimeSpan.FromMilliseconds(durationMs), totalTicks));

                            // Calculate when the next stop is. If we're too slow on the trigger then we'll skip ticks
                            nextStop = interval * (watch.ElapsedMilliseconds / interval + 1);
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
    }
}
