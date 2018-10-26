using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenSpiderSquirt : TriggeredSubBaseModule
    {
        public HalloweenSpiderSquirt(
            IReceivesBrightness eyesLight,
            DigitalOutput2 venom,
            StrobeDimmer3 strobeLight,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(venom, eyesLight, strobeLight);
                }
                else
                {
                    UnlockDevices();
                }
            });

            PowerOn.RunAction(ins =>
                {
                    audioPlayer.PlayNewEffect("348 Spider Hiss.wav");
                    eyesLight.SetBrightness(1);
                    ins.WaitFor(S(0.2));
                    strobeLight.SetBrightnessStrobeSpeed(1, 1);
                    venom.SetValue(true);
                    ins.WaitFor(S(2.0));
                    venom.SetValue(false);
                    strobeLight.SetBrightnessStrobeSpeed(0, 0);
                    ins.WaitFor(S(2.0));
                })
                .TearDown(ins =>
                {
                    venom.SetValue(false);
                    eyesLight.SetBrightness(0);
                    strobeLight.SetBrightnessStrobeSpeed(0, 0);
                    ins.WaitFor(S(1.0));
                });
        }
    }
}
