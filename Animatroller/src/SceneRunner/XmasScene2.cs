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
    internal class XmasScene2 : BaseScene
    {
        public enum States
        {
            Background,
            Music,
            Vader
        }

        protected OperatingHours hours;
        protected Pixel1D testPixels;
        protected Pixel1D testPixels2;
        protected Dimmer lightNetRight;
        protected Dimmer lightGarlandRight;
        protected Dimmer lightHatsRight;
        protected Dimmer lightTreesRight;
        protected Dimmer lightReindeers;
        protected Dimmer lightIcicles;
        protected Dimmer lightNetLeft;
        protected Dimmer lightTree;
        protected Dimmer lightGarlandLeft;
        protected Dimmer lightUnused1;
        protected Dimmer lightUnused2;
        protected StrobeColorDimmer lightJesus;
        protected StrobeColorDimmer lightCeiling1;
        protected StrobeColorDimmer lightCeiling2;
        protected StrobeColorDimmer lightCeiling3;
        protected StrobeColorDimmer lightVader;
        protected Switch buttonLightRed;
        protected Switch buttonLightBlue;
        protected Switch elLightsaber;
        protected Switch elJesus;
        protected Switch bigReindeer;

        protected DigitalInput buttonBlue;
        protected DigitalInput buttonRed;
        protected DigitalInput buttonStartReindeer;

        protected Timeline timeline;
        protected StateMachine<States> stateMachine;
        protected Effect.PopOut popOutPiano;
        protected Effect.PopOut popOutDrums;
        protected Effect.PopOut popOutDrumsFast;
        protected Effect.PopOut popOutChord;
        protected Effect.PopOut popOutSolo;
        protected Effect.PopOut popOutSolo2;
        protected Effect.PopOut popOutChoir;
        protected Effect.PopOut popOutVoice;
        protected Effect.PopOut popOutVocal2;
        protected Effect.PopOut popOutVocalLong;
        protected Effect.PopOut popOutEnd;
        protected Effect.Pulsating pulsatingEffect1;
        protected Effect.Pulsating pulsatingEffect2;
        protected Physical.NetworkAudioPlayer audioPlayer;

        protected Sequence candyCane;
        protected Sequence starwarsCane;
        protected Sequence backgroundLoop;
        protected Sequence musicSeq;
        protected Sequence buttonSeq;
        protected Sequence fatherSeq;
        protected Sequence breathSeq;
        protected Sequence laserSeq;


        public XmasScene2(IEnumerable<string> args)
        {
            hours = new OperatingHours("Hours");
            if (!args.Contains("TEST"))
            {
                hours.AddRange("5:00 pm", "10:00 pm");
                hours.AddRange("5:00 am", "7:00 am");
            }

            testPixels = new Pixel1D("G35", 50);
            testPixels2 = new Pixel1D("Strip60", 60);
            lightNetRight = new Dimmer("Net Right");
            lightGarlandRight = new Dimmer("Garland Right");
            lightHatsRight = new Dimmer("Hats Right");
            lightTreesRight = new Dimmer("Trees Right");
            lightReindeers = new Dimmer("Reindeers");
            lightIcicles = new Dimmer("Icicles");
            lightNetLeft = new Dimmer("Net Left");
            lightTree = new Dimmer("Tree");
            lightGarlandLeft = new Dimmer("Garland Left");
            lightUnused1 = new Dimmer("Unused 1");
            lightUnused2 = new Dimmer("Unused 2");
            lightJesus = new StrobeColorDimmer("Jesus");
            lightCeiling1 = new StrobeColorDimmer("Ceiling 1");
            lightCeiling2 = new StrobeColorDimmer("Ceiling 2");
            lightCeiling3 = new StrobeColorDimmer("Ceiling 3");
            lightVader = new StrobeColorDimmer("Vader");
            buttonLightRed = new Switch("Button Red");
            buttonLightBlue = new Switch("Button Blue");
            elLightsaber = new Switch("Lightsaber");
            elJesus = new Switch("Jesus Halo");
            bigReindeer = new Switch("Big Reindeer");

            buttonBlue = new DigitalInput("Button Blue");
            buttonRed = new DigitalInput("Button Red");
            buttonStartReindeer = new DigitalInput("Start Reindeer");

            timeline = new Timeline();
            stateMachine = new StateMachine<States>("Main");
            candyCane = new Sequence("Candy Cane");
            starwarsCane = new Sequence("Starwars Cane");
            backgroundLoop = new Sequence("Background");
            musicSeq = new Sequence("Christmas Canon");
            buttonSeq = new Sequence("Buttons");
            fatherSeq = new Sequence("Father");
            breathSeq = new Sequence("Breath");
            laserSeq = new Sequence("Laser");

            pulsatingEffect1 = new Effect.Pulsating("Pulse FX 1", S(2), 0.3, 1.0, false);
            pulsatingEffect2 = new Effect.Pulsating("Pulse FX 2", S(2), 0.3, 1.0, false);

            popOutPiano = new Effect.PopOut("Piano", S(0.4));
            popOutDrums = new Effect.PopOut("Drums", S(0.4));
            popOutDrumsFast = new Effect.PopOut("Drums Fast", S(0.3));
            popOutChord = new Effect.PopOut("Chord", S(0.4));
            popOutSolo = new Effect.PopOut("Solo", S(0.3));
            popOutSolo2 = new Effect.PopOut("Solo 2", S(0.2));
            popOutChoir = new Effect.PopOut("Choir", S(1.0));
            popOutVoice = new Effect.PopOut("Voice", S(1.0));
            popOutVocal2 = new Effect.PopOut("Vocal 2", S(2.0));
            popOutVocalLong = new Effect.PopOut("Vocal Long", S(5.0));
            popOutEnd = new Effect.PopOut("End", S(5.0));

            popOutPiano
                .AddDevice(lightIcicles);

            popOutDrums
                .AddDevice(lightReindeers)
                .AddDevice(lightHatsRight);

            popOutDrumsFast
                .AddDevice(lightCeiling1)
                .AddDevice(testPixels);

            popOutChord
                .AddDevice(lightTree)
                .AddDevice(lightCeiling2)
                .AddDevice(lightCeiling3);

            popOutSolo
                .AddDevice(lightNetLeft)
                .AddDevice(lightNetRight)
                .AddDevice(lightCeiling1)
                .AddDevice(lightCeiling3)
                .SetPriority(2);

            popOutSolo2
                .AddDevice(lightCeiling1)
                .AddDevice(lightCeiling3)
                .SetPriority(2);

            popOutChoir
                .AddDevice(lightGarlandRight)
                .AddDevice(lightTreesRight)
                .AddDevice(lightCeiling1);

            popOutVoice
                .AddDevice(lightGarlandLeft)
                .AddDevice(lightCeiling3);

            popOutVocal2
                .AddDevice(lightReindeers)
                .AddDevice(testPixels)
                .SetPriority(10);

            popOutVocalLong
                .AddDevice(lightNetRight)
                .AddDevice(lightGarlandRight)
                .AddDevice(lightHatsRight)
                .SetPriority(10);

            popOutEnd
                .AddDevice(lightIcicles)
                .AddDevice(lightReindeers)
                .AddDevice(lightHatsRight)
                .AddDevice(lightCeiling1)
                .AddDevice(lightCeiling2)
                .AddDevice(lightCeiling3)
                .AddDevice(lightGarlandRight)
                .AddDevice(lightGarlandLeft)
                .AddDevice(lightTreesRight)
                .AddDevice(lightNetRight)
                .AddDevice(lightNetLeft)
                .AddDevice(testPixels)
                .SetPriority(100);


            timeline.Add(0, "INIT");
            timeline.PopulateFromCSV("Christmas Canon Rock All Labels.csv");
            int state = 0;
            int halfSolo = 0;

            timeline.TimelineTrigger += (sender, e) =>
                {
                    switch (e.Step)
                    {
                        case 62:
                            // First drum
                            state = 1;
                            testPixels.TurnOff();
                            break;

                        case 69:
                            state = 2;
                            lightCeiling2.SetOnlyColor(Color.Green);
                            lightCeiling3.SetOnlyColor(Color.Blue);
                            break;

                        case 136:
                            // First solo
                            state = 3;
                            testPixels.TurnOff();
                            lightCeiling2.SetOnlyColor(Color.White);
                            lightCeiling3.SetOnlyColor(Color.Red);
                            break;

                        case 265:
                            // First choir
                            testPixels.TurnOff();
                            state = 4;
                            break;

                        case 396:
                            // Vocal 2
                            state = 5;
                            testPixels.SetAllOnlyColor(Color.Blue);
                            break;

                        case 497:
                            // Second solo
                            state = 6;
                            testPixels.TurnOff();
                            lightCeiling2.SetOnlyColor(Color.White);
                            lightCeiling3.SetOnlyColor(Color.Red);
                            break;

                        case 561:
                            // End second solo
                            state = 7;
                            testPixels.TurnOff();
                            break;

                        case 585:
                            // End third solo
                            state = 8;
                            testPixels.TurnOff();
                            break;

                        case 721:
                            // End third solo
                            state = 9;
                            testPixels.TurnOff();
                            break;
                    }

                    switch (e.Code)
                    {
                        case "INIT":
                            state = 0;
                            halfSolo = 0;
                            lightCeiling1.SetColor(Color.White, 0);
                            lightCeiling2.SetColor(Color.Blue, 0);
                            lightCeiling3.SetColor(Color.Red, 0);
                            break;

                        case "N1":
                            popOutPiano.Pop(0.4);
                            if (state == 0)
                                testPixels.Inject(Color.Red, 0.5);
                            break;

                        case "N2":
                            popOutPiano.Pop(0.6);
                            if (state == 0)
                                testPixels.Inject(Color.White, 0.5);
                            break;

                        case "N3":
                            popOutPiano.Pop(0.8);
                            if (state == 0)
                                testPixels.Inject(Color.Blue, 0.5);
                            break;

                        case "N4":
                            popOutPiano.Pop(1.0);
                            if (state == 0)
                                testPixels.Inject(Color.Black, 0.0);
                            break;

                        case "Base":
                            popOutDrums.Pop(1.0);
                            if (state < 3)
                            {
                                testPixels.SetAllOnlyColor(Color.Purple);
                                popOutDrumsFast.Pop(1.0);
                            }
                            break;

                        case "Cymbal":
                            popOutDrums.Pop(1.0);
                            if (state < 3)
                            {
                                testPixels.SetAllOnlyColor(Color.Green);
                                popOutDrumsFast.Pop(1.0);
                            }
                            break;

                        case "Chord":
                            popOutChord.Pop(1.0);
                            break;

                        case "Solo":
                            popOutSolo.Pop(1.0);
                            if ((halfSolo++ % 2) == 0)
                                popOutSolo2.Pop(0.8);
                            if (state == 3 || state == 6 || state == 8)
                            {
                                Color pixCol = Color.Black;
                                switch (e.Step % 4)
                                {
                                    case 0:
                                        pixCol = Color.Red;
                                        break;
                                    case 1:
                                        pixCol = Color.Yellow;
                                        break;
                                    case 2:
                                        pixCol = Color.Blue;
                                        break;
                                    case 3:
                                        pixCol = Color.Pink;
                                        break;
                                }
                                testPixels.Inject(pixCol, 1.0);
                            }
                            break;

                        case "Choir":
                            popOutChoir.Pop(1.0);
                            break;

                        case "Voice":
                            popOutVoice.Pop(1.0);
                            break;

                        case "Vocal2":
                            popOutVocal2.Pop(1.0);
                            break;

                        case "Long":
                            popOutVocalLong.Pop(1.0);
                            break;

                        case "LongUp":
                            // TODO
                            break;

                        case "Down":
                            // TODO
                            break;

                        case "End":
                            EverythingOff();
                            popOutEnd.Pop(1.0);
                            break;

                        default:
                            log.Info("Unhandled code: " + e.Code);
                            break;
                    }
                };

            audioPlayer = new Physical.NetworkAudioPlayer(
                Properties.Settings.Default.NetworkAudioPlayerIP,
                Properties.Settings.Default.NetworkAudioPlayerPort);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonBlue);
            sim.AddDigitalInput_Momentarily(buttonRed);
            sim.AddDigitalInput_Momentarily(buttonStartReindeer);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.IOExpander port)
        {
            port.Connect(new Physical.PixelRope(testPixels));

            port.DigitalInputs[0].Connect(buttonRed);
            port.DigitalInputs[1].Connect(buttonBlue);
            port.DigitalInputs[2].Connect(buttonStartReindeer);

            port.DigitalOutputs[0].Connect(buttonLightRed);
            port.DigitalOutputs[1].Connect(buttonLightBlue);
            port.DigitalOutputs[2].Connect(elLightsaber);
            port.DigitalOutputs[3].Connect(elJesus);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.GenericDimmer(lightNetRight, 1));
            port.Connect(new Physical.GenericDimmer(lightGarlandRight, 2));
            port.Connect(new Physical.GenericDimmer(lightHatsRight, 3));
            port.Connect(new Physical.GenericDimmer(lightTreesRight, 4));
            port.Connect(new Physical.GenericDimmer(lightReindeers, 5));
            port.Connect(new Physical.GenericDimmer(lightIcicles, 6));
            port.Connect(new Physical.GenericDimmer(lightNetLeft, 7));
            port.Connect(new Physical.GenericDimmer(lightTree, 8));
            port.Connect(new Physical.GenericDimmer(lightGarlandLeft, 9));
            port.Connect(new Physical.GenericDimmer(lightUnused1, 10));
            port.Connect(new Physical.GenericDimmer(lightUnused2, 11));
            port.Connect(new Physical.GenericDimmer(bigReindeer, 12));
            port.Connect(new Physical.SmallRGBStrobe(lightJesus, 20));
            port.Connect(new Physical.RGBStrobe(lightCeiling1, 30));
            port.Connect(new Physical.RGBStrobe(lightCeiling2, 50));
            port.Connect(new Physical.RGBStrobe(lightCeiling3, 60));
            port.Connect(new Physical.RGBStrobe(lightVader, 40));
        }

        public void WireUp(Expander.AcnStream port)
        {
            port.Connect(new Physical.GenericDimmer(lightNetRight, 181), 7);
            port.Connect(new Physical.GenericDimmer(lightHatsRight, 182), 7);

            port.Connect(new Physical.PixelRope(testPixels2), 3, 181);
            port.Connect(new Physical.PixelRope(testPixels), 2, 91);

            //            port.JoinDmxUniverse(1);
        }

        private void EverythingOff()
        {
            popOutPiano.Stop();
            popOutDrums.Stop();
            popOutDrumsFast.Stop();
            popOutChord.Stop();
            popOutSolo.Stop();
            popOutSolo2.Stop();
            popOutChoir.Stop();
            popOutVoice.Stop();
            popOutVocal2.Stop();
            popOutVocalLong.Stop();
            popOutEnd.Stop();

            pulsatingEffect1.Stop();
            pulsatingEffect2.Stop();
            lightGarlandLeft.TurnOff();
            lightGarlandRight.TurnOff();
            lightIcicles.TurnOff();
            lightTreesRight.TurnOff();
            lightNetLeft.TurnOff();
            lightNetRight.TurnOff();
            lightReindeers.TurnOff();
            lightHatsRight.TurnOff();
            lightTree.TurnOff();

            lightCeiling1.TurnOff();
            lightCeiling2.TurnOff();
            lightCeiling3.TurnOff();
            lightVader.TurnOff();
        }

        private void TestAllPixels(Color color, double brightness, TimeSpan delay)
        {
            testPixels.SetAll(color, brightness);
            testPixels2.SetAll(color, brightness);
            System.Threading.Thread.Sleep(delay);
        }

        public override void Start()
        {
            pulsatingEffect1.AddDevice(lightHatsRight);
            pulsatingEffect2
                .AddDevice(lightCeiling1)
                .AddDevice(lightCeiling2)
                .AddDevice(lightCeiling3);

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

            starwarsCane
                .WhenExecuted
                .Execute(instance =>
                {
                    const int spacing = 4;

                    testPixels.TurnOff();

                    while (!instance.CancelToken.IsCancellationRequested)
                    {
                        for (int i = 0; i < spacing; i++)
                        {
                            switch (i % spacing)
                            {
                                case 0:
                                case 1:
                                    testPixels.InjectRev(Color.Yellow, 1.0);
                                    break;
                                case 2:
                                case 3:
                                    testPixels.InjectRev(Color.Orange, 0.2);
                                    break;
                            }

                            instance.WaitFor(S(0.1));

                            if (instance.IsCancellationRequested)
                                break;
                        }
                    }
                    testPixels.TurnOff();
                });

            backgroundLoop
                .WhenExecuted
                .SetUp(() =>
                    {
                        pulsatingEffect1.Start();
                        lightGarlandLeft.Brightness = 1;
                        lightGarlandRight.Brightness = 1;
                        lightIcicles.Brightness = 1;
                        lightTreesRight.Brightness = 1;
                        lightHatsRight.Brightness = 1;
                        lightNetLeft.Brightness = 0.5;
                        lightNetRight.Brightness = 0.5;
                        lightReindeers.Brightness = 1;
                        lightTree.Brightness = 1;
                        lightCeiling1.SetColor(Color.Red, 0.5);
                        lightCeiling2.SetColor(Color.Red, 0.5);
                        lightCeiling3.SetColor(Color.Red, 0.5);

                        Executor.Current.Execute(candyCane);
                    })
                .TearDown(() =>
                    {
                        Executor.Current.Cancel(candyCane);

                        EverythingOff();
                    });


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
                        bigReindeer.SetPower(true);
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
            buttonBlue.ActiveChanged += (sender, e) =>
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
                    bigReindeer.SetPower(false);
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
