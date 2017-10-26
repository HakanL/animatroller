using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenSpiderDrop : TriggeredSubBaseModule
    {
        public HalloweenSpiderDrop(
            IReceivesBrightness eyesLight,
            DigitalOutput2 drop,
            DigitalOutput2 venom,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(drop, venom, eyesLight);
                }
                else
                {
                    UnlockDevices();
                }
            });

            PowerOn.RunAction(ins =>
                {
                    audioPlayer.PlayNewEffect("348 Spider Hiss.wav", 0, 1);
                    eyesLight.SetBrightness(1);
                    drop.SetValue(true);
                    ins.WaitFor(S(0.2));
                    venom.SetValue(true);
                    ins.WaitFor(S(2.0));
                    venom.SetValue(false);
                    ins.WaitFor(S(2.0));

                    ins.WaitFor(S(5.0));
                })
                .TearDown(ins =>
                {
                    drop.SetValue(false);
                    venom.SetValue(false);
                    eyesLight.SetBrightness(0);
                });
        }
    }
}
