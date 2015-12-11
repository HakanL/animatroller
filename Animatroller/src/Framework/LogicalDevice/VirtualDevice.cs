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
        private Action<double, IControlToken> xyz;

        public VirtualDevice(Action<double, IControlToken> xyz, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.xyz = xyz;
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
            xyz(brightness, token);
        }

        public void SetColor(Color color, double? brightness, IControlToken token)
        {
            throw new NotImplementedException();
        }
    }
}
