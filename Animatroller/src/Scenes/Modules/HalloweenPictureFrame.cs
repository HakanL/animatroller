using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenPictureFrame : TriggeredSubBaseModule
    {
        public HalloweenPictureFrame(
            CommandDevice medeaWizPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            PowerOn
                .SetLoop(true)
                .SetMaxRuntime(S(60))
                .RunAction(ins =>
                {
                    medeaWizPlayer.SendCommand(this.controlToken, 0x02);
                    ins.WaitFor(S(10));
                })
                .TearDown(ins =>
                {
                    medeaWizPlayer.SendCommand(this.controlToken, 0xff);
                });
        }
    }
}
