using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public interface IPixel1D : ILogicalDevice
    {
        int Pixels { get; }
        event EventHandler<PixelChangedEventArgs> PixelChanged;
        event EventHandler<MultiPixelChangedEventArgs> MultiPixelChanged;
    }
}
