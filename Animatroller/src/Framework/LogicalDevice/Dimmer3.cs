using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class Dimmer3 : SingleOwnerDevice, IReceivesBrightness
    {
        public Dimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        public override void BuildDefaultData(IData data)
        {
            data[DataElements.Brightness] = 0.0;
        }

        public void SetBrightness(double brightness, IControlToken token = null)
        {
            PushData(token, Utils.AdditionalData(DataElements.Brightness, brightness));
        }

        public double Brightness
        {
            get
            {
                return (double)this.currentData[DataElements.Brightness];
            }
        }
    }
}
