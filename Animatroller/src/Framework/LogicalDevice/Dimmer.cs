using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using NLog;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class Dimmer : IOutput, ILogicalDevice, IHasBrightnessControl, IOwner
    {
        protected object lockObject = new object();
        protected double brightness;
        protected IOwner owner;
        protected string name;
        protected Effect.MasterSweeper.Job effectJob;

        public event EventHandler<BrightnessChangedEventArgs> BrightnessChanged;

        public Dimmer(string name)
        {
            this.name = name;
            Executor.Current.Register(this);
        }

        protected virtual void RaiseBrightnessChanged()
        {
            var handler = BrightnessChanged;
            if (handler != null)
                handler(this, new BrightnessChangedEventArgs(this.Brightness));
        }

        public double Brightness
        {
            get { return this.brightness; }
            set
            {
                if (this.brightness != value)
                {
                    this.brightness = value.Limit(0, 1);

                    if (value == 0)
                        // Reset owner
                        owner = null;

                    RaiseBrightnessChanged();
                }
            }
        }

        public virtual Dimmer SetBrightness(double value)
        {
            this.Brightness = value;

            return this;
        }

        public virtual void SetBrightness(double value, IOwner owner)
        {
            if (this.owner != null && owner != this.owner)
            {
                if (owner != null)
                {
                    if (owner.Priority <= this.owner.Priority)
                        return;
                }
                else
                    return;
            }

            this.owner = owner;
            this.Brightness = value;            
        }

        public virtual Dimmer TurnOff()
        {
            this.Brightness = 0;
            
            return this;
        }

        public virtual Effect.MasterSweeper.Job RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration)
        {
            var effectAction = effect.GetEffectAction(brightness =>
                {
                    this.SetBrightness(brightness, this);
                });

            lock (this.lockObject)
            {
                if (this.effectJob == null)
                {
                    // Create new
                    this.effectJob = Executor.Current.RegisterSweeperJob(effectAction, oneSweepDuration, effect.OneShot);
                }
                else
                {
                    this.effectJob.Reset(effectAction, oneSweepDuration, effect.OneShot);
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

        public virtual void StartDevice()
        {
            RaiseBrightnessChanged();
        }

        public string Name
        {
            get { return this.name; }
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
