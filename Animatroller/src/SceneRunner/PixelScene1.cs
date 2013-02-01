using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class PixelScene1 : BaseScene
    {
        protected Pixel1D testPixels;
        protected Pixel1D testPixels2;
        protected DigitalInput buttonTest;

        protected Sequence candyCane;

        public PixelScene1(IEnumerable<string> args)
        {
            candyCane = new Sequence("Candy Cane");

            testPixels = new Pixel1D("G35", 50);
            testPixels2 = new Pixel1D("Strip60", 60);

            buttonTest = new DigitalInput("Test");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonTest);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.IOExpander port)
        {
            port.Connect(new Physical.PixelRope(testPixels));

            port.DigitalInputs[1].Connect(buttonTest);
        }

        public void WireUp(Expander.DMXPro port)
        {
        }

        public void WireUp(Expander.AcnStream port)
        {
            port.Connect(new Physical.PixelRope(testPixels2), 3, 181);
            port.Connect(new Physical.PixelRope(testPixels), 2, 91);
        }

        private void TestAllPixels(Color color, double brightness, TimeSpan delay)
        {
            testPixels.SetAll(color, brightness);
            testPixels2.SetAll(color, brightness);
            System.Threading.Thread.Sleep(delay);
        }

        public override void Start()
        {
            candyCane
                .WhenExecuted
                .SetUp(() => testPixels.TurnOff())
                .Execute(instance =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int i = 0; i < spacing; i++)
                        {
                            testPixels.Inject((i % spacing) == 0 ? Color.Red : Color.White, 1.0);

                            instance.WaitFor(S(0.2), true);
                        }
                    }
                })
                .TearDown(() => testPixels.TurnOff());


            musicSeq
                .Loop
                .WhenExecuted
                .SetUp(() =>
                    {
                        audioPlayer.CueTrack("21 Christmas Canon Rock");
                        // Make sure it's ready
                        System.Threading.Thread.Sleep(800);

                        EverythingOff();
                    })
                .Execute(instance =>
                    {
                        audioPlayer.PlayTrack();
                        var task = timeline.Start();

                        try
                        {
                            task.Wait(instance.CancelToken);

                            instance.WaitFor(S(10));
                        }
                        finally
                        {
                            timeline.Stop();
                            audioPlayer.PauseTrack();
                        }

                        if (!instance.IsCancellationRequested)
                            instance.WaitFor(S(2));
                        EverythingOff();

                        instance.WaitFor(S(2), true);


                        Executor.Current.Execute(backgroundLoop);
                        instance.WaitFor(S(30));
                        Executor.Current.Cancel(backgroundLoop);
                        EverythingOff();
                        instance.WaitFor(S(1));
                    })
                    .TearDown(() =>
                        {
                            EverythingOff();
                        });

            buttonSeq
                .Loop
                .WhenExecuted
                .Execute(instance =>
                {
                    buttonLightBlue.SetPower(true);
                    buttonLightRed.SetPower(false);
                    instance.WaitFor(S(0.2));
                    buttonLightBlue.SetPower(false);
                    buttonLightRed.SetPower(true);
                    instance.WaitFor(S(0.2));
                })
                .TearDown(() =>
                    {
                        buttonLightBlue.SetPower(false);
                        buttonLightRed.SetPower(false);
                    });

            fatherSeq
                .WhenExecuted
                .Execute(instance =>
                {
                    Executor.Current.Execute(starwarsCane);

                    EverythingOff();

                    audioPlayer.CueTrack("01 Star Wars_ Main Title");
                    // Make sure it's ready
                    instance.WaitFor(S(0.5));
                    audioPlayer.PlayTrack();

                    lightCeiling1.SetOnlyColor(Color.Yellow);
                    lightCeiling2.SetOnlyColor(Color.Yellow);
                    lightCeiling3.SetOnlyColor(Color.Yellow);
                    pulsatingEffect2.Start();
                    instance.WaitFor(S(16));
                    pulsatingEffect2.Stop();
                    audioPlayer.PauseTrack();
                    Executor.Current.Cancel(starwarsCane);
                    testPixels.TurnOff();
                    instance.WaitFor(S(0.5));

                    elJesus.SetPower(true);
                    lightJesus.SetColor(Color.White, 0.3);

                    instance.WaitFor(S(1.5));

                    elLightsaber.SetPower(true);
                    audioPlayer.PlayEffect("saberon");
                    instance.WaitFor(S(1));

                    lightVader.SetColor(Color.Red, 1.0);
                    audioPlayer.PlayEffect("father");
                    instance.WaitFor(S(3));

                    lightVader.TurnOff();
                    audioPlayer.PlayEffect("saberoff");
                    instance.WaitFor(S(0.5));
                    elLightsaber.SetPower(false);
                    instance.WaitFor(S(1));

                    lightJesus.TurnOff();
                    elLightsaber.TurnOff();
                    elJesus.TurnOff();
                });

            breathSeq
                .WhenExecuted
                .Execute(instance =>
                {
                    audioPlayer.PlayEffect("Darth Breathing");
                    instance.WaitFor(S(4));
                });

            laserSeq
                .WhenExecuted
                .SetUp(() =>
                    {
                        testPixels.TurnOff();
                        testPixels2.TurnOff();
                    })
                .Execute(instance =>
                {
                    audioPlayer.PlayEffect("lazer");

                    var cb = new ColorBrightness[6];
                    cb[0] = new ColorBrightness(Color.Black, 1.0);
                    cb[1] = new ColorBrightness(Color.Red, 1.0);
                    cb[2] = new ColorBrightness(Color.Orange, 1.0);
                    cb[3] = new ColorBrightness(Color.Yellow, 1.0);
                    cb[4] = new ColorBrightness(Color.Blue, 1.0);
                    cb[5] = new ColorBrightness(Color.White, 1.0);

                    for (int i = -6; i < testPixels2.Pixels; i++)
                    {
                        testPixels.SetColors(i, cb);
                        testPixels2.SetColors(i, cb);
                        System.Threading.Thread.Sleep(25);
                    }

                    instance.WaitFor(S(1));
                })
                .TearDown(() =>
                    {
                        testPixels.TurnOff();
                        testPixels2.TurnOff();
                    });

            stateMachine.ForFromSequence(States.Background, backgroundLoop);

            stateMachine.ForFromSequence(States.Music, musicSeq);

            stateMachine.ForFromSequence(States.Vader, fatherSeq);


            // Start Reindeer
            buttonStartReindeer.ActiveChanged += (sender, e) =>
                {
                    if (!e.NewState)
                        return;

                    if (hours.IsOpen)
                        buttonTest.SetPower(true);
                    else
                    {
                        TestAllPixels(Color.Red, 1.0, S(1));
                        TestAllPixels(Color.Red, 0.5, S(1));

                        TestAllPixels(Color.Green, 1.0, S(1));
                        TestAllPixels(Color.Green, 0.5, S(1));

                        TestAllPixels(Color.Blue, 1.0, S(1));
                        TestAllPixels(Color.Blue, 0.5, S(1));

                        TestAllPixels(Color.Purple, 1.0, S(1));
                        TestAllPixels(Color.Purple, 0.5, S(1));

                        TestAllPixels(Color.White, 1.0, S(1));
                        TestAllPixels(Color.White, 0.5, S(1));

                        testPixels.TurnOff();
                        testPixels2.TurnOff();
                    }
                };

            // Red Button
            buttonRed.ActiveChanged += (sender, e) =>
            {
                if (!e.NewState)
                    return;

                if (hours.IsOpen)
                {
                    stateMachine.SetMomentaryState(States.Vader);
                }
                else
                {
                    Executor.Current.Execute(laserSeq);
                }
            };

            // Blue Button
            buttonTest.ActiveChanged += (sender, e) =>
            {
                if (!e.NewState)
                    return;

                if (hours.IsOpen)
                {
                    if (stateMachine.CurrentState == States.Background)
                        stateMachine.SetMomentaryState(States.Music);
                }
                else
                {
                    Executor.Current.Execute(breathSeq);
                }
            };

            // Hours
            hours.OpenHoursChanged += (sender, e) =>
            {
                if (e.IsOpenNow)
                {
                    stateMachine.SetState(States.Background);

                    Executor.Current.Execute(buttonSeq);
                }
                else
                {
                    Executor.Current.Cancel(buttonSeq);

                    stateMachine.Hold();
                    buttonTest.SetPower(false);
                }
            };
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
            audioPlayer.PauseTrack();
        }
    }
}
