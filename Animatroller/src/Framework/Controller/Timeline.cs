using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Animatroller.Framework.Controller
{
    public class Timeline<T> : ITimeline
    {
        protected ILogger log;

        public class TimelineEventArgs : EventArgs
        {
            public double ElapsedS { get; private set; }
            public T Code { get; private set; }
            public int Step { get; private set; }

            public TimelineEventArgs(double elapsedS, T code, int step)
            {
                this.ElapsedS = elapsedS;
                this.Code = code;
                this.Step = step;
            }
        }

        public class MultiTimelineEventArgs : EventArgs
        {
            public double ElapsedS { get; private set; }
            public IEnumerable<T> Code { get; private set; }
            public int Step { get; private set; }

            public MultiTimelineEventArgs(double elapsedS, IEnumerable<T> code, int step)
            {
                this.ElapsedS = elapsedS;
                this.Code = code;
                this.Step = step;
            }
        }

        private SortedList<int, HashSet<T>> timeline;
        private Task task;
        private System.Threading.CancellationTokenSource cancelSource;
        private int? iterationsLeft;
        private int? iterations;

        public event EventHandler<TimelineEventArgs> TimelineTrigger;
        public event EventHandler<MultiTimelineEventArgs> MultiTimelineTrigger;
        protected Action tearDownAction;

        public Timeline(int? iterations)
        {
            this.log = Log.Logger;
            this.timeline = new SortedList<int, HashSet<T>>();
            this.iterations = iterations;
        }

        public void TearDown(Action action)
        {
            this.tearDownAction = action;
        }

        public Timeline<T> PopulateFromCSV(string filename)
        {
            using (var file = System.IO.File.OpenText(filename))
            {
                // Header
                file.ReadLine();

                while (true)
                {
                    string line = file.ReadLine();
                    if (line == null)
                        break;

                    string[] parts = line.Split(',');

                    double elapsedS = double.Parse(parts[0]);

                    for (int i = 3; i < parts.Length; i++)
                        if (!string.IsNullOrEmpty(parts[i]))
                            AddS(elapsedS, (T)Convert.ChangeType(parts[i], typeof(T)));
                }
            }

            return this;
        }

        public Timeline<T> AddS(double elapsedS, T code)
        {
            return AddMs((int)(elapsedS * 1000), code);
        }

        public Timeline<T> AddMs(int elapsedMs, T code)
        {
            HashSet<T> codes;
            if (!timeline.TryGetValue(elapsedMs, out codes))
            {
                codes = new HashSet<T>();
                timeline.Add(elapsedMs, codes);
            }
            codes.Add(code);

            return this;
        }

        public void Stop()
        {
            if (this.cancelSource != null)
            {
                this.cancelSource.Cancel();
            }
        }

        public Task Start(long offsetMs = 0, TimeSpan? duration = null)
        {
            if (duration.HasValue)
                throw new NotImplementedException("Duration not implemented in this class");

            if (this.cancelSource == null || this.cancelSource.IsCancellationRequested)
                this.cancelSource = new System.Threading.CancellationTokenSource();

            this.iterationsLeft = this.iterations;

            this.task = new Task(() =>
            {
                while (!this.iterationsLeft.HasValue || this.iterationsLeft.GetValueOrDefault() > 0)
                {
                    if (this.iterationsLeft.HasValue)
                        this.iterationsLeft = this.iterationsLeft.Value - 1;

                    // Find start pos
                    int startPos = 0;
                    for (int currentPos = 0; currentPos < this.timeline.Count; currentPos++)
                    {
                        int elapsed = this.timeline.Keys[currentPos];
                        if (elapsed >= offsetMs)
                        {
                            startPos = currentPos;
                            break;
                        }
                    }

                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    for (int currentPos = startPos; currentPos < this.timeline.Count; currentPos++)
                    {
                        long elapsedMs = this.timeline.Keys[currentPos];

                        while (watch.ElapsedMilliseconds + offsetMs < elapsedMs)
                        {
                            System.Threading.Thread.Sleep(1);
                            if (this.cancelSource.Token.IsCancellationRequested)
                            {
                                if (this.tearDownAction != null)
                                    this.tearDownAction.Invoke();

                                return;
                            }
                        }

                        var codes = this.timeline.Values[currentPos];

                        double elapsedS = elapsedMs / 1000.0;

                        // Invoke
                        string debugStr;
                        if (codes.Count == 1)
                            debugStr = string.Format("1 code ({0})", codes.First());
                        else
                            debugStr = string.Format("{0} codes ({1})", codes.Count,
                                string.Join(",", codes));
                        log.Debug(string.Format("Invoking {1} at {0:N2} s   (pos {2})", elapsedS, debugStr, currentPos + 1));
                        var handler = TimelineTrigger;
                        if (handler != null)
                        {
                            foreach (var code in codes)
                            {
                                handler(this, new TimelineEventArgs(elapsedS, code, currentPos + 1));
                            }
                        }

                        var multiHandler = MultiTimelineTrigger;
                        if (multiHandler != null)
                            multiHandler(this, new MultiTimelineEventArgs(elapsedS, codes, currentPos + 1));
                    }
                }

                if (this.tearDownAction != null)
                    this.tearDownAction.Invoke();
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            this.task.Start();
            Executor.Current.RegisterCancelSource(this.cancelSource, this.task, "Timeline");

            return task;
        }
    }
}
