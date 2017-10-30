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
        Effect.Pulsating pulsating = new Effect.Pulsating(S(4), 0.1, 1.0, false);

        public HalloweenSpiderDrop(
            IReceivesBrightness eyesLight,
            IReceivesBrightness smallSpiderEyes,
            DigitalOutput2 drop,
            DigitalOutput2 venom,
            StrobeDimmer3 strobeLight,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsating.ConnectTo(smallSpiderEyes);

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(drop, venom, eyesLight, strobeLight, smallSpiderEyes);

                    pulsating.Start(token: this.controlToken);
                }
                else
                {
                    pulsating.Stop();
                    UnlockDevices();
                }
            });

            PowerOn.RunAction(ins =>
                {
                    pulsating.Stop();
                    audioPlayer.PlayNewEffect("348 Spider Hiss.wav", 0, 1);
                    eyesLight.SetBrightness(1);
                    drop.SetValue(true);
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
                    drop.SetValue(false);
                    venom.SetValue(false);
                    eyesLight.SetBrightness(0);
                    strobeLight.SetBrightnessStrobeSpeed(0, 0);
                    pulsating.Start(token: this.controlToken);
                    ins.WaitFor(S(1.0));
                });
        }
    }
}
