using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class MotorStatusChangedEventArgs : EventArgs
    {
        public int? NewPos { get; private set; }
        public bool Failed { get; private set; }

        public MotorStatusChangedEventArgs(int? newPos, bool failed)
        {
            this.NewPos = newPos;
            this.Failed = failed;
        }
    }
}
