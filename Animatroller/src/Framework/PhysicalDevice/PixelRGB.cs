using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class PixelRGB : BaseDevice, INeedsPixelOutput
    {
        public IPixelOutput PixelOutputPort { protected get; set; }
    }
}
