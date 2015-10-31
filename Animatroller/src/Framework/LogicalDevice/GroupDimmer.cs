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
            if (token == null)
                token = this.internalLock;

            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    member.SetBrightness(brightness, token);
                }
            }
        }

        public void SetBrightness(double brightness, IData additionalData, IControlToken token = null)
        {
            if (token == null)
                token = this.internalLock;

            var data = new Data();
            data[DataElements.Brightness] = brightness;
            if (additionalData != null)
                foreach (var kvp in additionalData)
                    data[kvp.Key] = kvp.Value;

            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    member.PushData(token, data);
                }
            }
        }

        public void PushData(IControlToken token, IData data)
        {
            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    member.PushData(token, data);
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
