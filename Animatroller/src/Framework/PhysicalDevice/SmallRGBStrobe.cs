﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class SmallRGBStrobe : BaseDMXStrobeLight
    {
        public SmallRGBStrobe(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            var color = GetColorFromColorBrightness();

            byte strobe = (byte)(this.strobeSpeed == 0 ? 127 : this.strobeSpeed.GetByteScale(121) + 128);

            DmxOutputPort.SendDmxData(baseDmxChannel, new byte[] { strobe, color.R, color.B, color.G });
        }
    }
}
