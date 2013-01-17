using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework
{
    public class Timeline
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        public class TimelineEventArgs : EventArgs
        {
            public double ElapsedS { get; private set; }
            public string Code { get; private set; }
            public int Step { get; private set; }

            public TimelineEventArgs(double elapsedS, string code, int step)
            {
                this.ElapsedS = elapsedS;
                this.Code = code;
                this.Step = step;
            }
        }

        private SortedList<double, HashSet<string>> timeline;
        private Task task;
        private System.Threading.CancellationTokenSource cancelSource;

        public event EventHandler<TimelineEventArgs> TimelineTrigger;

        public Timeline()
        {
            this.timeline = new SortedList<double, HashSet<string>>();
        }

        public Timeline PopulateFromCSV(string filename)
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

                    double elapsed = double.Parse(parts[0]);

                    for (int i = 3; i < parts.Length; i++)
                        if (!string.IsNullOrEmpty(parts[i]))
                            Add(elapsed, parts[i]);
                }
            }

            return this;
        }

        public Timeline Add(double elapsed, string code)
        {
            HashSet<string> codes;
            if (!timeline.TryGetValue(elapsed, out codes))
            {
                codes = new HashSet<string>();
                timeline.Add(elapsed, codes);
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

        public Task Start()
        {
            this.cancelSource = new System.Threading.CancellationTokenSource();

            this.task = new Task(a =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (int currentPos = 0; currentPos < this.timeline.Count; currentPos++)
                {
                    double elapsed = this.timeline.Keys[currentPos];

                    while (watch.Elapsed.TotalSeconds < elapsed)
                    {
                        System.Threading.Thread.Sleep(1);
                        if (this.cancelSource.Token.IsCancellationRequested)
                            return;
                    }

                    var codes = this.timeline.Values[currentPos];

                    // Invoke
                    var handler = TimelineTrigger;
                    foreach (var code in codes)
                    {
                        log.Info(string.Format("Invoking codes {1} at {0:N2} s   (pos {2})", elapsed, code, currentPos + 1));
                        if (handler != null)
                            handler(this, new TimelineEventArgs(elapsed, code, currentPos + 1));
                    }

                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            task.Start();

            return task;
        }
    }
}
