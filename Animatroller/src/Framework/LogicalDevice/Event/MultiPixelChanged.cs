using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class MultiPixelChangedEventArgs : EventArgs
    {
        public int StartChannel { get; private set; }
        public ColorBrightness[] NewValues { get; private set; }

        public MultiPixelChangedEventArgs(int startChannel, ColorBrightness[] values)
        {
            this.StartChannel = startChannel;
            this.NewValues = values;
        }
    }
}
