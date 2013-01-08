using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class ColorDimmer : Dimmer
    {
        protected Color color;

        public event EventHandler<ColorChangedEventArgs> ColorChanged;

        public ColorDimmer(string name)
            : base(name)
        {
        }

        protected void RaiseColorChanged()
        {
            var handler = ColorChanged;
            if(handler != null)
                handler(this, new ColorChangedEventArgs(this.Color, this.Brightness));
        }

        protected override void RaiseBrightnessChanged()
        {
            // Also has to raise color changed since some physical lights may set it in one shot
            base.RaiseBrightnessChanged();
            RaiseColorChanged();
        }

        public override void StartDevice()
        {
            RaiseBrightnessChanged();
        }

        public Color Color
        {
            get { return this.color; }
            set
            {
                if (this.color != value)
                {
                    this.color = value;

                    RaiseColorChanged();
                }
            }
        }

        public virtual ColorDimmer SetColor(Color color, double brightness)
        {
            this.Color = color;
            this.Brightness = brightness;

            return this;
        }

        public virtual ColorDimmer SetOnlyColor(Color color)
        {
            this.Color = color;

            return this;
        }

        public virtual ColorDimmer SetColor(Color color)
        {
            this.Color = color;
            this.Brightness = 1.0;

            return this;
        }
    }
}
