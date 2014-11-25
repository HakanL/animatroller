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
    public class MasterShimmer
    {
        private TimerJobRunner timerJobRunner;

        public MasterShimmer(TimerJobRunner timerJobRunner)
        {
            this.timerJobRunner = timerJobRunner;
        }

        public MasterShimmer()
            : this(Executor.Current.TimerJobRunner)
        {
        }

        public void Shimmer(IReceivesBrightness device, double minBrightness, double maxBrightness, int durationMs, int priority = 1)
        {
            var control = device.TakeControl(priority);

            var deviceObserver = device.GetBrightnessObserver(control);

            bool state = false;

            var observer = Observer.Create<long>(
                onNext: currentElapsedMs =>
                {
                    state = !state;

                    deviceObserver.OnNext(state ? maxBrightness : minBrightness);
                },
                onCompleted: () =>
                {
                    control.Dispose();
                });

            this.timerJobRunner.AddTimerJob(observer, durationMs);
        }
    }
}
