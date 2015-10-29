using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

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
