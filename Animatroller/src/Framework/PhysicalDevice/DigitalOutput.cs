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

        public DigitalOutput Connect(LogicalDevice.DigitalOutput2 logicalDevice)
        {
            logicalDevice.Output.Subscribe(x =>
                {
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
