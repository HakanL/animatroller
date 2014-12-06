using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseDevice : IOutputDevice, IPhysicalDevice
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private ILogicalDevice[] logicalDevices;

        public BaseDevice(params ILogicalDevice[] logicalDevice)
        {
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
