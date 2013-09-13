using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class Motor : IPhysicalDevice
    {
        private Action<double> physicalTrigger;
        protected string name;

        public Motor(string name, Action<double> physicalTrigger)
        {
            this.name = name;
            Executor.Current.Register(this);

            this.physicalTrigger = physicalTrigger;
        }

        public Motor Connect(LogicalDevice.Motor logicalDevice)
        {
            logicalDevice.SpeedChanged += (sender, e) =>
                {
                    this.physicalTrigger(e.NewSpeed);
                };

            return this;
        }

        public void StartDevice()
        {
        }

        public string Name
        {
            get { return this.name; }
        }
    }
}
