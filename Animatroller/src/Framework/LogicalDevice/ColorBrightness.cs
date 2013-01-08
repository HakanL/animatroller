using System.Drawing;

namespace Animatroller.Framework.LogicalDevice
{
    public sealed class ColorBrightness
    {
        public Color Color;
        public double Brightness;

        public ColorBrightness(Color color, double brightness)
        {
            this.Color = color;
            this.Brightness = brightness;
        }

        public static ColorBrightness[] CreateBlank(int size)
        {
            var result = new ColorBrightness[size];
            for (int i = 0; i < size; i++)
                result[i] = new ColorBrightness(Color.Black, 0);

            return result;
        }
    }
}
