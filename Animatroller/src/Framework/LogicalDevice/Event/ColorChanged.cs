using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class ColorChangedEventArgs : BrightnessChangedEventArgs
    {
        public Color NewColor { get; private set; }

        public ColorChangedEventArgs(Color newColor, double newBrightness)
            : base(newBrightness)
        {
            this.NewColor = newColor;
        }
    }
}
