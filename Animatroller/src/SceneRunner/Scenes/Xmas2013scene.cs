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
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class Xmas2013scene : BaseScene, ISceneRequiresAcnStream, ISceneSupportsSimulator, ISceneRequiresRaspExpander1
    {
        public enum States
        {
            Background,
            Music1,
            Music2,
            Vader
        }

        private Controller.Timeline<string> timeline;
        private Controller.StateMachine<States> stateMachine;
        private OperatingHours hours;
        private VirtualPixel1D allPixels;
        private DigitalInput buttonTest;
        private DigitalInput buttonStartInflatables;
        private DigitalInput buttonOverrideHours;
        private DigitalInput buttonBlue;
        private DigitalInput buttonRed;
        private Dimmer lightStar;
        private Dimmer lightHat1;
        private Dimmer lightHat2;
        private Dimmer lightHat3;
        private Dimmer lightHat4;
        private Dimmer lightSnow1;
        private Dimmer lightSnow2;
        private Dimmer lightStairs1;
        private Dimmer lightGarland1;
        private Dimmer lightGarland2;
        private Dimmer lightGarland3;
        private Dimmer lightGarland4;
        private Dimmer lightGarland5;
        private Dimmer lightString1;
        private Dimmer lightString2;
        private Dimmer lightXmasTree;
        private Dimmer lightDeerLarge;
        private Dimmer lightDeerSmall;
        private Dimmer lightTopperSmall;
        private Dimmer lightTopperLarge;
        private Dimmer lightNet1;
        private Dimmer lightNet2;
        private StrobeColorDimmer lightTreeUp;
        private StrobeColorDimmer lightVader;
        private StrobeColorDimmer light3wise;
        private Switch switchSanta;
        private Switch switchDeerHuge;
        private Switch switchButtonBlue;
        private Switch switchButtonRed;
        private AudioPlayer audioPlayer;
        private Controller.Sequence backgroundLoop;
        private Controller.Sequence music1Seq;
        private Controller.Sequence music2Seq;
        private Controller.Sequence fatherSeq;

        private Effect.Pulsating pulsatingEffect1;
        private Effect.Flicker flickerEffect;
        private Controller.Sequence candyCane;
        private Controller.Sequence twinkleSeq;
        private bool inflatablesRunning;

        private Effect.PopOut popOutPiano;
        private Effect.PopOut popOutDrums;
        private Effect.PopOut popOutDrumsFast;
        private Effect.PopOut popOutChord;
        private Effect.PopOut popOutSolo;
        private Effect.PopOut popOutSolo2;
        private Effect.PopOut popOutChoir;
        private Effect.PopOut popOutVoice;
        private Effect.PopOut popOutVocal2;
        private Effect.PopOut popOutVocalLong;
        private Effect.PopOut popOutEnd;

        public Xmas2013scene(IEnumerable<string> args)
        {
            hours = new OperatingHours("Hours");

            timeline = new Controller.Timeline<string>(1);
            stateMachine = new Controller.StateMachine<States>("Main");
            lightStar = new Dimmer("Star");
            lightHat1 = new Dimmer("Hat 1");
            lightHat2 = new Dimmer("Hat 2");
            lightHat3 = new Dimmer("Hat 3");
            lightHat4 = new Dimmer("Hat 4");
            lightSnow1 = new Dimmer("Snow 1");
            lightSnow2 = new Dimmer("Snow 2");
            lightStairs1 = new Dimmer("Stair 1");
            lightGarland1 = new Dimmer("Garland 1");
            lightGarland2 = new Dimmer("Garland 2");
            lightGarland3 = new Dimmer("Garland 3");
            lightGarland4 = new Dimmer("Garland 4");
            lightGarland5 = new Dimmer("Garland 5");
            lightString1 = new Dimmer("String 1");
            lightString2 = new Dimmer("String 1");
            lightXmasTree = new Dimmer("Xmas Tree");

            lightDeerLarge = new Dimmer("Deer Large");
            lightDeerSmall = new Dimmer("Deer Small");
            lightTreeUp = new StrobeColorDimmer("Tree up");
            switchSanta = new Switch("Santa");
            switchDeerHuge = new Switch("Deer Huge");
            lightTopperSmall = new Dimmer("Topper Small");
            lightTopperLarge = new Dimmer("Topper Large");
            lightNet1 = new Dimmer("Net 1");
            lightNet2 = new Dimmer("Net 2");
            lightVader = new StrobeColorDimmer("Vader");
            light3wise = new StrobeColorDimmer("3wise");

            pulsatingEffect1 = new Effect.Pulsating("Pulse FX 1", S(4), 0.4, 1.0, false);
            flickerEffect = new Effect.Flicker("Flicker", 0.5, 0.6, false);

            candyCane = new Controller.Sequence("Candy Cane");
            twinkleSeq = new Controller.Sequence("Twinkle");
            backgroundLoop = new Controller.Sequence("Background");
            music1Seq = new Controller.Sequence("Christmas Canon");
            music2Seq = new Controller.Sequence("Let It Go");
            fatherSeq = new Controller.Sequence("Father");

            allPixels = new VirtualPixel1D("All Pixels", 100);

            buttonTest = new DigitalInput("Test");
            buttonStartInflatables = new DigitalInput("Inflatables");
            buttonOverrideHours = new DigitalInput("Override hours", true);

            buttonBlue = new DigitalInput("Blue");
            buttonRed = new DigitalInput("Red");
            switchButtonBlue = new Switch("Blue");
            switchButtonRed = new Switch("Red");
            audioPlayer = new AudioPlayer("Audio");

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
        }

        private void ConfigureMusic1()
        {
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
                .AddDevice(lightXmasTree);

            popOutDrums
                .AddDevice(lightDeerLarge)
                .AddDevice(lightHat1);

            popOutDrumsFast
                //.AddDevice(lightCeiling1)
                .AddDevice(allPixels);

            popOutChord
                .AddDevice(lightTopperSmall);
                //.AddDevice(lightTree)
                //.AddDevice(lightCeiling2)
                //.AddDevice(lightCeiling3);

            popOutSolo
                //.AddDevice(lightNetLeft)
                //.AddDevice(lightNetRight)
                //.AddDevice(lightCeiling1)
                //.AddDevice(lightCeiling3)
                .SetPriority(2);

            popOutSolo2
                //.AddDevice(lightCeiling1)
                //.AddDevice(lightCeiling3)
                .SetPriority(2);

            popOutChoir
                .AddDevice(lightGarland2)
                .AddDevice(lightTopperLarge);
                //.AddDevice(lightCeiling1);

            popOutVoice
                .AddDevice(lightGarland1);
                //.AddDevice(lightCeiling3);

            popOutVocal2
                //.AddDevice(lightReindeers)
                .AddDevice(allPixels)
                .SetPriority(10);

            popOutVocalLong
                //.AddDevice(lightNetRight)
                //.AddDevice(lightGarlandRight)
                //.AddDevice(lightHatsRight)
                .SetPriority(10);

            popOutEnd
                .AddDevice(lightTreeUp)
                //.AddDevice(lightReindeers)
                //.AddDevice(lightHatsRight)
                //.AddDevice(lightCeiling1)
                //.AddDevice(lightCeiling2)
                //.AddDevice(lightCeiling3)
                //.AddDevice(lightGarlandRight)
                //.AddDevice(lightGarlandLeft)
                //.AddDevice(lightTreesRight)
                //.AddDevice(lightNetRight)
                //.AddDevice(lightNetLeft)
                .AddDevice(allPixels)
                .SetPriority(100);

            timeline.AddMs(0, "INIT");
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
                        allPixels.TurnOff();
                        break;

                    case 69:
                        state = 2;
                        //lightCeiling2.SetOnlyColor(Color.Green);
                        //lightCeiling3.SetOnlyColor(Color.Blue);
                        break;

                    case 136:
                        // First solo
                        state = 3;
                        allPixels.TurnOff();
                        //lightCeiling2.SetOnlyColor(Color.White);
                        //lightCeiling3.SetOnlyColor(Color.Red);
                        break;

                    case 265:
                        // First choir
                        allPixels.TurnOff();
                        state = 4;
                        break;

                    case 396:
                        // Vocal 2
                        state = 5;
                        allPixels.SetAllOnlyColor(Color.Blue);
                        break;

                    case 497:
                        // Second solo
                        state = 6;
                        allPixels.TurnOff();
                        //lightCeiling2.SetOnlyColor(Color.White);
                        //lightCeiling3.SetOnlyColor(Color.Red);
                        break;

                    case 561:
                        // End second solo
                        state = 7;
                        allPixels.TurnOff();
                        break;

                    case 585:
                        // End third solo
                        state = 8;
                        allPixels.TurnOff();
                        break;

                    case 721:
                        // End third solo
                        state = 9;
                        allPixels.TurnOff();
                        break;
                }

                switch (e.Code)
                {
                    case "INIT":
                        state = 0;
                        halfSolo = 0;
                        //lightCeiling1.SetColor(Color.White, 0);
                        //lightCeiling2.SetColor(Color.Blue, 0);
                        //lightCeiling3.SetColor(Color.Red, 0);
                        break;

                    case "N1":
                        popOutPiano.Pop(0.4);
                        if (state == 0)
                            allPixels.Inject(Color.Red, 0.5);
                        break;

                    case "N2":
                        popOutPiano.Pop(0.6);
                        if (state == 0)
                            allPixels.Inject(Color.White, 0.5);
                        break;

                    case "N3":
                        popOutPiano.Pop(0.8);
                        if (state == 0)
                            allPixels.Inject(Color.Blue, 0.5);
                        break;

                    case "N4":
                        popOutPiano.Pop(1.0);
                        if (state == 0)
                            allPixels.Inject(Color.Black, 0.0);
                        break;

                    case "Base":
                        popOutDrums.Pop(1.0);
                        if (state < 3)
                        {
                            allPixels.SetAllOnlyColor(Color.Purple);
                            popOutDrumsFast.Pop(1.0);
                        }
                        break;

                    case "Cymbal":
                        popOutDrums.Pop(1.0);
                        if (state < 3)
                        {
                            allPixels.SetAllOnlyColor(Color.Green);
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
                            allPixels.Inject(pixCol, 1.0);
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
        }

        private bool InflatablesRunning
        {
            get { return this.inflatablesRunning; }
            set
            {
                this.inflatablesRunning = value;
                Exec.SetKey("InflatablesRunning", inflatablesRunning.ToString());
            }
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonTest);
            sim.AddDigitalInput_FlipFlop(buttonOverrideHours);
            sim.AddDigitalInput_Momentarily(buttonStartInflatables);

            sim.AddDigitalInput_Momentarily(buttonBlue);
            sim.AddDigitalInput_Momentarily(buttonRed);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
            port.Connect(new Physical.PixelRope(allPixels, 0, 100), 4, 1);
            // GECE
            port.Connect(new Physical.PixelRope(allPixels, 0, 50), 2, 91);

            port.Connect(new Physical.GenericDimmer(lightStar, 1), 21);
            port.Connect(new Physical.GenericDimmer(lightHat1, 2), 21);
            port.Connect(new Physical.GenericDimmer(lightHat2, 3), 21);
            port.Connect(new Physical.GenericDimmer(lightHat3, 4), 21);
            port.Connect(new Physical.GenericDimmer(lightHat4, 5), 21);
            port.Connect(new Physical.GenericDimmer(lightSnow1, 6), 21);
            port.Connect(new Physical.GenericDimmer(lightSnow2, 7), 21);
            port.Connect(new Physical.GenericDimmer(lightTopperSmall, 8), 21);
            port.Connect(new Physical.GenericDimmer(lightTopperLarge, 9), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland1, 22), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland2, 10), 21);
            port.Connect(new Physical.GenericDimmer(lightString2, 11), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland3, 12), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland5, 13), 21);
            port.Connect(new Physical.GenericDimmer(lightXmasTree, 14), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland4, 15), 21);
            port.Connect(new Physical.GenericDimmer(lightNet1, 17), 21);
            port.Connect(new Physical.GenericDimmer(lightString1, 19), 21);
            port.Connect(new Physical.GenericDimmer(lightNet2, 20), 21);
            port.Connect(new Physical.GenericDimmer(lightStairs1, 24), 21);

            port.Connect(new Physical.GenericDimmer(switchDeerHuge, 3), 20);
            port.Connect(new Physical.GenericDimmer(switchSanta, 4), 20);
            port.Connect(new Physical.RGBStrobe(lightTreeUp, 20), 20);
            port.Connect(new Physical.RGBStrobe(lightVader, 30), 20);
            port.Connect(new Physical.RGBStrobe(light3wise, 40), 20);
            port.Connect(new Physical.GenericDimmer(lightDeerLarge, 100), 20);
            port.Connect(new Physical.GenericDimmer(lightDeerSmall, 101), 20);
        }

        public void WireUp1(Expander.Raspberry port)
        {
            port.DigitalInputs[4].Connect(buttonRed);
            port.DigitalInputs[5].Connect(buttonBlue);
            port.DigitalOutputs[0].Connect(switchButtonBlue);
            port.DigitalOutputs[1].Connect(switchButtonRed);

            port.Connect(audioPlayer);
        }

        private void TestAllPixels(Color color, double brightness, TimeSpan delay)
        {
            allPixels.SetAll(color, brightness);
            System.Threading.Thread.Sleep(delay);
        }

        private void EverythingOff()
        {
            //TODO
            pulsatingEffect1.Stop();
            flickerEffect.Stop();
        }

        public override void Start()
        {
            hours.AddRange("5:00 pm", "11:00 pm");

            bool.TryParse(Exec.GetKey("InflatablesRunning", false.ToString()) , out inflatablesRunning);

            buttonOverrideHours.ActiveChanged += (i, e) =>
                {
                    if (e.NewState)
                        hours.SetForced(true);
                    else
                        hours.SetForced(null);
                };

            candyCane
                .WhenExecuted
                .SetUp(() => allPixels.TurnOff())
                .Execute(instance =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int i = 0; i < spacing; i++)
                        {
                            allPixels.Inject((i % spacing) == 0 ? Color.Red : Color.White, 0.5);

                            instance.WaitFor(S(0.2), true);
                        }
                    }
                })
                .TearDown(() =>
                    {
                        allPixels.TurnOff();
                    });

            twinkleSeq
                .WhenExecuted
                .SetUp(() => allPixels.TurnOff())
                .Execute(instance =>
                {
                    var rnd = new Random();

                    while (!instance.IsCancellationRequested)
                    {
                        allPixels.SetAll(Color.White, 0.5);
                        instance.WaitFor(S(1.0), true);

                        int pixel1 = rnd.Next(allPixels.Pixels);
                        int pixel2 = rnd.Next(allPixels.Pixels);
                        var task1 = allPixels.FadeToAsync(instance, pixel1, Color.Red, 1.0, S(2.0));
                        var task2 = allPixels.FadeToAsync(instance, pixel2, Color.Red, 1.0, S(2.0));
                        Task.WaitAll(task1, task2);

                        instance.WaitFor(S(1.0), true);

                        task1 = allPixels.FadeToAsync(instance, pixel1, Color.White, 0.5, S(1.0));
                        task2 = allPixels.FadeToAsync(instance, pixel2, Color.White, 0.5, S(1.0));
                        Task.WaitAll(task1, task2);
                    }
                })
                .TearDown(() => allPixels.TurnOff());

            backgroundLoop
                .WhenExecuted
                .SetUp(() =>
                {
                    pulsatingEffect1.Start();
                    flickerEffect.Start();
                    //lightGarlandLeft.Brightness = 1;
                    //lightGarlandRight.Brightness = 1;
                    //lightIcicles.Brightness = 1;
                    //lightTreesRight.Brightness = 1;
                    //lightHatsRight.Brightness = 1;
                    //lightNetLeft.Brightness = 0.5;
                    //lightNetRight.Brightness = 0.5;
                    //lightReindeers.Brightness = 1;
                    //lightTree.Brightness = 1;
                    //lightCeiling1.SetColor(Color.Red, 0.5);
                    //lightCeiling2.SetColor(Color.Red, 0.5);
                    //lightCeiling3.SetColor(Color.Red, 0.5);

                    Executor.Current.Execute(twinkleSeq);
                })
                .TearDown(() =>
                {
                    Executor.Current.Cancel(twinkleSeq);

                    EverythingOff();
                });

            music1Seq
                .WhenExecuted
                .SetUp(() =>
                {
                    audioPlayer.CueTrack("21. Christmas Canon Rock");
                    ConfigureMusic1();
                    // Make sure it's ready
                    System.Threading.Thread.Sleep(800);

                    EverythingOff();
                })
                .Execute(instance =>
                {
                    audioPlayer.ResumeTrack();
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

            music2Seq
                .WhenExecuted
                .SetUp(() =>
                {
                    audioPlayer.CueTrack("05. Frozen - Let It Go");
                    //ConfigureMusic2();
                    // Make sure it's ready
                    System.Threading.Thread.Sleep(800);

                    EverythingOff();
                })
                .Execute(instance =>
                {
                    audioPlayer.ResumeTrack();
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

            fatherSeq
                .WhenExecuted
                .Execute(instance =>
                {
                    //Executor.Current.Execute(starwarsCane);

                    EverythingOff();

                    audioPlayer.CueTrack("01. Star Wars - Main Title");
                    // Make sure it's ready
                    instance.WaitFor(S(0.5));
                    audioPlayer.ResumeTrack();

                    //lightCeiling1.SetOnlyColor(Color.Yellow);
                    //lightCeiling2.SetOnlyColor(Color.Yellow);
                    //lightCeiling3.SetOnlyColor(Color.Yellow);
                    //pulsatingEffect2.Start();
                    instance.WaitFor(S(16));
                    //pulsatingEffect2.Stop();
                    audioPlayer.PauseTrack();
                    //Executor.Current.Cancel(starwarsCane);
                    allPixels.TurnOff();
                    instance.WaitFor(S(0.5));

                    //elJesus.SetPower(true);
                    //lightJesus.SetColor(Color.White, 0.3);

                    instance.WaitFor(S(1.5));

                    //elLightsaber.SetPower(true);
                    audioPlayer.PlayEffect("saberon");
                    instance.WaitFor(S(1));

                    lightVader.SetColor(Color.Red, 1.0);
                    audioPlayer.PlayEffect("father");
                    instance.WaitFor(S(3));

                    lightVader.TurnOff();
                    audioPlayer.PlayEffect("saberoff");
                    instance.WaitFor(S(0.5));
                    //elLightsaber.SetPower(false);
                    instance.WaitFor(S(1));

                    //lightJesus.TurnOff();
                    //elLightsaber.TurnOff();
                    //elJesus.TurnOff();
                });

            buttonStartInflatables.ActiveChanged += (o, e) =>
                {
                    if (e.NewState && hours.IsOpen)
                    {
                        InflatablesRunning = true;

                        switchDeerHuge.SetPower(true);
                        switchSanta.SetPower(true);
                    }
                };

            // Test Button
            buttonTest.ActiveChanged += (sender, e) =>
            {
                if (!e.NewState)
                    return;

                //Exec.Cancel(candyCane);

                //allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();
                //allPixels.SetAllOnlyColor(Color.Purple);
                //allPixels.RunEffect(new Effect2.Fader(0.0, 1.0), S(2.0)).Wait();
                //allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();

                //allPixels.SetAllOnlyColor(Color.Orange);
                //allPixels.RunEffect(new Effect2.Fader(0.0, 1.0), S(2.0)).Wait();

                //allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();

                //Exec.Execute(candyCane);
            };

            buttonBlue.ActiveChanged += (o, e) =>
                {
                    if (e.NewState)
                    {
                        if (hours.IsOpen)
                        {
                            if (stateMachine.CurrentState == States.Background)
                                stateMachine.SetMomentaryState(States.Music1);
                        }
                    }
                };

            buttonRed.ActiveChanged += (o, e) =>
            {
                switchButtonRed.SetPower(e.NewState);
                if (e.NewState)
                    audioPlayer.PauseTrack();
            };

            audioPlayer.AudioTrackDone += (o, e) =>
            {
                switchButtonBlue.SetPower(false);
            };

            pulsatingEffect1.AddDevice(lightStar);
            flickerEffect
                .AddDevice(lightHat1)
                .AddDevice(lightHat2)
                .AddDevice(lightHat3)
                .AddDevice(lightHat4);

            hours.OpenHoursChanged += (i, e) =>
            {
                if (e.IsOpenNow)
                {
                    stateMachine.SetBackgroundState(States.Background);
                    stateMachine.SetState(States.Background);
                    //lightTreeUp.SetColor(Color.Red, 1.0);
                    //lightSnow1.SetBrightness(1.0);
                    //lightSnow2.SetBrightness(1.0);

                    if (InflatablesRunning)
                    {
                        switchDeerHuge.SetPower(true);
                        switchSanta.SetPower(true);
                    }
                }
                else
                {
                    if (buttonOverrideHours.Active)
                        return;

                    stateMachine.Hold();
                    stateMachine.SetBackgroundState(null);
                    EverythingOff();
                    lightSnow1.TurnOff();
                    lightSnow2.TurnOff();
                    lightTreeUp.TurnOff();
                    System.Threading.Thread.Sleep(200);

                    switchDeerHuge.TurnOff();
                    switchSanta.TurnOff();
                    InflatablesRunning = false;
                }
            };

            stateMachine.ForFromSequence(States.Background, backgroundLoop);
            stateMachine.ForFromSequence(States.Music1, music1Seq);
            stateMachine.ForFromSequence(States.Music2, music2Seq);
            stateMachine.ForFromSequence(States.Vader, fatherSeq);

            //lightGarland1.Follow(hours);
            //lightGarland2.Follow(hours);
            //lightGarland3.Follow(hours);
            //lightGarland4.Follow(hours);
            //lightGarland5.Follow(hours);
            //lightXmasTree.Follow(hours);
            //lightStairs1.Follow(hours);
            //lightDeerSmall.Follow(hours);
            //lightDeerLarge.Follow(hours);
            //lightTopperLarge.Follow(hours);
            //lightTopperSmall.Follow(hours);
            //lightNet1.Follow(hours);
            //lightNet2.Follow(hours);
            //lightString1.Follow(hours);
            //lightString2.Follow(hours);

            //light3wise.Follow(hours);
            //lightVader.Follow(hours);
            //switchButtonBlue.Follow(hours);
            //switchButtonRed.Follow(hours);
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
