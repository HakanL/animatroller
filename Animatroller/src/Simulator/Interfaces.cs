using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework;
using Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Simulator
{
    public interface IUpdateActionParent
    {
        void AddUpdateAction(Action action);
    }

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
        Control.PixelLight1D LightControl { set; }

        int Pixels { get; }

        ILogicalDevice ConnectedDevice { get; }
    }

    public interface INeedsMatrixLight : IOutputDevice
    {
        Control.PixelLight2D LightControl { set; }

        int PixelWidth { get; }

        int PixelHeight { get; }
    }
}
