using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework;
using Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Simulator
{
    public interface IUpdateableControl
    {
        void Update();
    }

    public interface INeedsLabelLight : IOutputDevice
    {
        Control.StrobeBulb LabelLightControl { set; }
    }

    public interface INeedsRopeLight : IOutputDevice
    {
        Control.RopeLight RopeLightControl { set; }

        int Pixels { get; }

        ILogicalDevice ConnectedDevice { get; }
    }
}
