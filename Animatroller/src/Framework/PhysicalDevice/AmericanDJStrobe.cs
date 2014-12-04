using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class AmericanDJStrobe : BaseStrobeLight, INeedsDmxOutput
    {
        protected int baseDmxChannel;

        public IDmxOutput DmxOutputPort { protected get; set; }

        public AmericanDJStrobe(Dimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public AmericanDJStrobe(Dimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public AmericanDJStrobe(StrobeDimmer logicalDevice, int dmxChannel)
            : this((Dimmer)logicalDevice, dmxChannel)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public AmericanDJStrobe(ILogicalDevice logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        protected override void Output()
        {
            byte brightness = (byte)(GetMonochromeBrightnessFromColorBrightness().GetByteScale(250) + 5);

            byte strobe;
            if (this.strobeSpeed == 0)
                strobe = 255;
            else
                strobe = (byte)(2 + this.strobeSpeed.GetByteScale(125));

            DmxOutputPort.SendDimmerValues(baseDmxChannel, new byte[] { strobe, brightness });
        }
    }
}
