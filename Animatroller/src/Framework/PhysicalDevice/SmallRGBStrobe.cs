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
        public IDmxOutput DmxOutputPort { protected get; set; }

        public SmallRGBStrobe(ColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.ColorChanged += (sender, e) =>
                {
                    // Handles brightness as well

                    var hsv = new HSV(e.NewColor);
                    hsv.Value = hsv.Value * e.NewBrightness;

                    Output(dmxChannel, hsv.Color, 0);
                };
        }

        public SmallRGBStrobe(ColorDimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            logicalDevice.InputColor.Subscribe(x =>
                {
                    var hsv = new HSV(x);
                    hsv.Value = hsv.Value * logicalDevice.Brightness;

                    Output(dmxChannel, hsv.Color, 0);
                });

            logicalDevice.InputBrightness.Subscribe(x =>
            {
                var hsv = new HSV(logicalDevice.Color);
                hsv.Value = hsv.Value * x.Value;

                Output(dmxChannel, hsv.Color, 0);
            });
        }

        public SmallRGBStrobe(StrobeColorDimmer logicalDevice, int dmxChannel)
            : this((ColorDimmer)logicalDevice, dmxChannel)
        {
            logicalDevice.StrobeSpeedChanged += (sender, e) =>
                {
                    var hsv = new HSV(logicalDevice.Color);
                    hsv.Value = hsv.Value * logicalDevice.Brightness;

                    Output(dmxChannel, hsv.Color, e.NewSpeed);
                };
        }

        public SmallRGBStrobe(StrobeColorDimmer2 logicalDevice, int dmxChannel)
            : this((ColorDimmer2)logicalDevice, dmxChannel)
        {
            logicalDevice.InputStrobeSpeed.Subscribe(x =>
            {
                var hsv = new HSV(logicalDevice.Color);
                hsv.Value = hsv.Value * logicalDevice.Brightness;

                Output(dmxChannel, hsv.Color, x.Value);
            });
        }

        private void Output(int baseDmxChannel, System.Drawing.Color color, double strobeSpeed)
        {
            var strobe = (byte)(strobeSpeed == 0 ? 127 : strobeSpeed.GetByteScale(121) + 128);

            DmxOutputPort.SendDimmerValues(baseDmxChannel, new byte[] { strobe, color.R, color.B, color.G });
        }
    }
}
