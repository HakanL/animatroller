using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Utility;

namespace Animatroller.Framework.PhysicalDevice
{
    public class MonopriceMovingHeadLight : BaseDMXRGBStrobeLight
    {
        public MonopriceMovingHeadLight(ColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        public MonopriceMovingHeadLight(ColorDimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        public MonopriceMovingHeadLight(StrobeColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        public MonopriceMovingHeadLight(StrobeColorDimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            byte function = (byte)(this.strobeSpeed == 0 ? 255 : this.strobeSpeed.GetByteScale(105) + 135);

            var color = GetColorFromColorBrightness();

            var rgbw = RgbwConverter.GetRgbw(color);

            byte autoRun = 0;

            DmxOutputPort.SendDimmerValues(this.baseDmxChannel, new byte[] { function, rgbw.R, rgbw.B, rgbw.G, rgbw.W, autoRun });
        }
    }
}
