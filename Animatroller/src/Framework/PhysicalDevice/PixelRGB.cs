using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class PixelRGB : BaseDevice, INeedsPixelOutput
    {
        public IPixelOutput PixelOutputPort { protected get; set; }

        public PixelRGB(ColorDimmer logicalDevice, int channel)
            : base(logicalDevice)
        {
            logicalDevice.ColorChanged += (sender, e) =>
                {
                    // Handles brightness as well

                    var hsv = new HSV(e.NewColor);
                    hsv.Value = hsv.Value * e.NewBrightness;
                    var color = hsv.Color;

                    PixelOutputPort.SendPixelValue(channel, color.R, color.G, color.B);
                };
        }
    }
}
