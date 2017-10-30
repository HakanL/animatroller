using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class DigitalOutput : IPhysicalDevice
    {
        private Action<bool> physicalTrigger;

        public DigitalOutput(Action<bool> physicalTrigger)
        {
            Executor.Current.Register(this);

            this.physicalTrigger = physicalTrigger;
        }

        public DigitalOutput Connect(ILogicalOutputDevice<bool> logicalDevice, bool inverted = false)
        {
            logicalDevice.Output.Subscribe(x =>
            {
                if (inverted)
                    this.physicalTrigger(!x);
                else
                    this.physicalTrigger(x);
            });

            return this;
        }

        public void SetInitialState()
        {
        }

        public string Name
        {
            get { return string.Empty; }
        }
    }
}
