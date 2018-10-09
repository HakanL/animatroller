using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Serilog;

namespace Animatroller.Framework.LogicalDevice
{
    public class GroupDimmer : Group<IReceivesBrightness>, IReceivesBrightness
    {
        public GroupDimmer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }
        /*FIXME
                public void SetBrightness(double brightness, IData additionalData, IControlToken token = null)
                {
                    if (token == null)
                        token = this.internalLock;

                    foreach (var member in AllMembers)
                    {
                        var data = GetFrameBuffer(token, member);

                        data[DataElements.Brightness] = brightness;
                        if (additionalData != null)
                            foreach (var kvp in additionalData)
                                data[kvp.Key] = kvp.Value;

                        member.PushOutput(token);
                    }
                }
        */
        public void BuildDefaultData(IData data)
        {
            // Do nothing
        }

        public void SetData(IData data)
        {
            SetData(channel: Channel.Main, token: null, data: data);
        }

        public void SetData(IControlToken token, IData data)
        {
            SetData(channel: Channel.Main, token: token, data: data);
        }

        public void SetData(IChannel channel, IControlToken token, IData data)
        {
            if (token == null)
                token = this.internalLock;

            foreach (var member in AllMembers)
            {
                var frame = GetFrameBuffer(channel, token, member);

                foreach (var kvp in data)
                    frame[kvp.Key] = kvp.Value;

                member.PushOutput(channel, token);
            }
        }
    }
}
