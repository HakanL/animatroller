using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class GenericDimmer : BaseDevice, INeedsDmxOutput
    {
        public IDmxOutput DmxOutputPort { protected get; set; }

        public GenericDimmer(Dimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.BrightnessChanged += (sender, e) =>
            {
                DmxOutputPort.SendDimmerValue(dmxChannel, e.NewBrightness.GetByteScale());
            };
        }

        public GenericDimmer(Switch logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.PowerChanged += (sender, e) =>
                {
                    DmxOutputPort.SendDimmerValue(dmxChannel, (byte)(e.NewState ? 255 : 0));
                };
        }

        public GenericDimmer(DigitalOutput2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.InputPower.Subscribe(x =>
            {
                DmxOutputPort.SendDimmerValue(dmxChannel, (byte)(x ? 255 : 0));
            });
        }

        public GenericDimmer(Dimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.InputBrightness.Subscribe(x =>
            {
                DmxOutputPort.SendDimmerValue(dmxChannel, x.Value.GetByteScale());
            });
        }
    }
}
