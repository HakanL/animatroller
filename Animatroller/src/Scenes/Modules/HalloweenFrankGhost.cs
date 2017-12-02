using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenFrankGhost : TriggeredSubBaseModule
    {
        Effect.Pulsating pulsatingLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Framework.Import.LevelsPlayback levelsPlayback = new Framework.Import.LevelsPlayback();

        public HalloweenFrankGhost(
            IReceivesColor light,
            DigitalOutput2 air,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsatingLow.ConnectTo(light);
            levelsPlayback.Output.Controls(b => light.SetBrightness(b, token: this.controlToken));

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(air, light);

                    air.SetValue(true, token: this.controlToken);
                    light.SetColor(Color.Red, token: this.controlToken);
                    pulsatingLow.Start(token: this.controlToken);
                }
                else
                {
                    pulsatingLow.Stop();

                    UnlockDevices();
                }
            });

            PowerOn
                .SetLoop(true)
                .SetMaxRuntime(S(60))
                .SetUp(ins =>
                {
                    pulsatingLow.Stop();
                })
                .RunAction(ins =>
                {
                    audioPlayer.PlayEffect("Thriller2.wav", levelsPlayback);
                    // The control token is optional since it's passed in via the Subroutine
                    light.SetColor(Color.Purple);
                    var cts = levelsPlayback.Start(this.controlToken);

                    ins.WaitFor(S(45));
                    cts.Cancel();
                })
                .TearDown(ins =>
                {
                    light.SetColor(Color.Red);
                    pulsatingLow.Start(token: this.controlToken);
                });

            PowerOff.RunAction(ins =>
                {
                    audioPlayer.PlayEffect("Happy Halloween.wav", 0.15);
                    ins.WaitFor(S(5));
                });
        }
    }
}
