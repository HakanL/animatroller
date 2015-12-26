using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Text;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reactive;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice;

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

        public Task Fade(IReceivesBrightness device, double start, double end, int durationMs, int priority = 1, ITransformer transformer = null, IControlToken token = null,
            params Tuple<DataElements, object>[] additionalData)
        {
            if (token != null)
                return Fade(device.GetDataObserver(token), start, end, durationMs, transformer, additionalData);

            var controlToken = device.TakeControl(priority);

            return Fade(device.GetDataObserver(controlToken), start, end, durationMs, transformer, additionalData)
                .ContinueWith(x =>
                {
                    controlToken.Dispose();
                });
        }

        public Task Fade(IPushDataController deviceObserver, double start, double end, int durationMs, ITransformer transformer = null,
            params Tuple<DataElements, object>[] additionalData)
        {
            var taskSource = new TaskCompletionSource<bool>();

            double brightnessRange = end - start;

            if (brightnessRange == 0)
            {
                taskSource.SetResult(true);

                return taskSource.Task;
            }

            if (additionalData.Any())
                deviceObserver.SetDataFromIData(additionalData.GenerateIData());

            var observer = Observer.Create<double>(
                onNext: pos =>
                {
                    if (transformer != null)
                        pos = transformer.Transform(pos);

                    double brightness = start + (pos * brightnessRange);

                    deviceObserver.Data[DataElements.Brightness] = brightness;
                    deviceObserver.PushData();
                },
                onCompleted: () =>
                {
                    taskSource.SetResult(true);
                });

            var cancelSource = this.timerJobRunner.AddTimerJobPos(observer, durationMs);

            Executor.Current.SetManagedTask(taskSource.Task, cancelSource);

            return taskSource.Task;
        }

        public Task Custom(double[] customList, IReceivesBrightness device, int durationMs, int? loop, int priority = 1)
        {
            var controlToken = device.TakeControl(priority);

            return Custom(customList, device.GetDataObserver(controlToken), durationMs, loop)
                .ContinueWith(x =>
                {
                    controlToken.Dispose();
                });
        }

        public Task Custom(double[] customList, IPushDataController deviceObserver, int durationMs, int? loop)
        {
            var taskSource = new TaskCompletionSource<bool>();

            if (customList == null || customList.Length == 0)
                throw new ArgumentNullException("customList");

            CancellationTokenSource cancelSource = null;

            var observer = Observer.Create<long>(
                onNext: elapsedMs =>
                {
                    if (loop.HasValue)
                    {
                        long loopCounter = elapsedMs / durationMs;

                        if (loopCounter >= loop.Value)
                        {
                            deviceObserver.Data[DataElements.Brightness] = customList[customList.Length - 1];
                            deviceObserver.PushData();

                            cancelSource.Cancel();
                            return;
                        }
                    }

                    double instanceMs = elapsedMs % durationMs;

                    int pos = (int)(customList.Length * instanceMs / durationMs);

                    deviceObserver.Data[DataElements.Brightness] = customList[pos];
                    deviceObserver.PushData();
                },
                onCompleted: () =>
                {
                    taskSource.SetResult(true);
                });

            cancelSource = this.timerJobRunner.AddTimerJobMs(observer, loop.HasValue ? durationMs * loop.Value : (long?)null);

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
                    deviceObserver.OnCompleted();

                    taskSource.SetResult(true);
                });

            var cancelSource = this.timerJobRunner.AddTimerJobPos(observer, durationMs);

            Executor.Current.SetManagedTask(taskSource.Task, cancelSource);

            return taskSource.Task;
        }

        public Task Shimmer(IReceivesBrightness device, double minBrightness, double maxBrightness, int durationMs, int priority = 1, IControlToken token = null)
        {
            if (token != null)
                return Shimmer(device.GetDataObserver(token), minBrightness, maxBrightness, durationMs);

            var controlToken = device.TakeControl(priority);

            return Shimmer(device.GetDataObserver(controlToken), minBrightness, maxBrightness, durationMs)
                .ContinueWith(x =>
                {
                    controlToken.Dispose();
                });
        }

        public Task Shimmer(IPushDataController deviceObserver, double minBrightness, double maxBrightness, int durationMs, int priority = 1)
        {
            var taskSource = new TaskCompletionSource<bool>();

            bool state = false;

            var observer = Observer.Create<long>(
                onNext: currentElapsedMs =>
                {
                    state = !state;

                    deviceObserver.Data[DataElements.Brightness] = state ? maxBrightness : minBrightness;
                    deviceObserver.PushData();
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
