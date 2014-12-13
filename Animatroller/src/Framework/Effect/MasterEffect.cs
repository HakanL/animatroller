using System;
using System.Collections.Generic;
using System.Linq;
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

        //public Task Fade(IReceivesBrightness device, double startBrightness, double endBrightness, int durationMs, int priority = 1)
        //{
        //    var taskSource = new TaskCompletionSource<bool>();

        //    IControlToken newToken = null;

        //    IControlToken control = Executor.Current.GetControlToken(device);

        //    if (control == null)
        //    {
        //        newToken =
        //        control = device.TakeControl(priority);

        //        Executor.Current.SetControlToken(device, control);
        //    }

        //    var deviceObserver = device.GetBrightnessObserver(control);

        //    double brightnessRange = endBrightness - startBrightness;

        //    var observer = Observer.Create<long>(
        //        onNext: currentElapsedMs =>
        //        {
        //            double pos = (double)currentElapsedMs / (double)durationMs;

        //            double brightness = startBrightness + (pos * brightnessRange);

        //            deviceObserver.OnNext(brightness);
        //        },
        //        onCompleted: () =>
        //        {
        //            if (newToken != null)
        //            {
        //                Executor.Current.RemoveControlToken(device);

        //                newToken.Dispose();
        //            }

        //            taskSource.SetResult(true);
        //        });

        //    this.timerJobRunner.AddTimerJob(observer, durationMs);

        //    return taskSource.Task;
        //}

        public Task Fade(IReceivesBrightness device, double startBrightness, double endBrightness, int durationMs, int priority = 1)
        {
            var controlToken = device.TakeControl(priority);

            return Fade(device.GetBrightnessObserver(), startBrightness, endBrightness, durationMs)
                .ContinueWith(x =>
                {
                    controlToken.Dispose();
                });
        }

        public Task Fade(IObserver<double> deviceObserver, double startBrightness, double endBrightness, int durationMs)
        {
            var taskSource = new TaskCompletionSource<bool>();

            double brightnessRange = endBrightness - startBrightness;

            var observer = Observer.Create<long>(
                onNext: currentElapsedMs =>
                {
                    double pos = (double)currentElapsedMs / (double)durationMs;

                    double brightness = startBrightness + (pos * brightnessRange);

                    deviceObserver.OnNext(brightness);
                },
                onCompleted: () =>
                {
                    taskSource.SetResult(true);
                });

            this.timerJobRunner.AddTimerJob(observer, durationMs);

            Executor.Current.SetManagedTask(taskSource.Task);

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

            this.timerJobRunner.AddTimerJob(observer, durationMs);

            return taskSource.Task;
        }
    }
}
