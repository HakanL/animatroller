using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class EliminatorFlash192 : BaseDMXStrobeLight, INeedsDmxOutput
    {
        public EliminatorFlash192(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            byte brightness = (byte)(GetMonochromeBrightnessFromColorBrightness().GetByteScale(234) + 21);

            byte strobe;
            if (this.strobeSpeed == 0)
                strobe = 0;
            else
                strobe = (byte)(11 + this.strobeSpeed.GetByteScale(244));

            DmxOutputPort.SendDimmerValues(baseDmxChannel, new byte[] {
                brightness,
                strobe,
                0,  // Sound strobing
                0   // Fading
            });
        }
    }
}
