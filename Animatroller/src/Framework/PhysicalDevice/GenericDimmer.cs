using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class GenericDimmer : BaseLight, INeedsDmxOutput
    {
        protected int baseDmxChannel;

        public IDmxOutput DmxOutputPort { protected get; set; }

        public GenericDimmer(Dimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public GenericDimmer(Switch logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public GenericDimmer(DigitalOutput2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public GenericDimmer(Dimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public GenericDimmer(ILogicalDevice logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        protected override void Output()
        {
            double brightness = GetMonochromeBrightnessFromColorBrightness();

            DmxOutputPort.SendDimmerValue(baseDmxChannel, brightness.GetByteScale());
        }
    }
}
