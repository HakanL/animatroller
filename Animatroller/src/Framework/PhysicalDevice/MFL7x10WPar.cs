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
    public class MFL7x10WPar : BaseDMXStrobeLight
    {
        private byte componentMaster;
        private byte componentRed;
        private byte componentGreen;
        private byte componentBlue;
        private byte componentWhite;

        public MFL7x10WPar(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void SetFromIData(ILogicalDevice logicalDevice, IData data)
        {
            base.SetFromIData(logicalDevice, data);

            if (data.ContainsKey(DataElements.ColorRGB) ||
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
                if (data.TryGetValue(DataElements.ColorWhite, out value))
                    componentWhite = ((double)value).GetByteScale();
            }
            else
            {
                var rgbw = RgbConverter.GetRGBW(this.colorBrightness.Color);

                componentMaster = this.colorBrightness.Brightness.GetByteScale();
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
            // 5 = White
            // 6 = Strobe (0-15 = off, 16-255 = slow to fast)
            // 7 = Speed (0 = off)
            // 8 = Color mixing and run automatically (0 = off)

            DmxOutputPort.SendDmxData(this.baseDmxChannel, new byte[] {
                componentMaster,
                componentRed,
                componentGreen,
                componentBlue,
                componentWhite,
                strobe,
                0,
                0
            });
        }
    }
}
