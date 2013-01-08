using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class BrightnessChangedEventArgs : EventArgs
    {
        public double NewBrightness { get; private set; }

        public BrightnessChangedEventArgs(double newBrightness)
        {
            this.NewBrightness = newBrightness;
        }
    }
}
