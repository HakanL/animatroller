//#define DEBUG_LOG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Controller
{
    public class Timeline2<T> : ITimeline
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        public class MultiTimelineEventArgs : EventArgs
        {
            public int ElapsedMs { get; private set; }

            public IEnumerable<T> Code { get; private set; }
            public int Step { get; private set; }

            public MultiTimelineEventArgs(int elapsedMs, IEnumerable<T> code, int step)
            {
                this.ElapsedMs = elapsedMs;
                this.Code = code;
                this.Step = step;
            }
        }

        private SortedList<int, HashSet<T>> timeline;
        private Task task;
        private System.Threading.CancellationTokenSource cancelSource;
        private int? iterationsLeft;
        private int? iterations;

        public event EventHandler<MultiTimelineEventArgs> MultiTimelineTrigger;
        protected Action setupAction;
        protected Action tearDownAction;

        public Timeline2(int? iterations = null)
        {
            this.timeline = new SortedList<int, HashSet<T>>();
            this.iterations = iterations;
        }

        public void Setup(Action action)
        {
            this.setupAction = action;
        }

        public void TearDown(Action action)
        {
            this.tearDownAction = action;
        }

        public Timeline2<T> PopulateFromCSV(string filename)
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

        public Timeline2<T> AddS(double elapsedS, T code)
        {
            return AddMs((int)(elapsedS * 1000), code);
        }

        public Timeline2<T> AddMs(int elapsedMs, T code)
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

        public Task Start(long offsetMs, TimeSpan? duration = null)
        {
            if (this.cancelSource == null || this.cancelSource.IsCancellationRequested)
                this.cancelSource = new System.Threading.CancellationTokenSource();

            this.iterationsLeft = this.iterations;

            this.task = new Task(() =>
            {
                if (this.setupAction != null)
                    this.setupAction.Invoke();

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
                        int elapsed = this.timeline.Keys[currentPos];

                        while (watch.ElapsedMilliseconds + offsetMs < elapsed)
                        {
                            System.Threading.Thread.Sleep(1);
                            if (this.cancelSource.Token.IsCancellationRequested ||
                                duration.HasValue && watch.Elapsed > duration.Value)
                            {
                                if (this.tearDownAction != null)
                                    this.tearDownAction.Invoke();

                                return;
                            }
                        }

                        var codes = this.timeline.Values[currentPos];

#if DEBUG_LOG
                        // Invoke
                        string debugStr;
                        if (codes.Count == 1)
                            debugStr = string.Format("1 code ({0})", codes.First());
                        else
                            debugStr = string.Format("{0} codes ({1})", codes.Count,
                                string.Join(",", codes));
                        log.Debug(string.Format("Invoking {1} at {0:N2} s   (pos {2})", elapsed, debugStr, currentPos + 1));
#endif

                        var multiHandler = MultiTimelineTrigger;
                        if (multiHandler != null)
                            multiHandler(this, new MultiTimelineEventArgs(elapsed, codes, currentPos + 1));
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
