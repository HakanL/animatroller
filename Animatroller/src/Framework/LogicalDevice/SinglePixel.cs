using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class SinglePixel : ILogicalDevice, IOwner, IHasBrightnessControl, IHasColorControl, IHasControlledDevice
    {
        protected object lockObject = new object();
        protected string name;
        protected IOwner owner;
        protected VirtualPixel1D2 pixelDevice;
        protected int position;

        protected Effect.MasterSweeper.Job effectJob;


        public SinglePixel(string name, VirtualPixel1D2 pixelDevice, int position)
        {
            this.name = name;
            this.pixelDevice = pixelDevice;
            this.position = position;

            Executor.Current.Register(this);
        }

        public void SetInitialState()
        {
        }

        public string Name
        {
            get { return this.name; }
        }

        public IControlledDevice ControlledDevice
        {
            get
            { //return this.pixelDevice; 
                //FIXME
                throw new NotImplementedException();
            }
        }

        public double Brightness
        {
            set
            {
                if (value == 0)
                    // Reset owner
                    owner = null;

                this.pixelDevice.SetBrightness(this.position, value);
            }
        }

        public void SetBrightness(double value, IOwner owner)
        {
            if (value == 0)
                // Reset owner
                owner = null;

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

        public void ReleaseOwner()
        {
            this.owner = null;
        }

        public Effect.MasterSweeper.Job RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration)
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

        public int Priority
        {
            get { return 0; }
        }

        public void SetColor(Color value, IOwner owner)
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
            this.pixelDevice.SetColor(this.position, value);
        }

        public void EnableOutput()
        {
        }

        public Color Color
        {
            get
            {
                return this.pixelDevice.GetColorBrightness(this.position).Color;
            }
            set
            {
                this.pixelDevice.SetColor(this.position, value);
            }
        }
    }
}
