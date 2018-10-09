using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class PixelChangedEventArgs : EventArgs
    {
        public IChannel Channel { get; private set; }
        public double NewBrightness { get; private set; }
        public Color NewColor { get; private set; }

        public PixelChangedEventArgs(IChannel channel, Color newColor, double newBrightness)
        {
            this.Channel = channel;
            this.NewBrightness = newBrightness;
            this.NewColor = newColor;
        }
    }
}
