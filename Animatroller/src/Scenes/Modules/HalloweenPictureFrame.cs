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
                    //ins.WaitFor(S(2));
                    medeaWizPlayer.SendCommand(this.controlToken, 1);
                    ins.WaitFor(S(6));
                })
                .TearDown(ins =>
                {
                    medeaWizPlayer.SendCommand(this.controlToken, 255);
                });
        }
    }
}
