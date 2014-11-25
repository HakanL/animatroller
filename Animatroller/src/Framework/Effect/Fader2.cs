using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Effect2
{
    public class Fader2
    {
        protected class FadeJob
        {
            public bool Running { get; private set; }

            public IObserver<DoubleZeroToOne> Device { get; private set; }

            public Action CompleteAction { get; private set; }

            public double StartBrightness { get; private set; }

            public double EndBrightness { get; private set; }

            public double BrightnessRange { get; private set; }

            public int DurationMs { get; private set; }

            public long EndDurationMs { get; private set; }

            public long StartDurationMs { get; private set; }

            public void Init(IObserver<DoubleZeroToOne> device, double startBrightness, double endBrightness, int durationMs, long startDurationMs, Action completeAction)
            {
                this.Device = device;
                this.StartBrightness = startBrightness;
                this.EndBrightness = endBrightness;

                this.BrightnessRange = endBrightness - startBrightness;

                this.DurationMs = durationMs;

                this.StartDurationMs = startDurationMs;
                this.EndDurationMs = startDurationMs + durationMs;
                this.CompleteAction = completeAction;

                this.Running = true;
            }

            public void Update(long elapsedMs)
            {
                if (elapsedMs > EndDurationMs)
                {
                    this.Running = false;

                    this.Device.OnNext(new DoubleZeroToOne(this.EndBrightness));

                    if (CompleteAction != null)
                        CompleteAction();

                    return;
                }

                long currentElapsedMs = elapsedMs - this.StartDurationMs;

                double pos = (double)currentElapsedMs / (double)DurationMs;

                double brightness = this.StartBrightness + (pos * this.BrightnessRange);

                this.Device.OnNext(new DoubleZeroToOne(brightness));
            }
        }

        private List<FadeJob> fadeJobs = new List<FadeJob>();
        private Controller.HighPrecisionTimer2 masterTimer;

        public Fader2()
        {
            this.masterTimer = Executor.Current.MasterTimer;

            masterTimer.Output.Subscribe(x =>
                {
                    lock (this.fadeJobs)
                    {
                        foreach (var fadeJob in this.fadeJobs)
                        {
                            if (fadeJob.Running)
                                fadeJob.Update(this.masterTimer.ElapsedMs);
                        }
                    }
                });
        }

        private void AddFadeJob(Action<FadeJob> initAction)
        {
            lock (this.fadeJobs)
            {
                FadeJob fadeJob = null;

                foreach (var existingFadeJob in fadeJobs)
                {
                    if (!existingFadeJob.Running)
                    {
                        // Reuse
                        fadeJob = existingFadeJob;
                        break;
                    }
                }

                if (fadeJob == null)
                {
                    fadeJob = new FadeJob();
                    fadeJobs.Add(fadeJob);
                }

                initAction(fadeJob);
            }
        }

        public void Fade(IReceivesBrightness device, DoubleZeroToOne startBrightness, DoubleZeroToOne endBrightness, int durationMs)
        {
            AddFadeJob(fadeJob =>
            {
//                var subject = new Subject<DoubleZeroToOne>();

//                var owner = device.ControlBrightness(subject);

                //fadeJob.Init(subject, startBrightness.Value, endBrightness.Value, durationMs, this.masterTimer.ElapsedMs, () =>
                //{
                //    owner.Dispose();
                //});
            });
        }

        public void Fade(IObserver<DoubleZeroToOne> device, DoubleZeroToOne startBrightness, DoubleZeroToOne endBrightness, int durationMs)
        {
            device.OnNext(startBrightness);

            AddFadeJob(x =>
                {
                    x.Init(device, startBrightness.Value, endBrightness.Value, durationMs, this.masterTimer.ElapsedMs, null);
                });
        }

        //public Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction)
        //{
        //    return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, oneToZeroToOne, forced, totalTicks, final) =>
        //        {
        //            double brightness = zeroToOne.ScaleToMinMax(this.startBrightness, this.endBrightness);

        //            setBrightnessAction.Invoke(brightness);
        //        });
        //}

        //public int? Iterations
        //{
        //    get { return 1; }
        //}
    }
}
