using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Animatroller.Framework.Utility
{
    public static class RgbwConverter
    {

        public static Rgbw GetRgbw(Color rgb)
        {
            Rgbw result = new Rgbw
            {
                R = rgb.R,
                G = rgb.G,
                B = rgb.B
            };

            result.W = GetWhite(rgb);

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
