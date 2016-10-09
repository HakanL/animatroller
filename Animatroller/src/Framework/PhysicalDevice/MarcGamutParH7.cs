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
    public class MarcGamutParH7 : BaseDMXStrobeLight
    {
        public MarcGamutParH7(IApiVersion3 logicalDevice, int dmxChannel, int channels = 8)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            byte strobe = (byte)(this.strobeSpeed == 0 ? 0 : this.strobeSpeed.GetByteScale(255 - 16) + 16);

            var color = GetColorFromColorBrightness();

            var rgbw = RgbwConverter.GetRgbw(color);

            // 8-channel mode
            // 1 = Dimmer
            // 2 = Red
            // 3 = Green
            // 4 = Blue
            // 5 = Amber
            // 6 = White
            // 7 = UV
            // 8 = Strobe (0-15 = off, 16-255 = slow to fast)

            DmxOutputPort.SendDimmerValues(this.baseDmxChannel, new byte[] {
                255,
                rgbw.R,
                rgbw.G,
                rgbw.B,
                0,
                rgbw.W,
                0,
                strobe });
        }
    }
}
