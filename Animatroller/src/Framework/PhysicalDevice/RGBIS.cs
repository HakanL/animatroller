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
    /// <summary>
    /// RGBIS - RGBW, master, strobe = 6 channels
    /// </summary>
    public class RGBIS : BaseDMXStrobeLight
    {
        public RGBIS(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            byte strobe = (byte)(this.strobeSpeed == 0 ? 0 : this.strobeSpeed.GetByteScale(255 - 20) + 20);

            var color = GetColorFromColorBrightness();

            var rgbw = RgbConverter.GetRGBW(color);

            DmxOutputPort.SendDmxData(this.baseDmxChannel, new byte[] { rgbw.R, rgbw.G, rgbw.B, rgbw.W, 255, strobe });
        }
    }
}
