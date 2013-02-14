using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.LogicalDevice
{
    public interface IHasBrightnessControl : ILogicalDevice
    {
        double Brightness { set; }
        void SetBrightness(double value, IOwner owner);
        Effect.MasterSweeper.Job RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration);
        void StopEffect();
    }

    public interface IHasColorControl : ILogicalDevice
    {
        void SetColor(Color value, IOwner owner);
        Color Color { get; set; }
        //Effect.MasterSweeper.Job RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration);
        //void StopEffect();
    }

    public interface IHasControlledDevice : ILogicalDevice
    {
        IControlledDevice ControlledDevice { get; }
    }
}
