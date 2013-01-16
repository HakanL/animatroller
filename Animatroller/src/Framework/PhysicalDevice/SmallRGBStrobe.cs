using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class SmallRGBStrobe : BaseDevice, INeedsDmxOutput
    {
        public IDmxOutput DmxOutputPort { protected get; set; }

        public SmallRGBStrobe(ColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.ColorChanged += (sender, e) =>
                {
                    // Handles brightness as well

                    var hsv = new HSV(e.NewColor);
                    hsv.Value = hsv.Value * e.NewBrightness;
                    var color = hsv.Color;

                    DmxOutputPort.SendDimmerValues(dmxChannel + 1, new byte[] { color.R, color.B, color.G });
                };
        }

        public SmallRGBStrobe(StrobeColorDimmer logicalDevice, int dmxChannel)
            : this((ColorDimmer)logicalDevice, dmxChannel)
        {
            logicalDevice.StrobeSpeedChanged += (sender, e) =>
                {
                    var val = (byte)(e.NewSpeed == 0 ? 127 : e.NewSpeed.GetByteScale(121) + 128);
                    DmxOutputPort.SendDimmerValue(dmxChannel, val);
                };
        }
    }
}
