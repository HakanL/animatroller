using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Reactive;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;
using System.Threading.Tasks;

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
