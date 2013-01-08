using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseDevice : IOutputDevice
    {
        private ILogicalDevice[] logicalDevices;

        public BaseDevice(params ILogicalDevice[] logicalDevice)
        {
            Executor.Current.Register(this);

            this.logicalDevices = logicalDevice;
        }

        public void StartDevice()
        {
        }
    }
}
