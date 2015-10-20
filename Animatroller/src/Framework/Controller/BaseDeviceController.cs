using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Controller
{
    public abstract class BaseDeviceController<T> where T : IOwnedDevice
    {
        public T Device { get; private set; }

        public int Priority { get; set; }

        public BaseDeviceController(T device, int priority)
        {
            Device = device;
            Priority = priority;
        }
    }
}
