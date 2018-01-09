//#define PROFILE
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Diagnostics;
using System.Reactive.Linq;
using Collections.Generic;

namespace Animatroller.Framework.Controller
{
    public class HighPrecisionTimer3 : IMasterTimer, IDisposable
    {
        public readonly static double TicksPerMs = Stopwatch.Frequency / 1000.0;

#if PROFILE
        protected CircularBuffer<int> tickTiming;
        protected CircularBuffer<long> execTiming;
#endif
        protected ILogger log;
        protected CancellationTokenSource cancelSource;
        private ManualResetEvent taskComplete;
        private Task task;
        protected ISubject<long> outputValue;
        private Stopwatch masterClock;

        public HighPrecisionTimer3(ILogger logger, int intervalMs, bool startRunning = true)
        {
            this.log = logger;
            this.log.Information("Starting HighPrecisionTimer3 with {0} ms interval", intervalMs);

            this.outputValue = new Subject<long>();

            if (intervalMs < 1)
                throw new ArgumentOutOfRangeException();
            Trace.Assert(intervalMs > 10, "Not reliable/tested, may use too much CPU");

            this.IntervalMs = intervalMs;
            this.cancelSource = new CancellationTokenSource();
            this.taskComplete = new ManualResetEvent(false);

#if PROFILE
            // Used to report timing accuracy for 1 sec, running total
            tickTiming = new CircularBuffer<int>(1000 / intervalMs, true);
            execTiming = new CircularBuffer<long>(1000 / intervalMs, true);
#endif

            this.masterClock = Stopwatch.StartNew();
            long durationMs = 0;
#if PROFILE
            long lastReport = 0;
#endif

            this.task = new Task(() =>
                {
                    long ticksIn1ms = (long)(TicksPerMs * 1);
                    long ticksIn5ms = (long)(TicksPerMs * 5);
                    long ticksIn15ms = (long)(TicksPerMs * 15);

                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        // Calculate when the next stop is. If we're too slow on the trigger then we'll skip ticks
                        long nextStop = (long)(intervalMs * (masterClock.ElapsedMilliseconds / intervalMs + 1) * TicksPerMs);

                        while (true)
                        {
                            long ticksLeft = nextStop - this.masterClock.ElapsedTicks;

                            if (ticksLeft <= 10)
                                break;

                            if (ticksLeft < ticksIn1ms)
                            {
                                Thread.SpinWait(10);
                            }
                            else if (ticksLeft < ticksIn5ms)
                            {
                                Thread.SpinWait(100);
                            }
                            else if (ticksLeft < ticksIn15ms)
                            {
                                Thread.Sleep(1);
                            }
                            else
                            {
                                if (this.cancelSource.IsCancellationRequested)
                                    break;
                                Thread.Sleep(10);
                            }
                        }

                        durationMs = masterClock.ElapsedMilliseconds;

#if PROFILE
                        var execWatch = Stopwatch.StartNew();
#endif
                        this.outputValue.OnNext(durationMs);

#if PROFILE
                        execWatch.Stop();
                        execTiming.Put(execWatch.ElapsedTicks);
                        tickTiming.Put((int)Math.Abs(durationMs * TicksPerMs - nextStop));

                        if (durationMs - lastReport >= 1000)
                        {
                            // Report
                            log.Debug("HighPTimer  avg: {Avg:N2}  best: {Min:N2}  worst: {Max:N2}   MaxExec: {3:N1}ms",
                                TimeSpan.FromTicks((long)tickTiming.Average()).TotalMilliseconds,
                                TimeSpan.FromTicks(tickTiming.Min()).TotalMilliseconds,
                                TimeSpan.FromTicks(tickTiming.Max()).TotalMilliseconds,
                                TimeSpan.FromTicks(execTiming.Max()).TotalMilliseconds);

                            lastReport = durationMs;
                        }
#endif
                    }

                    this.taskComplete.Set();
                }, cancelSource.Token, TaskCreationOptions.LongRunning);

            if (startRunning)
                this.task.Start();
        }

        public long ElapsedMs
        {
            get { return this.masterClock.ElapsedMilliseconds; }
        }

        public IObservable<long> Output
        {
            get
            {
                return this.outputValue.AsObservable();
            }
        }

        public void Start()
        {
            this.task.Start();
        }

        public void Stop()
        {
            log.Debug("Cancel 3");
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
            log.Debug("Cancel 4");
            this.cancelSource.Cancel();
        }

        public int IntervalMs { get; private set; }
    }
}
