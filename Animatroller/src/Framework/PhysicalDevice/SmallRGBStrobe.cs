using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class SmallRGBStrobe : BaseDevice, INeedsDmxOutput
    {
        private int baseDmxChannel;

        public IDmxOutput DmxOutputPort { protected get; set; }

        public SmallRGBStrobe(ColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            baseDmxChannel = dmxChannel;

            logicalDevice.ColorChanged += (sender, e) =>
                {
                    // Handles brightness as well

                    var hsv = new HSV(e.NewColor);
                    hsv.Value = hsv.Value * e.NewBrightness;
                    var color = hsv.Color;

                    DmxOutputPort.SendDimmerValues(dmxChannel + 1, new byte[] { color.R, color.B, color.G });
                };
        }

        public SmallRGBStrobe(ColorDimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            baseDmxChannel = dmxChannel;

            logicalDevice.InputColor.Subscribe(x =>
                {
                    var hsv = new HSV(x);
                    hsv.Value = hsv.Value * logicalDevice.Brightness;
                    var color = hsv.Color;

                    SetColor(color);
                });

            logicalDevice.InputBrightness.Subscribe(x =>
            {
                var hsv = new HSV(logicalDevice.Color);
                hsv.Value = hsv.Value * x.Value;
                var color = hsv.Color;

                SetColor(color);
            });
        }

        public SmallRGBStrobe(StrobeColorDimmer logicalDevice, int dmxChannel)
            : this((ColorDimmer)logicalDevice, dmxChannel)
        {
            baseDmxChannel = dmxChannel;

            logicalDevice.StrobeSpeedChanged += (sender, e) =>
                {
                    SetStrobeSpeed(e.NewSpeed);
                };
        }

        private void SetStrobeSpeed(double speed)
        {
            var val = (byte)(speed == 0 ? 127 : speed.GetByteScale(121) + 128);

            DmxOutputPort.SendDimmerValue(baseDmxChannel, val);
        }

        private void SetColor(System.Drawing.Color color)
        {
            DmxOutputPort.SendDimmerValues(baseDmxChannel + 1, new byte[] { color.R, color.B, color.G });
        }

        public override void StartDevice()
        {
            base.StartDevice();

            SetStrobeSpeed(0);
        }
    }
}
