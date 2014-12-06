using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive.Subjects;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class ColorDimmer2 : Dimmer2
    {
        protected Color currentColor;
        protected ISubject<Color> inputColor;

        public ColorDimmer2([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.currentColor = Color.White;

            this.inputColor = new Subject<Color>();

            this.inputColor.Subscribe(x =>
            {
                if (this.currentColor != x)
                {
                    this.currentColor = x;
                }
            });
        }

        public override void SetInitialState()
        {
            base.SetInitialState();
            InputColor.OnNext(this.currentColor);
        }

        public ISubject<Color> InputColor
        {
            get
            {
                return this.inputColor;
            }
        }

        public Color Color
        {
            get { return this.currentColor; }
            set
            {
                this.inputColor.OnNext(value);
            }
        }

        public virtual ColorDimmer2 SetColor(Color color, double brightness)
        {
            this.Color = color;
            this.Brightness = brightness;

            return this;
        }

        public virtual ColorDimmer2 SetOnlyColor(Color color)
        {
            this.Color = color;

            return this;
        }

        public virtual ColorDimmer2 SetColor(Color color)
        {
            this.Color = color;
            this.Brightness = 1.0;

            return this;
        }
    }
}
