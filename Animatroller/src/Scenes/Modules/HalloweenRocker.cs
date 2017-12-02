using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Controller = Animatroller.Framework.Controller;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenRocker : TriggeredSubBaseModule
    {
        Effect.Pulsating pulsatingRocking = new Effect.Pulsating(S(4), 0.1, 0.5, false);
        bool isRockingLadyTalking;
        Controller.Subroutine sub = new Controller.Subroutine();

        public HalloweenRocker(
            DigitalOutput2 rockingMotor,
            DigitalOutput2 ladyEyes,
            StrobeColorDimmer3 strobeLight,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsatingRocking.ConnectTo(strobeLight, Utils.Data(DataElements.Color, Color.HotPink));

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(rockingMotor, ladyEyes, strobeLight);
                    audioPlayer.SetBackgroundVolume(0.2);
                    audioPlayer.PlayBackground();
                    sub.Run();
                }
                else
                {
                    audioPlayer.PauseBackground();
                    UnlockDevices();
                    Exec.Cancel(sub);
                }
            });

            sub
                .SetUp(ins =>
                {
                    ladyEyes.SetValue(true, token: this.controlToken);
                    rockingMotor.SetValue(true, token: this.controlToken);
                })
                .RunAction(ins =>
                {
                    while (!ins.IsCancellationRequested)
                    {
                        isRockingLadyTalking = true;
                        audioPlayer.PlayEffect("A conversation with Mother.wav");
                        ins.WaitFor(S(42), true);
                        isRockingLadyTalking = false;

                        ins.WaitFor(S(15.0));
                    }
                })
                .TearDown(ins =>
                {
                    audioPlayer.PauseFX();
                    ladyEyes.SetValue(false, token: this.controlToken);
                    rockingMotor.SetValue(false, token: this.controlToken);
                });

            PowerOn.RunAction(ins =>
                {
                    pulsatingRocking.Start(token: this.controlToken);
                    if (!isRockingLadyTalking)
                    {
                        switch (random.Next(3))
                        {
                            case 0:
                                audioPlayer.PlayEffect("032495843-old-woman-cough.wav");
                                break;

                            case 1:
                                audioPlayer.PlayEffect("049951942-grampy-old-woman-cackle.wav");
                                break;

                            case 2:
                                audioPlayer.PlayEffect("053851373-old-ungly-female-laughter.wav");
                                break;
                        }
                    }

                    ins.WaitFor(S(7));
                })
                .TearDown(ins =>
                {
                    pulsatingRocking.Stop();
                });
        }
    }
}
