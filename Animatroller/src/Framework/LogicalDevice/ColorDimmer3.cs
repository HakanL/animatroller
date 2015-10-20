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
    public class ColorDimmer3 : Dimmer3, IReceivesColor, ISendsColor
    {
        protected ControlSubject<Color, IControlToken> color;

        public ColorDimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.color = new ControlSubject<Color, IControlToken>(Color.White, HasControl);
        }

        public ControlledObserver<Color> GetColorObserver(IControlToken token = null)
        {
            return new ControlledObserver<Color>(token ?? GetCurrentOrNewToken(), this.color);
        }

        public ControlledObserverRGB GetRgbObserver(IControlToken token = null)
        {
            this.color.OnNext(Color.Black);
            this.brightness.OnNext(1.0);

            return new ControlledObserverRGB(token ?? GetCurrentOrNewToken(), this.color);
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
                // Note that this will only match the token when called on the same thread as
                // where control was taken (TakeControl)
                this.color.OnNext(value, Executor.Current.GetControlToken(this));
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
