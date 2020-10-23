using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenSpider : TriggeredSubBaseModule
    {
        Effect.Pulsating pulsating = new Effect.Pulsating(S(4), 0.1, 1.0, false);

        public HalloweenSpider(
            IReceivesBrightness spiderEyes,
            StrobeDimmer3 strobeLight,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsating.ConnectTo(spiderEyes);

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(strobeLight, spiderEyes);

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
                    spiderEyes?.SetBrightness(1);
                    strobeLight.SetBrightnessStrobeSpeed(1, 1);
                    ins.WaitFor(S(5.0));
                    strobeLight.SetBrightnessStrobeSpeed(0, 0);
                    ins.WaitFor(S(2.0));
                })
                .TearDown(ins =>
                {
                    spiderEyes?.SetBrightness(0);
                    strobeLight.SetBrightnessStrobeSpeed(0, 0);
                    pulsating.Start(token: this.controlToken);
                    ins.WaitFor(S(1.0));
                });
        }
    }
}
