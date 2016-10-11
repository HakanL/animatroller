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
    /// RGBW - RGBW = 4 channels
    /// </summary>
    public class RGBW : BaseDMXStrobeLight
    {
        public RGBW(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            var color = GetColorFromColorBrightness();

            var rgbw = RgbConverter.GetRGBW(color);

            DmxOutputPort.SendDimmerValues(this.baseDmxChannel, new byte[] { rgbw.R, rgbw.G, rgbw.B, rgbw.W });
        }
    }
}
