using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Reactive;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Effect2
{
    public class MasterFader : TimerJobRunner
    {
        public MasterFader()
            : base(Executor.Current.MasterTimer)
        {
        }

        public void Fade(IReceivesBrightness device, double startBrightness, double endBrightness, int durationMs, int priority = 1)
        {
            var control = device.TakeControl(priority);

            var deviceObserver = device.GetBrightnessObserver(control);

            double brightnessRange = endBrightness - startBrightness;

            var observer = Observer.Create<long>(onNext: currentElapsedMs =>
                {
                    double pos = (double)currentElapsedMs / (double)durationMs;

                    double brightness = startBrightness + (pos * brightnessRange);

                    deviceObserver.OnNext(brightness);
                },
                onCompleted: () =>
                {
                    control.Dispose();
                });

            AddTimerJob(observer, durationMs);
        }
    }
}
