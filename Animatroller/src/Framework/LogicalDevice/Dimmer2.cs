using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class Dimmer2 : SingleOwnerDevice, IOutput//, /*IHasBrightnessControl, *///, IHasAnalogInput
    {
        protected object lockObject = new object();
        protected double currentBrightness;
        protected Effect.MasterSweeper.Job effectJob;
        protected ISubject<DoubleZeroToOne> inputBrightness;

        public Dimmer2([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputBrightness = new Subject<DoubleZeroToOne>();

            this.inputBrightness.Subscribe(x =>
                {
                    if (this.currentBrightness != x.Value)
                    {
#if DEBUG
                        if (!x.IsValid())
                            throw new ArgumentOutOfRangeException("Value is out of range");
#endif
                        this.currentBrightness = x.Value.Limit(0, 1);

                        if (x.Value == 0)
                            // Reset owner
                            currentOwner = null;
                    }
                });
        }

        public ISubject<DoubleZeroToOne> InputBrightness
        {
            get
            {
                return this.inputBrightness;
            }
        }

        public double Brightness
        {
            get { return this.currentBrightness; }
            set
            {
                this.inputBrightness.OnNext(new DoubleZeroToOne(value));
            }
        }

        public virtual void SetBrightness(double value, IOwner owner)
        {
            if (this.currentOwner != null && owner != this.currentOwner)
            {
                if (owner != null)
                {
//FIXME                    if (owner.Priority <= this.currentOwner.Priority)
                        return;
                }
                else
                    return;
            }

            if (value == 0)
                // Reset owner
                owner = null;

//FIXME            this.currentOwner = owner;
            this.InputBrightness.OnNext(new DoubleZeroToOne { Value = value });
        }

        public virtual void TurnOff()
        {
            this.InputBrightness.OnNext(DoubleZeroToOne.Zero);
        }

        public virtual Effect.MasterSweeper.Job RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration)
        {
            var effectAction = effect.GetEffectAction(brightness =>
                {
//FIXME                    this.SetBrightness(brightness, this);
                });

            lock (this.lockObject)
            {
                if (this.effectJob == null)
                {
                    // Create new
                    this.effectJob = Executor.Current.RegisterSweeperJob(effectAction, oneSweepDuration, effect.Iterations);
                }
                else
                {
                    this.effectJob.Reset(effectAction, oneSweepDuration, effect.Iterations);
                }
                this.effectJob.Restart();
            }

            return this.effectJob;
        }

        public void StopEffect()
        {
            if (this.effectJob != null)
                this.effectJob.Stop();
        }

        protected override void UpdateOutput()
        {
            InputBrightness.OnNext(new DoubleZeroToOne(this.currentBrightness));
        }
    }
}
