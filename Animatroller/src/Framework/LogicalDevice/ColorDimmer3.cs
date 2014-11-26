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
    public class ColorDimmer3 : Dimmer3
    {
        protected ControlSubject<Color, IControlToken> color;

        public ColorDimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.color = new ControlSubject<Color, IControlToken>(Color.White, HasControl);
        }

        public ControlledObserver<Color> GetColorObserver(IControlToken controlToken)
        {
            return new ControlledObserver<Color>(controlToken, this.color);
        }

        public IObservable<Color> OutputColor
        {
            get
            {
                return this.color.DistinctUntilChanged();
            }
        }

        public Color Color
        {
            get { return this.color.Value; }
            set
            {
                if (HasControl(null))
                    // Only allow if nobody is controlling us
                    this.color.OnNext(value);
            }
        }

        protected override void UpdateOutput()
        {
            base.UpdateOutput();

            this.color.OnNext(this.color.Value);
        }

        public void SetColor(Color color, double brightness)
        {
            this.Color = color;
            this.Brightness = brightness;
        }

        public void SetOnlyColor(Color color)
        {
            this.Color = color;
        }

        public void SetColor(Color color)
        {
            this.Color = color;
            this.Brightness = 1.0;
        }
    }
}
