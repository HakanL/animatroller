using System;
using System.Drawing;

namespace Animatroller.Framework.LogicalDevice
{
    public class ColorDimmer3 : Dimmer3, IReceivesColor
    {
        public ColorDimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.currentData[DataElements.Color] = Color.White;
        }

        public Color Color
        {
            get { return (Color)this.currentData[DataElements.Color]; }
        }

        public void SetColor(Color color, double? brightness = 1.0, IControlToken token = null)
        {
            if (brightness.HasValue)
            {
                PushData(token,
                    Tuple.Create(DataElements.Brightness, (object)brightness),
                    Tuple.Create(DataElements.Color, (object)color)
                    );
            }
            else
                PushData(token, Tuple.Create(DataElements.Color, (object)color));
        }
    }
}
