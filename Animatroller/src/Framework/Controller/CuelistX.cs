using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Controller
{
    public class CuelistX : Subroutine
    {
        private Effect.Transformer.EaseInOut easeTransform = new Effect.Transformer.EaseInOut();

        public class CuePointDouble
        {
            public double Current { get; set; }

            public double Next { get; set; }

            public int TimeToNext { get; set; }
        }

        public class CuePoint
        {
            public ILogicalDevice Device { get; set; }

            public double Brightness { get; set; }
        }

        private Dictionary<ILogicalDevice, SortedList<int, double>> timelineBrightness;
        private SortedList<int, List<ILogicalDevice>> elapsedIndex;
        private int? iterations;
        private bool forward;

        public CuelistX(int? iterations = null)
        {
            this.iterations = iterations;

            this.timelineBrightness = new Dictionary<ILogicalDevice, SortedList<int, double>>();
            this.elapsedIndex = new SortedList<int, List<ILogicalDevice>>();

            RunAction(i =>
                {
                    int? iterationsLeft = this.iterations;

                    while (!iterationsLeft.HasValue || iterationsLeft.GetValueOrDefault() > 0)
                    {
                        if (iterationsLeft.HasValue)
                            iterationsLeft = iterationsLeft.Value - 1;

                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        for (int currentPos = 0; currentPos < this.elapsedIndex.Count; currentPos++)
                        {
                            int elapsed = this.elapsedIndex.Keys[currentPos];

                            while (watch.ElapsedMilliseconds < elapsed)
                            {
                                System.Threading.Thread.Sleep(1);
                                if (i.IsCancellationRequested)
                                {
                                    log.Debug("Cuelist stopped");
                                    return;
                                }
                            }

                            var deviceList = this.elapsedIndex.Values[currentPos];

                            foreach (var device in deviceList)
                            {
                                var elapsedList = this.timelineBrightness[device];
                                double brightness = elapsedList.Values[currentPos];

                                int nextPos = currentPos + 1;
                                if (nextPos >= elapsedList.Count)
                                {
                                    if (iterationsLeft.GetValueOrDefault() > 0)
                                        nextPos = 0;
                                    else
                                        // No trigger if we don't have any more cue points
                                        continue;
                                }

                                double nextBrightness = elapsedList.Values[nextPos];
                                int nextElapsedMs = elapsedList.Keys[nextPos];

                                int durationMs = nextElapsedMs - elapsed - 1;

                                var brightnessDevice = device as IReceivesBrightness;

                                if (brightnessDevice != null)
                                {
                                    if (durationMs < 5)
                                    {
                                        brightnessDevice.Brightness = nextBrightness;
                                    }
                                    else
                                        Executor.Current.MasterEffect.Fade(brightnessDevice, brightness, nextBrightness, durationMs, transformer: easeTransform);
                                }
                            }
                        }
                    }
                });
        }
/*
        private void Xyz(double pos)
        {
            var taskSource = new TaskCompletionSource<bool>();

            double brightnessRange = endBrightness - startBrightness;

            var observer = Observer.Create<long>(
                onNext: currentElapsedMs =>
                {
//                    double pos = (double)currentElapsedMs / (double)durationMs;

                    if (transformer != null)
                        pos = transformer.Transform(pos);

                    double brightness = startBrightness + (pos * brightnessRange);

                    deviceObserver.OnNext(brightness);
                },
                onCompleted: () =>
                {
                    taskSource.SetResult(true);
                });

            var cancelSource = this.timerJobRunner.AddTimerJob(observer, durationMs);

            Executor.Current.SetManagedTask(taskSource.Task, cancelSource);

            return taskSource.Task;
        }
*/
        private void AddBrightness(ILogicalDevice device, int elapsedMs, double brightness)
        {
            SortedList<int, double> list;
            if (!this.timelineBrightness.TryGetValue(device, out list))
            {
                list = new SortedList<int, double>();
                this.timelineBrightness.Add(device, list);
            }

            if (list.ContainsKey(elapsedMs))
                throw new ArgumentException("Key already exists");

            list.Add(elapsedMs, brightness);

            List<ILogicalDevice> deviceList;
            if (!this.elapsedIndex.TryGetValue(elapsedMs, out deviceList))
            {
                deviceList = new List<ILogicalDevice>();
                this.elapsedIndex.Add(elapsedMs, deviceList);
            }
            deviceList.Add(device);
        }

        public void AddMs(int elapsedMs, ILogicalDevice device, double brightness, Color color, double pan, double tilt)
        {
            AddBrightness(device, elapsedMs, brightness);
        }
    }
}
