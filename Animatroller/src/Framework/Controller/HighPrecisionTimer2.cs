using NLog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;

namespace Animatroller.Framework.Controller
{
    public class HighPrecisionTimer2 : IDisposable
    {
#if PROFILE
        protected CircularBuffer.CircularBuffer<int> tickTiming;
        protected CircularBuffer.CircularBuffer<long> execTiming;
#endif
        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected CancellationTokenSource cancelSource;
        private ManualResetEvent taskComplete;
        private Task task;
        protected ISubject<long> outputValue;
        private System.Diagnostics.Stopwatch watch;

        public HighPrecisionTimer2(int intervalMs, bool startRunning = true)
        {
            log.Info("Starting HighPrecisionTimer with {0} ms interval", intervalMs);

            this.outputValue = new Subject<long>();

            if (intervalMs < 1)
                throw new ArgumentOutOfRangeException();
            System.Diagnostics.Trace.Assert(intervalMs >= 10, "Not reliable/tested, may use too much CPU");

            this.IntervalMs = intervalMs;
            this.cancelSource = new CancellationTokenSource();
            this.taskComplete = new ManualResetEvent(false);

#if PROFILE
            // Used to report timing accuracy for 1 sec, running total
            tickTiming = new CircularBuffer.CircularBuffer<int>(1000 / intervalMs, true);
            execTiming = new CircularBuffer.CircularBuffer<long>(1000 / intervalMs, true);
#endif

            watch = System.Diagnostics.Stopwatch.StartNew();
            long durationMs = 0;
            long nextStop = intervalMs;
#if PROFILE
            long lastReport = 0;
#endif

            this.task = new Task(() =>
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        long msLeft = nextStop - watch.ElapsedMilliseconds;
                        if (msLeft <= 0)
                        {
                            durationMs = watch.ElapsedMilliseconds;

#if PROFILE
                            var execWatch = System.Diagnostics.Stopwatch.StartNew();
#endif
                            this.outputValue.OnNext(durationMs);

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

                    this.taskComplete.Set();
                }, cancelSource.Token, TaskCreationOptions.LongRunning);

            if (startRunning)
                this.task.Start();
        }

        public long ElapsedMs
        {
            get { return this.watch.ElapsedMilliseconds; }
        }

        public IObservable<long> Output
        {
            get
            {
                return this.outputValue;
            }
        }

        public void Start()
        {
            this.task.Start();
        }

        public void Stop()
        {
            this.cancelSource.Cancel();
            this.task.Wait();
        }

        public void WaitUntilFinished(ISequenceInstance instance)
        {
            WaitHandle.WaitAny(new WaitHandle[] { this.taskComplete, instance.CancelToken.WaitHandle });
            Stop();
        }

        public void Dispose()
        {
            this.cancelSource.Cancel();
        }

        public int IntervalMs { get; private set; }
    }
}
