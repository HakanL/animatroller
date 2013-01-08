using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class RGBStrobe : BaseDevice, INeedsDmxOutput
    {
        public IDmxOutput DmxOutputPort { protected get; set; }

        public RGBStrobe(ColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.ColorChanged += (sender, e) =>
                {
                    // Handles brightness as well

                    var hsv = new HSV(e.NewColor);
                    hsv.Value = hsv.Value * e.NewBrightness;
                    var color = hsv.Color;

                    DmxOutputPort.SendDimmerValues(dmxChannel, color.R, color.G, color.B);
                };
        }

        public RGBStrobe(StrobeColorDimmer logicalDevice, int dmxChannel)
            : this((ColorDimmer)logicalDevice, dmxChannel)
        {
            logicalDevice.StrobeSpeedChanged += (sender, e) =>
                {
                    DmxOutputPort.SendDimmerValue(dmxChannel + 3, (byte)(e.NewSpeed == 0 ? 0 : 28));
                    DmxOutputPort.SendDimmerValue(dmxChannel + 5, e.NewSpeed.GetByteScale());
                };
        }
    }
}
