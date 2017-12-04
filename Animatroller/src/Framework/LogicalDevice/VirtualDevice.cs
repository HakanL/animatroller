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
    public class VirtualDevice : SingleOwnerDevice, IApiVersion3, IReceivesBrightness, IReceivesColor
    {
        private Action<double> action;

        public VirtualDevice(Action<double> action, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.action = action;

            OutputData.Subscribe(x =>
            {
                object value;

                if (x.TryGetValue(DataElements.Brightness, out value))
                {
                    action((double)value);
                }
            });
        }

        public double Brightness
        {
            get { return double.NaN; }
        }

        public Color Color
        {
            get { return Color.Transparent; }
        }

        public void SetBrightness(double brightness, IControlToken token)
        {
            action(brightness);
        }

        public void SetColor(Color color, double? brightness, IControlToken token)
        {
            throw new NotImplementedException();
        }

        public override void BuildDefaultData(IData data)
        {
            // Do nothing
        }
    }
}
