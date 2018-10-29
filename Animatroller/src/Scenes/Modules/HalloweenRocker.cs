using System;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;

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
                        switch (random.Next(2))
                        {
                            case 0:
                                audioPlayer.PlayEffect("Disgusting Things.wav");
                                ins.WaitFor(S(6), true);
                                break;

                            case 1:
                                audioPlayer.PlayEffect("Guts Boy.wav");
                                ins.WaitFor(S(4), true);
                                break;
                        }
                        isRockingLadyTalking = false;

                        ins.WaitFor(S(20.0));
                    }
                })
                .TearDown(ins =>
                {
                    audioPlayer.StopFX();
                    ladyEyes.SetValue(false, token: this.controlToken);
                    rockingMotor.SetValue(false, token: this.controlToken);
                });

            PowerOn
                .SetLoop(true)
                .RunAction(ins =>
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
