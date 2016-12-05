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
    public class MonopriceMovingHeadLight12chn : BaseDMXStrobeLight
    {
        protected double pan;
        protected double tilt;

        public MonopriceMovingHeadLight12chn(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void SetFromIData(ILogicalDevice logicalDevice, IData data)
        {
            base.SetFromIData(logicalDevice, data);

            object value;
            if (data.TryGetValue(DataElements.Pan, out value))
                this.pan = ((double)value).Limit(0, 540);

            if (data.TryGetValue(DataElements.Tilt, out value))
                this.tilt = ((double)value).Limit(0, 270);
        }

        protected override void Output()
        {
            byte function = (byte)(this.strobeSpeed == 0 ? 255 : this.strobeSpeed.GetByteScale(97) + 135);

            var color = GetColorFromColorBrightness();

            uint panValue = (uint)this.pan.LimitAndScale(0, 540).ScaleToMinMax(0, 65535);
            uint tiltValue = (uint)this.tilt.LimitAndScale(0, 270).ScaleToMinMax(0, 65535);

            DmxOutputPort.SendDimmerValues(this.baseDmxChannel, new byte[] {
                (byte)(panValue >> 8),      // Pan
                (byte)panValue,      // Pan fine
                (byte)(tiltValue >> 8),      // Tilt
                (byte)tiltValue,      // Tilt fine
                0,      // Vector speed (Pan/Tilt)
                function,   // Dimmer/Strobe
                color.R,
                color.G,
                color.B,
                0,      // Color Macros
                0,      // Vector speed (Color)
                0});    // Movement Macros
        }
    }
}
