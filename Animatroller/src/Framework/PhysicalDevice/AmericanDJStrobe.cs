using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class AmericanDJStrobe : BaseDMXStrobeLight, INeedsDmxOutput
    {
        public AmericanDJStrobe(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            byte brightness = (byte)(GetMonochromeBrightnessFromColorBrightness().GetByteScale(250) + 5);

            byte strobe;
            if (this.strobeSpeed == 0)
                strobe = 255;
            else
                strobe = (byte)(2 + this.strobeSpeed.GetByteScale(125));

            DmxOutputPort.SendDmxData(baseDmxChannel, new byte[] { strobe, brightness });
        }
    }
}
