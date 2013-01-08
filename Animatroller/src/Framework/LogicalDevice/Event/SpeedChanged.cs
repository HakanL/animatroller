using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class SpeedChangedEventArgs : EventArgs
    {
        public double NewSpeed { get; private set; }

        public SpeedChangedEventArgs(double newSpeed)
        {
            this.NewSpeed = newSpeed;
        }
    }
}
