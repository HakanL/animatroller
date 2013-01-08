using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class MotorVectorChangedEventArgs : EventArgs
    {
        public MotorWithFeedback.MotorVector Vector { get; private set; }

        public MotorVectorChangedEventArgs(MotorWithFeedback.MotorVector vector)
        {
            this.Vector = vector;
        }
    }
}
