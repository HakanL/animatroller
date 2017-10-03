using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseDevice : IOutputDevice, IPhysicalDevice
    {
        protected ILogger log;
        protected ILogicalDevice[] logicalDevices;

        public BaseDevice(params ILogicalDevice[] logicalDevice)
        {
            this.log = Log.Logger;
            Executor.Current.Register(this);

            this.logicalDevices = logicalDevice;
        }

        public virtual void SetInitialState()
        {
        }

        public string Name
        {
            get { return this.logicalDevices.First().Name; }
        }
    }
}
