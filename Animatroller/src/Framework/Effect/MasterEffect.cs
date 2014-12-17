using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reactive;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;
using System.Threading.Tasks;

namespace Animatroller.Framework.Effect2
{
    public class MasterEffect
    {
        private TimerJobRunner timerJobRunner;

        public MasterEffect(TimerJobRunner timerJobRunner)
        {
            this.timerJobRunner = timerJobRunner;
        }

        public MasterEffect()
            : this(Executor.Current.TimerJobRunner)
        {
        }

        public Task Fade(IReceivesBrightness device, double start, double end, int durationMs, int priority = 1, ITransformer transformer = null)
        {
            var controlToken = device.TakeControl(priority);

            return Fade(device.GetBrightnessObserver(), start, end, durationMs, transformer)
                .ContinueWith(x =>
                {
                    controlToken.Dispose();
                });
        }

        public Task Fade(IObserver<double> deviceObserver, double start, double end, int durationMs, ITransformer transformer = null)
        {
            var taskSource = new TaskCompletionSource<bool>();

            double brightnessRange = end - start;

            if (brightnessRange == 0)
            {
                taskSource.SetResult(true);

                return taskSource.Task;
            }

            var observer = Observer.Create<double>(
                onNext: pos =>
                {
                    if (transformer != null)
                        pos = transformer.Transform(pos);

                    double brightness = start + (pos * brightnessRange);

                    deviceObserver.OnNext(brightness);
                },
                onCompleted: () =>
                {
                    taskSource.SetResult(true);
                });

            var cancelSource = this.timerJobRunner.AddTimerJobPos(observer, durationMs);

            Executor.Current.SetManagedTask(taskSource.Task, cancelSource);

            return taskSource.Task;
        }

        public Task Fade(IObserver<Color> deviceObserver, Color start, Color end, int durationMs, ITransformer transformer = null)
        {
            var taskSource = new TaskCompletionSource<bool>();

            if (start == end)
            {
                taskSource.SetResult(true);

                return taskSource.Task;
            }

            var startHSV = new HSV(start);
            var endHSV = new HSV(end);

            if (startHSV.Value == 0)
                startHSV = new HSV(endHSV.Hue, endHSV.Saturation, 0);

            var observer = Observer.Create<double>(
                onNext: pos =>
                {
                    if (transformer != null)
                        pos = transformer.Transform(pos);

                    double hue = startHSV.Hue + (endHSV.Hue - startHSV.Hue) * pos;
                    double sat = startHSV.Saturation + (endHSV.Saturation - startHSV.Saturation) * pos;
                    double val = startHSV.Value + (endHSV.Value - startHSV.Value) * pos;

                    Color newColor = HSV.ColorFromHSV(hue, sat, val);

                    deviceObserver.OnNext(newColor);
                },
                onCompleted: () =>
                {
                    taskSource.SetResult(true);
                });

            var cancelSource = this.timerJobRunner.AddTimerJobPos(observer, durationMs);

            Executor.Current.SetManagedTask(taskSource.Task, cancelSource);

            return taskSource.Task;
        }

        public Task Shimmer(IObserver<double> deviceObserver, double minBrightness, double maxBrightness, int durationMs, int priority = 1)
        {
            var taskSource = new TaskCompletionSource<bool>();

            bool state = false;

            var observer = Observer.Create<long>(
                onNext: currentElapsedMs =>
                {
                    state = !state;

                    deviceObserver.OnNext(state ? maxBrightness : minBrightness);
                },
                onCompleted: () =>
                {
                    taskSource.SetResult(true);
                });

            var cancelSource = this.timerJobRunner.AddTimerJobMs(observer, durationMs);

            Executor.Current.SetManagedTask(taskSource.Task, cancelSource);

            return taskSource.Task;
        }
    }
}
