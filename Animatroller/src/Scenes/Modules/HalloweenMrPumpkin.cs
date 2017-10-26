using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenMrPumpkin : TriggeredSubBaseModule
    {
        Effect.Pulsating pulsatingLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Framework.Import.LevelsPlayback levelsPlayback = new Framework.Import.LevelsPlayback();

        public HalloweenMrPumpkin(
            Dimmer3 light,
            DigitalOutput2 air,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsatingLow.ConnectTo(light);
            levelsPlayback.Output.Controls(b => light.SetBrightness(b, this.controlToken));

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(air, light);

                    air.SetValue(true, this.controlToken);
                    pulsatingLow.Start(token: this.controlToken);
                }
                else
                {
                    pulsatingLow.Stop();
                    UnlockDevices();
                }
            });

            PowerOn
                .RunAction(ins =>
                {
                    pulsatingLow.Stop();

                    audioPlayer.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav", levelsPlayback);
                    levelsPlayback.Start(this.controlToken);

                    ins.WaitFor(S(8));
                })
                .TearDown(ins =>
                {
                    pulsatingLow.Start(token: this.controlToken);
                });
        }
    }
}
