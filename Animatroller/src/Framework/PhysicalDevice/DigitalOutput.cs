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

        public DigitalOutput Connect(ILogicalOutputDevice<bool> logicalDevice)
        {
            logicalDevice.Output.Subscribe(this.physicalTrigger);

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
