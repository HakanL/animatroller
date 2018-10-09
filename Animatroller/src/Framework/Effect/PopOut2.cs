using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Effect
{
    public class PopOut2 : GroupDimmer
    {
        private double defaultStartBrightness;
        private TimeSpan defaultSweepDuration;

        public PopOut2(
            TimeSpan defaultSweepDuration,
            double defaultStartBrightness = 1.0,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.defaultStartBrightness = defaultStartBrightness;
            this.defaultSweepDuration = defaultSweepDuration;
        }

        public Task Pop(double? startBrightness = null, Color? color = null, int priority = 1, IChannel channel = null, TimeSpan? sweepDuration = null)
        {
            var token = TakeControl(channel: currentChannel, priority: priority, name: Name);

            if (color.HasValue)
            {
                this.members.OfType<IReceivesColor>().ToList()
                    .ForEach(x => x.SetColor(color.Value, null, channel, token));
            }

            return Executor.Current.MasterEffect.Fade(
                device: this,
                start: startBrightness ?? this.defaultStartBrightness,
                end: 0.0,
                durationMs: (int)(sweepDuration ?? this.defaultSweepDuration).TotalMilliseconds,
                token: token)
                .ContinueWith(x =>
                {
                    token.Dispose();
                });
        }

        public void ConnectTo(IReceivesBrightness device)
        {
            Add(device);
        }
    }
}

