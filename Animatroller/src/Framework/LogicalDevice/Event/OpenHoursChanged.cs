using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class OpenHoursEventArgs : EventArgs
    {
        public bool IsOpenNow { get; private set; }

        public OpenHoursEventArgs(bool isOpenNow)
        {
            this.IsOpenNow = isOpenNow;
        }
    }
}
