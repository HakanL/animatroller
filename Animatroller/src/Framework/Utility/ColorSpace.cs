using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public class HSV
    {
        public double Hue { get; set; }
        public double Saturation { get; set; }
        public double Value { get; set; }

        public HSV(Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            this.Hue = color.GetHue();
            this.Saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            this.Value = max / 255d;
        }

        public HSV(double hue, double saturation, double value)
        {
            this.Hue = hue;
            this.Saturation = saturation;
            this.Value = value;
        }

        public Color Color
        {
            get
            {
                double hue = this.Hue;
                double saturation = this.Saturation;
                double value = this.Value;

                int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
                double f = hue / 60 - Math.Floor(hue / 60);

                value = value * 255;
                int v = Convert.ToInt32(value);
                int p = Convert.ToInt32(value * (1 - saturation));
                int q = Convert.ToInt32(value * (1 - f * saturation));
                int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

                if (hi == 0)
                    return Color.FromArgb(255, v, t, p);
                else if (hi == 1)
                    return Color.FromArgb(255, q, v, p);
                else if (hi == 2)
                    return Color.FromArgb(255, p, v, t);
                else if (hi == 3)
                    return Color.FromArgb(255, p, q, v);
                else if (hi == 4)
                    return Color.FromArgb(255, t, p, v);
                else
                    return Color.FromArgb(255, v, p, q);
            }
        }

        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            var hsv = new HSV(color);
            hue = hsv.Hue;
            saturation = hsv.Saturation;
            value = hsv.Value;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            var hsv = new HSV(hue, saturation, value);

            return hsv.Color;
        }
    }
}
