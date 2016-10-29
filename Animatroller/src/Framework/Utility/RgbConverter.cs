using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Animatroller.Framework.Utility
{
    public static class RgbConverter
    {
        public static RGBAW GetRGBAW(Color inp)
        {
            var result = new RGBAW();

            result.W = (byte)(Math.Min(Math.Min(inp.R, inp.G), inp.B));

            int amber = inp.R - result.W;
            int a2 = (inp.G - result.W) * 2;
            if (amber > a2)
                result.A = (byte)a2;
            else
                result.A = (byte)amber;

            result.R = (byte)(inp.R - result.W - result.A);
            result.G = (byte)(inp.G - result.W - (result.A / 2));
            result.B = (byte)(inp.B - result.W);

            return result;
        }

        public static RGBW GetRGBW(Color inp)
        {
            var result = new RGBW();

            result.W = (byte)(Math.Min(Math.Min(inp.R, inp.G), inp.B));

            result.R = (byte)(inp.R - result.W);
            result.G = (byte)(inp.G - result.W);
            result.B = (byte)(inp.B - result.W);

            return result;
        }

        // The saturation is the colorfulness of a color relative to its own brightness.
        private static int GetSaturation(Color rgb)
        {
            // Find the smallest of all three parameters.
            float low = Math.Min(rgb.R, Math.Min(rgb.G, rgb.B));

            // Find the highest of all three parameters.
            float high = Math.Max(rgb.R, Math.Max(rgb.G, rgb.B));

            // The difference between the last two variables
            // divided by the highest is the saturation.
            return (int)Math.Round(100 * ((high - low) / high));
        }

        private static byte GetWhite(Color rgb)
        {
            return (byte)((255.0 - GetSaturation(rgb)) / 255.0 * (rgb.R + rgb.G + rgb.B) / 3.0);
        }

        /*colorRgbw rgbToRgbw(unsigned int red, unsigned int redMax,
                            unsigned int green, unsigned int greenMax,
                            unsigned int blue, unsigned int blueMax) {
            unsigned int white = 0;
            colorRgbw rgbw = {red, green, blue, white};
            rgbw.white = getWhite(rgbw, redMax, greenMax, blueMax);
            return rgbw;
        } */
    }
}
