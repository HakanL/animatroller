using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Utility;
using System.Drawing;

namespace Animatroller.Framework.PhysicalDevice
{
    public class MarcGamutParH7 : BaseDMXStrobeLight
    {
        private byte componentMaster;
        private byte componentRed;
        private byte componentGreen;
        private byte componentBlue;
        private byte componentAmber;
        private byte componentWhite;
        private byte componentUV;

        public MarcGamutParH7(IApiVersion3 logicalDevice, int dmxChannel, int channels = 8)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void SetFromIData(IData data)
        {
            base.SetFromIData(data);

            if (data.ContainsKey(DataElements.ColorAmber) ||
                data.ContainsKey(DataElements.ColorRGB) ||
                data.ContainsKey(DataElements.ColorUltraViolet) ||
                data.ContainsKey(DataElements.ColorWhite))
            {
                object value;

                componentMaster = (this.colorBrightness.Brightness * (1 - Executor.Current.Blackout.Value)).GetByteScale();

                Color colorRGB;
                if (data.TryGetValue(DataElements.ColorRGB, out value))
                    colorRGB = (Color)value;
                else
                    colorRGB = this.colorBrightness.Color;

                componentRed = colorRGB.R;
                componentGreen = colorRGB.G;
                componentBlue = colorRGB.B;

                //TODO: WhiteOut
                if (data.TryGetValue(DataElements.ColorAmber, out value))
                    componentAmber = ((double)value).GetByteScale();
                if (data.TryGetValue(DataElements.ColorWhite, out value))
                    componentWhite = ((double)value).GetByteScale();
                if (data.TryGetValue(DataElements.ColorUltraViolet, out value))
                    componentUV = ((double)value).GetByteScale();
            }
            else
            {
                var color = GetColorFromColorBrightness();

                var rgbw = RgbwConverter.GetRgbw(color);

                componentMaster = 255;
                componentRed = rgbw.R;
                componentGreen = rgbw.G;
                componentBlue = rgbw.B;
                componentWhite = rgbw.W;
            }
        }

        protected override void Output()
        {
            byte strobe = (byte)(this.strobeSpeed == 0 ? 0 : this.strobeSpeed.GetByteScale(255 - 16) + 16);

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
                componentMaster,
                componentRed,
                componentGreen,
                componentBlue,
                componentAmber,
                componentWhite,
                componentUV,
                strobe
            });
        }
    }
}
