using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class StrobeSpeedChangedEventArgs : EventArgs
    {
        public double NewSpeed { get; private set; }

        public StrobeSpeedChangedEventArgs(double newSpeed)
        {
            this.NewSpeed = newSpeed;
        }
    }
}
