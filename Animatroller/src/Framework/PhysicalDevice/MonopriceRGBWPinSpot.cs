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
    public class MonopriceRGBWPinSpot : BaseDMXStrobeLight
    {
        public MonopriceRGBWPinSpot(ColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        public MonopriceRGBWPinSpot(ColorDimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        public MonopriceRGBWPinSpot(StrobeColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        public MonopriceRGBWPinSpot(StrobeColorDimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            byte function = (byte)(this.strobeSpeed == 0 ? 255 : this.strobeSpeed.GetByteScale(104) + 135);

            var color = GetColorFromColorBrightness();

            var rgbw = RgbwConverter.GetRgbw(color);

            byte autoRun = 0;

            DmxOutputPort.SendDimmerValues(this.baseDmxChannel, new byte[] { function, rgbw.R, rgbw.G, rgbw.B, rgbw.W, autoRun });
        }
    }
}
