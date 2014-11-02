using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class AmericanDJStrobe : BaseDevice, INeedsDmxOutput
    {
        private int baseDmxChannel;

        public IDmxOutput DmxOutputPort { protected get; set; }

        public AmericanDJStrobe(Dimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;

            logicalDevice.BrightnessChanged += (sender, e) =>
            {
                var dimmerValue = e.NewBrightness.GetByteScale(250) + 5;

                DmxOutputPort.SendDimmerValue(dmxChannel + 1, (byte)dimmerValue);
            };
        }

        public AmericanDJStrobe(Dimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;

            logicalDevice.InputBrightness.Subscribe(x =>
            {
                var dimmerValue = x.Value.GetByteScale(250) + 5;

                DmxOutputPort.SendDimmerValue(dmxChannel + 1, (byte)dimmerValue);
            });
        }

        public AmericanDJStrobe(StrobeDimmer logicalDevice, int dmxChannel)
            : this((Dimmer)logicalDevice, dmxChannel)
        {
            this.baseDmxChannel = dmxChannel;

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

        public override void StartDevice()
        {
            base.StartDevice();

            DmxOutputPort.SendDimmerValue(this.baseDmxChannel, 255);
        }
    }
}
