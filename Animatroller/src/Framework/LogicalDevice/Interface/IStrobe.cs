using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public interface IStrobe
    {
        event EventHandler<StrobeSpeedChangedEventArgs> StrobeSpeedChanged;
        double StrobeSpeed { get; }
        IStrobe SetStrobe(double value);
        IStrobe StopStrobe();
    }
}
