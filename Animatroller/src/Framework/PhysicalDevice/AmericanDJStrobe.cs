using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class AmericanDJStrobe : BaseDevice, INeedsDmxOutput
    {
        public IDmxOutput DmxOutputPort { protected get; set; }

        public AmericanDJStrobe(Dimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.BrightnessChanged += (sender, e) =>
            {
                var dimmerValue = e.NewBrightness.GetByteScale(250) + 5;

                DmxOutputPort.SendDimmerValue(dmxChannel + 1, (byte)dimmerValue);
            };
        }

        public AmericanDJStrobe(StrobeDimmer logicalDevice, int dmxChannel)
            : this((Dimmer)logicalDevice, dmxChannel)
        {
            logicalDevice.StrobeSpeedChanged += (sender, e) =>
            {
                if (e.NewSpeed == 0)
                    DmxOutputPort.SendDimmerValue(dmxChannel, 255);
                else
                {
                    // 2-127 strobe effect, slow to fast
                    DmxOutputPort.SendDimmerValue(dmxChannel, (byte)(2 + e.NewSpeed.GetByteScale(125)));
                }
            };
        }
    }
}
