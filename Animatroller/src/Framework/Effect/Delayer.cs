using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Effect2
{
    public class Delayer
    {
        protected class DelayJob
        {
            public bool Running { get; private set; }

            public Action DelayedAction { get; private set; }

            public long StartDurationMs { get; private set; }

            public void Init(Action delayedAction, int delayMs, long startDurationMs)
            {
                this.DelayedAction = delayedAction;
                this.StartDurationMs = startDurationMs + delayMs;

                this.Running = true;
            }

            public void CheckAndExecute(long elapsedMs)
            {
                if (elapsedMs >= StartDurationMs)
                {
                    // Execute
                    this.DelayedAction();

                    this.Running = false;
                }
            }
        }

        private List<DelayJob> delayJobs = new List<DelayJob>();
        private Controller.HighPrecisionTimer2 masterTimer;

        public Delayer()
        {
            this.masterTimer = Executor.Current.MasterTimer;

            masterTimer.Output.Subscribe(x =>
                {
                    lock (this.delayJobs)
                    {
                        foreach (var delayJob in this.delayJobs)
                        {
                            if (delayJob.Running)
                                delayJob.CheckAndExecute(this.masterTimer.ElapsedMs);
                        }
                    }
                });
        }

        public void After(int delayMs, Action action)
        {
            lock (this.delayJobs)
            {
                DelayJob delayJob = null;

                foreach (var existingFadeJob in delayJobs)
                {
                    if (!existingFadeJob.Running)
                    {
                        // Reuse
                        delayJob = existingFadeJob;
                        break;
                    }
                }

                if (delayJob == null)
                {
                    delayJob = new DelayJob();
                    delayJobs.Add(delayJob);
                }

//                delayJob.Init(device, startBrightness.Value, endBrightness.Value, durationMs, this.masterTimer.ElapsedMs);
            }
        }
    }
}
