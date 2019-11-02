using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using Animatroller.Framework;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenBigEye : TriggeredSubBaseModule
    {
        public HalloweenBigEye(
            Framework.Expander.OscClient oscSender,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            OutputPower.Subscribe(x =>
            {
                if(!x)
                    oscSender.Send("/eyecontrol", 0);
            });

            PowerOn
                .RunAction(ins =>
                    {
                        //ins.WaitFor(S(2));
                        oscSender.Send("/eyecontrol", 1);

                        audioPlayer.PlayEffect("Short Laugh.wav", 1.0, 1.0);
                        ins.WaitFor(S(5.0));
                    })
                .TearDown(ins =>
                    {
                        oscSender.Send("/eyecontrol", 0);
                    });


            PowerOff
                .RunAction(ins =>
                {
                    ins.WaitFor(S(2));
//                    audioPlayer.PlayEffect("No Mercy.wav", 0.2);
                    ins.WaitFor(S(5));
                });
        }
    }
}
