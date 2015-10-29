using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;

namespace Animatroller.Framework.LogicalDevice
{
    public class GroupDimmer : Group<IReceivesBrightness>, IReceivesBrightness
    {
        public GroupDimmer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        public void SetBrightness(double brightness, IControlToken token = null)
        {
            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    member.SetBrightness(brightness, token);
                }
            }
        }

        public double Brightness
        {
            get
            {
                return double.NaN;
            }
        }
    }
}
