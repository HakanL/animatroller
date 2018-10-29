using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;
using System.Drawing;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenFlying : TriggeredSubBaseModule
    {
        Effect.Pulsating pulsatingLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);

        public HalloweenFlying(
            Dimmer3 eyes,
            ThroughputDevice fogStairsPump1,
            ThroughputDevice fogStairsPump2,
            StrobeColorDimmer3 fogStairsLight1,
            StrobeColorDimmer3 fogStairsLight2,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsatingLow.ConnectTo(eyes);

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(eyes);

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

                    eyes.SetBrightness(1);
                    audioPlayer.PlayEffect("Who is that knocking.wav");

                    ins.WaitFor(S(2.5));

                    fogStairsPump1.SetThroughput(0.4);
                    fogStairsLight1.SetColor(Color.Purple, 1.0);

                    fogStairsPump2.SetThroughput(0.4);
                    fogStairsLight2.SetColor(Color.Purple, 1.0);

                    ins.WaitFor(S(1.0));

                    fogStairsPump1.SetThroughput(0);
                    fogStairsPump2.SetThroughput(0);

                    ins.WaitFor(S(1.0));
                    fogStairsLight1.SetBrightness(0);
                    fogStairsLight2.SetBrightness(0);
                })
                .TearDown(ins =>
                {
                    pulsatingLow.Start(token: this.controlToken);
                });

            PowerOff
                .RunAction(ins =>
                {
                    audioPlayer.PlayEffect("Short Laugh.wav", 0.2);
                    ins.WaitFor(S(5));
                });
        }
    }
}
