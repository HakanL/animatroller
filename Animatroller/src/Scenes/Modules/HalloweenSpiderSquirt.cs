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
        Effect.Pulsating pulsatingEyes = new Effect.Pulsating(S(4), 0.1, 0.5, false);

        public HalloweenSpiderSquirt(
            IReceivesBrightness headEyesLight,
            DigitalOutput2 venom,
            StrobeDimmer3 strobeLight,
            AudioPlayer audioPlayerSpider,
            AudioPlayer audioPlayerHead = null,
            IReceivesBrightness spiderEyesLight = null,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsatingEyes.ConnectTo(headEyesLight);

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(venom, spiderEyesLight, strobeLight, headEyesLight);
                }
                else
                {
                    UnlockDevices();
                }
            });

            PowerOn.RunAction(ins =>
                {
                    ins.WaitFor(S(1));
                    venom.SetValue(true);

                    pulsatingEyes.Start(token: this.controlToken);

                    if (audioPlayerHead != null)
                    {
                        ins.WaitFor(S(0.3));
                        audioPlayerHead.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav");
                        ins.WaitFor(S(3.0));
                    }

                    ins.WaitFor(S(1));
                    audioPlayerSpider.PlayNewEffect("348 Spider Hiss.wav");
                    spiderEyesLight?.SetBrightness(1);
                    ins.WaitFor(S(0.2));
                    strobeLight.SetBrightnessStrobeSpeed(1, 1);
                    ins.WaitFor(S(2.0));
                    venom.SetValue(false);
                    ins.WaitFor(S(1.0));
                    strobeLight.SetBrightnessStrobeSpeed(0, 0);
                    //pulsatingEyes.Stop();
                    ins.WaitFor(S(1.0));
                })
                .TearDown(ins =>
                {
                    pulsatingEyes.Stop();
                    venom.SetValue(false);
                    spiderEyesLight?.SetBrightness(0);
                    strobeLight.SetBrightnessStrobeSpeed(0, 0);
                    ins.WaitFor(S(1.0));
                });
        }
    }
}
