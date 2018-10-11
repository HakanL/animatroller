using Serilog;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Animatroller.Framework.Controller
{
    public class HighPrecisionTimer4 : IMasterTimer, IDisposable
    {
        protected ILogger log;
        private readonly MultimediaTimer multimediaTimer;
        private readonly Stopwatch masterClock;
        protected ISubject<long> outputValue;

        public int IntervalMs => this.multimediaTimer.Interval;

        public long ElapsedMs => this.masterClock.ElapsedMilliseconds;

        public IObservable<long> Output => this.outputValue.AsObservable();

        public HighPrecisionTimer4(ILogger logger, int intervalMs, bool startRunning = true)
        {
            this.log = logger;
            this.log.Information("Starting HighPrecisionTimer4 with {0} ms interval", intervalMs);

            this.multimediaTimer = new MultimediaTimer
            {
                Interval = intervalMs
            };

            this.multimediaTimer.Elapsed += (o, e) =>
            {
                try
                {
                    this.outputValue.OnNext(ElapsedMs);
                }
                catch (Exception ex)
                {
                    this.log.Error(ex, "Error in HighPrecisionTimer4 callback");
                }
            };

            this.masterClock = new Stopwatch();
            this.outputValue = new Subject<long>();

            if (startRunning)
            {
                this.multimediaTimer.Start();
                this.masterClock.Start();
            }
        }

        public void Start()
        {
            this.multimediaTimer.Start();
        }

        public void Stop()
        {
            this.multimediaTimer.Stop();
        }

        public void Dispose()
        {
            this.log.Debug("Cancel HPT4");

            this.multimediaTimer.Stop();
            this.multimediaTimer.Dispose();
        }
    }
}
