﻿using System;
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

namespace Animatroller.Scenes
{
    internal class Xmas2013scene : BaseScene,
        ISceneRequiresAcnStream
    {
        public enum States
        {
            Background,
            Music1,
            Music2,
            Vader
        }

        private Controller.Timeline<string> timeline1;
        private Controller.EnumStateMachine<States> stateMachine;
        private Controller.IntStateMachine hatLightState;
        private OperatingHours hours;
        private VirtualPixel1D allPixels;
        private VirtualPixel1D starwarsPixels;
        private VirtualPixel1D saberPixels;
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput buttonTest;
        private DigitalInput buttonStartInflatables;
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput buttonOverrideHours;
        private DigitalInput buttonBlue;
        private DigitalInput buttonRed;
        private StrobeColorDimmer lightJesus;
        private Dimmer lightStar;
        private Dimmer lightHat1;
        private Dimmer lightHat2;
        private Dimmer lightHat3;
        private Dimmer lightHat4;
        private Dimmer lightSnow1;
        private Dimmer lightSnow2;
        private Dimmer lightStairs1;
        private Dimmer lightStairs2;
        private Dimmer lightGarland1;
        private Dimmer lightGarland2;
        private Dimmer lightGarland3;
        private Dimmer lightGarland4;
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
        private Switch elJesus;
        private AudioPlayer audioPlayer;
        private Controller.Sequence backgroundLoop;
        private Controller.Sequence music1Seq;
        private Controller.Sequence fatherSeq;
        private Controller.Sequence offHours1Seq;
        private Controller.Sequence offHours2Seq;
        private Controller.Sequence starwarsCane;
        private Controller.Sequence waveformSeq;

        private Effect.Pulsating pulsatingEffect1;
        private Effect.Pulsating pulsatingStar;
        private Effect.Fader faderIn;
        private Effect.Flicker flickerEffect;
        private Controller.Sequence candyCane;
        private Controller.Sequence twinkleSeq;
        private bool inflatablesRunning;
        private int whichMusic;
        private DateTime? mute;

        private Effect.PopOut popOut1Piano;
        private Effect.PopOut popOut1Drums;
        private Effect.PopOut popOut1DrumsFast;
        private Effect.PopOut popOut1Chord;
        private Effect.PopOut popOut1Solo;
        private Effect.PopOut popOut1Solo2;
        private Effect.PopOut popOut1Choir;
        private Effect.PopOut popOut1Voice;
        private Effect.PopOut popOut1Vocal2;
        private Effect.PopOut popOut1VocalLong;
        private Effect.PopOut popOut1End;
        private Expander.OscServer oscServer;
        private Expander.Raspberry raspberry = new Expander.Raspberry();

        public Xmas2013scene(IEnumerable<string> args)
        {
            hours = new OperatingHours("Hours");

            timeline1 = new Controller.Timeline<string>(1);
            stateMachine = new Controller.EnumStateMachine<States>("Main");
            hatLightState = new Controller.IntStateMachine("Hats");
            lightJesus = new StrobeColorDimmer("Jesus");
            lightStar = new Dimmer("Star");
            lightHat1 = new Dimmer("Hat 1");
            lightHat2 = new Dimmer("Hat 2");
            lightHat3 = new Dimmer("Hat 3");
            lightHat4 = new Dimmer("Hat 4");
            lightSnow1 = new Dimmer("Snow 1");
            lightSnow2 = new Dimmer("Snow 2");
            lightStairs1 = new Dimmer("Stair 1");
            lightStairs2 = new Dimmer("Stairs 2");
            lightGarland1 = new Dimmer("Garland 1");
            lightGarland2 = new Dimmer("Garland 2");
            lightGarland3 = new Dimmer("Garland 3");
            lightGarland4 = new Dimmer("Garland 4");
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

            pulsatingEffect1 = new Effect.Pulsating(S(4), 0.4, 1.0, false);
            pulsatingStar = new Effect.Pulsating(S(2), 0.2, 0.4, false);
            flickerEffect = new Effect.Flicker(0.5, 0.6, false);
            faderIn = new Effect.Fader(S(2), 0.0, 1.0, false);

            candyCane = new Controller.Sequence("Candy Cane");
            twinkleSeq = new Controller.Sequence("Twinkle");
            backgroundLoop = new Controller.Sequence("Background");
            music1Seq = new Controller.Sequence("Christmas Canon");
            starwarsCane = new Controller.Sequence("Starwars Cane");
            fatherSeq = new Controller.Sequence("Father");
            offHours1Seq = new Controller.Sequence("Off hours 1");
            offHours2Seq = new Controller.Sequence("Off hours 2");
            waveformSeq = new Controller.Sequence("Waveform");

            allPixels = new VirtualPixel1D(100);
            starwarsPixels = new VirtualPixel1D(50);
            saberPixels = new VirtualPixel1D(60);

            buttonTest = new DigitalInput("Test");
            buttonStartInflatables = new DigitalInput("Inflatables");
            buttonOverrideHours = new DigitalInput("Override hours", true);

            buttonBlue = new DigitalInput("Blue");
            buttonRed = new DigitalInput("Red");
            switchButtonBlue = new Switch("Blue");
            switchButtonRed = new Switch("Red");
            elJesus = new Switch("Jesus Halo");
            audioPlayer = new AudioPlayer("Audio");

            popOut1Piano = new Effect.PopOut(S(0.4));
            popOut1Drums = new Effect.PopOut(S(0.4));
            popOut1DrumsFast = new Effect.PopOut(S(0.3));
            popOut1Chord = new Effect.PopOut(S(0.4));
            popOut1Solo = new Effect.PopOut(S(0.3));
            popOut1Solo2 = new Effect.PopOut(S(0.2));
            popOut1Choir = new Effect.PopOut(S(1.0));
            popOut1Voice = new Effect.PopOut(S(1.0));
            popOut1Vocal2 = new Effect.PopOut(S(2.0));
            popOut1VocalLong = new Effect.PopOut(S(5.0));
            popOut1End = new Effect.PopOut(S(5.0));

            this.oscServer = new Expander.OscServer(10000);

            raspberry.DigitalInputs[4].Connect(buttonRed);
            raspberry.DigitalInputs[5].Connect(buttonBlue);
            raspberry.DigitalOutputs[0].Connect(switchButtonBlue);
            raspberry.DigitalOutputs[1].Connect(switchButtonRed);
            raspberry.DigitalOutputs[2].Connect(elJesus);

            raspberry.Connect(audioPlayer);
        }

        private void ConfigureMusic1()
        {
            popOut1Piano = new Effect.PopOut(S(0.4));
            popOut1Drums = new Effect.PopOut(S(0.4));
            popOut1DrumsFast = new Effect.PopOut(S(0.3));
            popOut1Chord = new Effect.PopOut(S(0.4));
            popOut1Solo = new Effect.PopOut(S(0.3));
            popOut1Solo2 = new Effect.PopOut(S(0.2));
            popOut1Choir = new Effect.PopOut(S(1.0));
            popOut1Voice = new Effect.PopOut(S(1.0));
            popOut1Vocal2 = new Effect.PopOut(S(2.0));
            popOut1VocalLong = new Effect.PopOut(S(5.0));
            popOut1End = new Effect.PopOut(S(5.0));

            popOut1Piano
                .AddDevice(lightString1)
                .AddDevice(lightString2)
                .AddDevice(lightTreeUp)
                .AddDevice(lightStar);

            popOut1Drums
                .AddDevice(lightDeerLarge);

            popOut1DrumsFast
                .AddDevice(lightDeerSmall)
                .AddDevice(lightGarland1)
                .AddDevice(lightGarland2)
                .AddDevice(lightGarland3)
                .AddDevice(lightGarland4)
                .AddDevice(allPixels);

            popOut1Chord
                .AddDevice(lightTopperSmall)
                .AddDevice(lightTopperLarge);
            //.AddDevice(lightTree)
            //.AddDevice(lightCeiling2)
            //.AddDevice(lightCeiling3);

            popOut1Solo
                .AddDevice(lightNet1)
                .AddDevice(lightNet2)
                //.AddDevice(lightCeiling1)
                //.AddDevice(lightCeiling3)
                .SetPriority(2);

            popOut1Solo2
                .AddDevice(lightTreeUp)
                //.AddDevice(lightCeiling1)
                //.AddDevice(lightCeiling3)
                .SetPriority(2);

            popOut1Choir
                .AddDevice(lightGarland1)
                .AddDevice(lightSnow1)
                .AddDevice(lightTopperLarge)
                .AddDevice(lightTopperSmall);
            //.AddDevice(lightCeiling1);

            popOut1Voice
                .AddDevice(lightGarland1)
                .AddDevice(lightGarland2)
                .AddDevice(lightSnow2);
            //.AddDevice(lightCeiling3);

            popOut1Vocal2
                //.AddDevice(lightReindeers)
                .AddDevice(allPixels)
                .SetPriority(10);

            popOut1VocalLong
                .AddDevice(lightSnow1)
                .AddDevice(lightSnow2)
                .AddDevice(lightGarland1)
                .AddDevice(lightGarland2)
                .AddDevice(lightGarland3)
                .AddDevice(lightGarland4)
                //.AddDevice(lightNetRight)
                //.AddDevice(lightGarlandRight)
                //.AddDevice(lightHatsRight)
                .SetPriority(10);

            popOut1End
                .AddDevice(lightStar)
                .AddDevice(lightHat1)
                .AddDevice(lightHat2)
                .AddDevice(lightHat3)
                .AddDevice(lightHat4)
                .AddDevice(lightSnow1)
                .AddDevice(lightSnow2)
                .AddDevice(lightStairs1)
                .AddDevice(lightStairs2)
                .AddDevice(lightGarland1)
                .AddDevice(lightGarland2)
                .AddDevice(lightGarland3)
                .AddDevice(lightGarland4)
                .AddDevice(lightString1)
                .AddDevice(lightString2)
                .AddDevice(lightXmasTree)
                .AddDevice(lightDeerLarge)
                .AddDevice(lightDeerSmall)
                .AddDevice(lightTreeUp)
                .AddDevice(lightTopperSmall)
                .AddDevice(lightTopperLarge)
                .AddDevice(lightNet1)
                .AddDevice(lightNet2)
                .AddDevice(allPixels)
                .SetPriority(100);

            timeline1.AddMs(0, "INIT");
            timeline1.PopulateFromCSV("Christmas Canon Rock All Labels.csv");
            int state = 0;
            int halfSolo = 0;

            timeline1.TimelineTrigger += (sender, e) =>
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
                        hatLightState.NextState();
                        popOut1Piano.Pop(0.4);
                        if (state == 0)
                            allPixels.Inject(Color.Red, 0.5);
                        break;

                    case "N2":
                        hatLightState.NextState();
                        popOut1Piano.Pop(0.6);
                        if (state == 0)
                            allPixels.Inject(Color.White, 0.5);
                        break;

                    case "N3":
                        hatLightState.NextState();
                        popOut1Piano.Pop(0.8);
                        if (state == 0)
                            allPixels.Inject(Color.Blue, 0.5);
                        break;

                    case "N4":
                        hatLightState.NextState();
                        popOut1Piano.Pop(1.0);
                        if (state == 0)
                            allPixels.Inject(Color.Black, 0.0);
                        break;

                    case "Base":
                        popOut1Drums.Pop(1.0);
                        if (state < 3)
                        {
                            allPixels.SetAllOnlyColor(Color.Purple);
                            popOut1DrumsFast.Pop(1.0);
                        }
                        break;

                    case "Cymbal":
                        popOut1Drums.Pop(1.0);
                        if (state < 3)
                        {
                            allPixels.SetAllOnlyColor(Color.Green);
                            popOut1DrumsFast.Pop(1.0);
                        }
                        break;

                    case "Chord":
                        popOut1Chord.Pop(1.0);
                        break;

                    case "Solo":
                        popOut1Solo.Pop(1.0);
                        if ((halfSolo++ % 2) == 0)
                            popOut1Solo2.Pop(0.8);
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
                        popOut1Choir.Pop(1.0);
                        break;

                    case "Voice":
                        popOut1Voice.Pop(1.0);
                        break;

                    case "Vocal2":
                        popOut1Vocal2.Pop(1.0);
                        break;

                    case "Long":
                        popOut1VocalLong.Pop(1.0);
                        break;

                    case "LongUp":
                        // TODO
                        break;

                    case "Down":
                        // TODO
                        break;

                    case "End":
                        AllLightsOff();
                        popOut1End.Pop(1.0);
                        break;

                    default:
                        this.log.Information("Unhandled code: " + e.Code);
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

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
            port.Connect(new Physical.PixelRope(allPixels, 0, 100), 4, 1);
            port.Connect(new Physical.PixelRope(saberPixels, 0, 60), 1, 1);
            port.Connect(new Physical.PixelRope(starwarsPixels, 0, 50), 4, 1);
            // GECE
            port.Connect(new Physical.PixelRope(allPixels, 0, 50), 2, 91);
            port.Connect(new Physical.PixelRope(starwarsPixels, 0, 50), 2, 91);

            port.Connect(new Physical.GenericDimmer(lightStar, 1), 21);
            port.Connect(new Physical.GenericDimmer(lightHat1, 2), 21);
            port.Connect(new Physical.GenericDimmer(lightHat2, 3), 21);
            port.Connect(new Physical.GenericDimmer(lightHat3, 4), 21);
            port.Connect(new Physical.GenericDimmer(lightHat4, 5), 21);
            port.Connect(new Physical.GenericDimmer(lightSnow1, 6), 21);
            port.Connect(new Physical.GenericDimmer(lightSnow2, 7), 21);
            port.Connect(new Physical.GenericDimmer(lightTopperSmall, 8), 21);
            port.Connect(new Physical.GenericDimmer(lightTopperLarge, 9), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland1, 10), 21);
            port.Connect(new Physical.GenericDimmer(lightString2, 11), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland4, 12), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland3, 13), 21);
            port.Connect(new Physical.GenericDimmer(lightXmasTree, 14), 21);
            port.Connect(new Physical.GenericDimmer(lightStairs2, 15), 21);
            port.Connect(new Physical.GenericDimmer(lightNet1, 17), 21);
            port.Connect(new Physical.GenericDimmer(lightString1, 19), 21);
            port.Connect(new Physical.GenericDimmer(lightNet2, 20), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland2, 22), 21);
            port.Connect(new Physical.GenericDimmer(lightStairs1, 24), 21);

            port.Connect(new Physical.GenericDimmer(switchDeerHuge, 3), 20);
            port.Connect(new Physical.GenericDimmer(switchSanta, 4), 20);
            port.Connect(new Physical.RGBStrobe(lightTreeUp, 20), 20);
            port.Connect(new Physical.RGBStrobe(lightVader, 40), 20);
            port.Connect(new Physical.RGBStrobe(light3wise, 30), 20);
            port.Connect(new Physical.GenericDimmer(lightDeerLarge, 100), 20);
            port.Connect(new Physical.GenericDimmer(lightDeerSmall, 101), 20);
            port.Connect(new Physical.SmallRGBStrobe(lightJesus, 48), 20);
        }

        private void TestAllPixels(Color color, double brightness, TimeSpan delay)
        {
            allPixels.SetAll(color, brightness);
            System.Threading.Thread.Sleep(delay);
        }

        private void EverythingOff()
        {
            Exec.Cancel(starwarsCane);
            audioPlayer.PauseTrack();
            audioPlayer.PauseFX();
            pulsatingEffect1.Stop();
            pulsatingStar.Stop();
            faderIn.Stop();
            flickerEffect.Stop();

            AllLightsOff();
        }

        private void AllLightsOff()
        {
            lightGarland1.TurnOff();
            lightGarland2.TurnOff();
            lightGarland3.TurnOff();
            lightGarland4.TurnOff();
            lightStairs1.TurnOff();
            lightStairs2.TurnOff();
            lightXmasTree.TurnOff();
            lightDeerSmall.TurnOff();
            lightDeerLarge.TurnOff();
            lightTopperLarge.TurnOff();
            lightTopperSmall.TurnOff();
            lightNet1.TurnOff();
            lightNet2.TurnOff();
            lightString1.TurnOff();
            lightString2.TurnOff();
            lightSnow1.TurnOff();
            lightSnow2.TurnOff();
            lightTreeUp.TurnOff();
            lightStar.TurnOff();
            light3wise.TurnOff();
            lightVader.TurnOff();
            elJesus.TurnOff();
            lightJesus.TurnOff();
            allPixels.TurnOff();
            starwarsPixels.TurnOff();
            saberPixels.TurnOff();
        }

        public override void Start()
        {
            hours.AddRange("5:00 pm", "11:00 pm");

            bool.TryParse(Exec.GetKey("InflatablesRunning", false.ToString()), out inflatablesRunning);

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
                    switchButtonBlue.SetPower(true);
                    switchButtonRed.SetPower(true);
                    lightTreeUp.SetOnlyColor(Color.Red);

                    faderIn.Restart();

                    Executor.Current.Execute(twinkleSeq);
                })
                .TearDown(() =>
                {
                    Executor.Current.Cancel(twinkleSeq);

                    switchButtonBlue.SetPower(false);
                    switchButtonRed.SetPower(false);
                    EverythingOff();
                });

            offHours1Seq
                .WhenExecuted
                .Execute(instance =>
                {
                    audioPlayer.PlayEffect("force1");
                    instance.WaitFor(S(4));
                });

            offHours2Seq
                .WhenExecuted
                .Execute(instance =>
                {
                    audioPlayer.PlayEffect("darkside");
                    instance.WaitFor(S(4));
                });

            music1Seq
                .WhenExecuted
                .SetUp(() =>
                {
                    audioPlayer.CueTrack("21. Christmas Canon Rock");
                    // Make sure it's ready
                    System.Threading.Thread.Sleep(800);

                    EverythingOff();
                })
                .Execute(instance =>
                {
                    audioPlayer.ResumeTrack();
                    var task = timeline1.Start();

                    try
                    {
                        task.Wait(instance.CancelToken);

                        instance.WaitFor(S(10));
                    }
                    finally
                    {
                        timeline1.Stop();
                        audioPlayer.PauseTrack();
                    }

                    if (!instance.IsCancellationRequested)
                        instance.WaitFor(S(2));
                    EverythingOff();

                    instance.WaitFor(S(2));
                })
                .TearDown(() =>
                {
                    EverythingOff();
                });

            starwarsCane
                .WhenExecuted
                .SetUp(() =>
                {
                    allPixels.TurnOff();
                    starwarsPixels.TurnOff();
                })
                .Execute(instance =>
                {
                    const int spacing = 4;

                    while (!instance.CancelToken.IsCancellationRequested)
                    {
                        for (int i = 0; i < spacing; i++)
                        {
                            switch (i % spacing)
                            {
                                case 0:
                                case 1:
                                    starwarsPixels.InjectRev(Color.Yellow, 1.0);
                                    break;
                                case 2:
                                case 3:
                                    starwarsPixels.InjectRev(Color.Orange, 0.2);
                                    break;
                            }

                            instance.WaitFor(S(0.1));

                            if (instance.IsCancellationRequested)
                                break;
                        }
                    }
                })
                .TearDown(() => starwarsPixels.TurnOff());

            fatherSeq
                .WhenExecuted
                .Execute(instance =>
                {
                    EverythingOff();

                    Executor.Current.Execute(starwarsCane);

                    audioPlayer.PlayTrack("01. Star Wars - Main Title");

                    //lightCeiling1.SetOnlyColor(Color.Yellow);
                    //lightCeiling2.SetOnlyColor(Color.Yellow);
                    //lightCeiling3.SetOnlyColor(Color.Yellow);
                    //pulsatingEffect2.Start();
                    instance.WaitFor(S(16));
                    //pulsatingEffect2.Stop();
                    audioPlayer.PauseTrack();
                    Executor.Current.Cancel(starwarsCane);
                    allPixels.TurnOff();
                    instance.WaitFor(S(0.5));

                    elJesus.SetPower(true);
                    pulsatingStar.Start();
                    lightJesus.SetColor(Color.White, 0.3);
                    light3wise.SetOnlyColor(Color.LightYellow);
                    light3wise.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));
                    lightVader.SetOnlyColor(Color.LightYellow);
                    lightVader.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));

                    instance.WaitFor(S(2.5));

                    //elLightsaber.SetPower(true);
                    audioPlayer.PlayEffect("saberon");
                    for (int sab = 0; sab < 60; sab++)
                    {
                        saberPixels.Inject(Color.Red, 0.5);
                        instance.WaitFor(S(0.01));
                    }

//                    lightVader.SetColor(Color.Red, 1.0);
                    audioPlayer.PlayEffect("father");
                    instance.WaitFor(S(4));

                    lightVader.TurnOff();
                    light3wise.TurnOff();
                    lightJesus.TurnOff();
                    pulsatingStar.Stop();
                    elJesus.TurnOff();

                    audioPlayer.PlayEffect("force1");
                    instance.WaitFor(S(4));

                    lightVader.TurnOff();
                    audioPlayer.PlayEffect("saberoff");
                    instance.WaitFor(S(0.7));
                    for (int sab = 0; sab < 30; sab++)
                    {
                        saberPixels.InjectRev(Color.Black, 0);
                        saberPixels.InjectRev(Color.Black, 0);
                        instance.WaitFor(S(0.01));
                    }
                    //elLightsaber.SetPower(false);
                    instance.WaitFor(S(2));

                    //lightJesus.TurnOff();
                    //light3wise.TurnOff();
                    //elLightsaber.TurnOff();
                    //pulsatingStar.Stop();
                    //elJesus.TurnOff();
                    //instance.WaitFor(S(2));
                })
                .TearDown(() => {
                    EverythingOff();
                });


            waveformSeq
                .WhenExecuted
                .SetUp(() =>
                {
                    audioPlayer.CueTrack("05. Frozen - Let It Go");
                    // Make sure it's ready
                    System.Threading.Thread.Sleep(800);

                    EverythingOff();
                })
                .Execute(i =>
                {
                    var timer = new Controller.HighPrecisionTimer3(50, false);
                    byte[] buffer;
                    using (var fs = System.IO.File.OpenRead("Let It Go - Waveform 50ms.dat"))
                    {
                        buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        fs.Close();
                    }

                    var cb = new ColorBrightness[allPixels.Pixels];
                    for(int cb_pos = 0; cb_pos < cb.Length; cb_pos++)
                        cb[cb_pos] = new ColorBrightness(Color.Turquoise, 0.0);

                    double lastValue = 0;

                    timer.Subscribe( += (o, e) =>
                        {
                            int pos = (int)e.TotalTicks;
                            if(pos >= buffer.Length)
                            {
                                e.Cancel = true;
                                return;
                            }
                            
                            double curValue = buffer[pos].GetDouble();
                            if (curValue > lastValue)
                                lastValue = curValue;
                            double value = lastValue;
                            lastValue = (lastValue - 0.02).Limit(0.0, 1.0);

                            const double d1 = 0.00;
                            const double d2 = 0.15;
                            const double d3 = 0.20;
                            const double d4 = 0.25;
                            lightHat1.Brightness = value.LimitAndScale(d1, 0.2);
                            lightHat2.Brightness = value.LimitAndScale(d1, 0.2);
                            lightHat3.Brightness = value.LimitAndScale(d2, 0.2);
                            lightHat4.Brightness = value.LimitAndScale(d2, 0.2);
                            lightTreeUp.Brightness = value.LimitAndScale(d3, 0.2);
                            lightGarland1.Brightness = value.LimitAndScale(d3, 0.2);
                            lightGarland2.Brightness = value.LimitAndScale(d3, 0.2);
                            lightGarland3.Brightness = value.LimitAndScale(d3, 0.2);
                            lightGarland4.Brightness = value.LimitAndScale(d3, 0.2);
                            lightNet1.Brightness = value.LimitAndScale(d2, 0.2);
                            lightNet2.Brightness = value.LimitAndScale(d2, 0.2);
                            lightSnow1.Brightness = value.LimitAndScale(d3, 0.2);
                            lightSnow2.Brightness = value.LimitAndScale(d3, 0.2);
                            lightStairs1.Brightness = value.LimitAndScale(d1, 0.2);
                            lightStairs2.Brightness = value.LimitAndScale(d1, 0.2);
                            lightDeerSmall.Brightness = curValue;
                            lightTopperLarge.Brightness = curValue;
                            lightDeerLarge.Brightness = curValue;
                            lightTopperSmall.Brightness = curValue;
                            lightString1.Brightness = value.LimitAndScale(d4, 0.2);
                            lightString2.Brightness = value.LimitAndScale(d4, 0.2);
                            lightXmasTree.Brightness = value.LimitAndScale(d4, 0.2);
                            lightStar.Brightness = value.LimitAndScale(d4, 0.2);

                            int vuPos = (int)(cb.Length * value);
                            for (int cb_pos = 0; cb_pos < cb.Length; cb_pos++)
                                cb[cb_pos].Brightness = cb_pos < vuPos ? 1.0 : 0.2;

                            allPixels.SetColors(0, cb);
                        };

                    timer.Start();
                    audioPlayer.ResumeTrack();

                    timer.WaitUntilFinished(i);
                })
                .TearDown(() =>
                {
                    EverythingOff();
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

                this.oscServer.RegisterAction<int>("/osc/button1", (msg, data) =>
                {
                    if (data.Any() && data.First() == 1)
                    {
                        stateMachine.SetState(States.Vader);
                    }
                });

                this.oscServer.RegisterAction<int>("/osc/button2", (msg, data) =>
                {
                    if (data.Any() && data.First() == 1)
                    {
                        stateMachine.SetState(States.Music1);
                    }
                });

                this.oscServer.RegisterAction<int>("/osc/button3", (msg, data) =>
                {
                    if (data.Any() && data.First() == 1)
                    {
                        stateMachine.SetState(States.Music2);
                    }
                });

                this.oscServer.RegisterAction<int>("/osc/button4", (msg, data) =>
                {
                    if (data.Any() && data.First() == 1)
                    {
                        stateMachine.SetState(States.Background);
                    }
                });

                this.oscServer.RegisterAction<int>("/osc/button5", (msg, data) =>
                {
                    if (data.Any() && data.First() == 1)
                    {
                        audioPlayer.PlayEffect("darkside");
                    }
                });

                this.oscServer.RegisterAction<int>("/osc/button6", (msg, data) =>
                {
                    if (data.Any() && data.First() == 1)
                    {
                        audioPlayer.PlayEffect("Darth Breathing");
                    }
                });

                // Test Button
            buttonTest.ActiveChanged += (sender, e) =>
            {
                //                lightGarland4.Brightness = e.NewState ? 1.0 : 0.0;

                if (e.NewState)
                    Exec.Execute(waveformSeq);
                else
                    Exec.Cancel(waveformSeq);

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
                    if (!e.NewState)
                        return;

                    if (mute.HasValue && (DateTime.Now - mute.Value).TotalSeconds < 30)
                        return;

                    if (hours.IsOpen)
                    {
                        if (stateMachine.CurrentState == States.Music1 ||
                            stateMachine.CurrentState == States.Music2)
                        {
                            // Stop
                            //stateMachine.SetState(States.Background);
                            return;
                        }

                        switch (this.whichMusic++ % 2)
                        {
                            case 0:
                                stateMachine.SetState(States.Music1);
                                break;

                            case 1:
                                stateMachine.SetState(States.Music2);
                                break;
                        }
                    }
                    else
                        Exec.Execute(offHours1Seq);

                    mute = DateTime.Now;
                };

            buttonRed.ActiveChanged += (o, e) =>
            {
                if (!e.NewState)
                    return;

                if (mute.HasValue && (DateTime.Now - mute.Value).TotalSeconds < 30)
                    return;

                if (hours.IsOpen)
                {
                    if (stateMachine.CurrentState == States.Vader)
                        return;
                        //stateMachine.SetState(States.Background);
                    else
                        stateMachine.SetState(States.Vader);
                }
                else
                    Exec.Execute(offHours2Seq);

                mute = DateTime.Now;
            };

            audioPlayer.AudioTrackDone += (o, e) =>
            {
                //                switchButtonBlue.SetPower(false);
            };

            pulsatingEffect1
                .AddDevice(lightStar);

            pulsatingStar
                .AddDevice(lightStar);

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
                    System.Threading.Thread.Sleep(200);

                    switchDeerHuge.TurnOff();
                    switchSanta.TurnOff();
                    InflatablesRunning = false;
                }
            };

            faderIn
                .AddDevice(lightGarland1)
                .AddDevice(lightGarland2)
                .AddDevice(lightGarland3)
                .AddDevice(lightGarland4)
                .AddDevice(lightStairs1)
                .AddDevice(lightStairs2)
                .AddDevice(lightXmasTree)
                .AddDevice(lightDeerSmall)
                .AddDevice(lightDeerLarge)
                .AddDevice(lightTopperLarge)
                .AddDevice(lightTopperSmall)
                .AddDevice(lightNet1)
                .AddDevice(lightNet2)
                .AddDevice(lightString1)
                .AddDevice(lightString2)
                .AddDevice(lightSnow1)
                .AddDevice(lightSnow2)
                .AddDevice(lightTreeUp);

            stateMachine.ForFromSequence(States.Background, backgroundLoop);
            stateMachine.ForFromSequence(States.Music1, music1Seq);
            stateMachine.ForFromSequence(States.Music2, waveformSeq);
            stateMachine.ForFromSequence(States.Vader, fatherSeq);

            hatLightState.For(0).Execute(i => lightHat1.RunEffect(new Effect2.Fader(1.0, 0.0), S(0.5)));
            hatLightState.For(1).Execute(i => lightHat2.RunEffect(new Effect2.Fader(1.0, 0.0), S(0.5)));
            hatLightState.For(2).Execute(i => lightHat3.RunEffect(new Effect2.Fader(1.0, 0.0), S(0.5)));
            hatLightState.For(3).Execute(i => lightHat4.RunEffect(new Effect2.Fader(1.0, 0.0), S(0.5)));
            hatLightState.For(4).Execute(i => lightHat3.RunEffect(new Effect2.Fader(1.0, 0.0), S(0.5)));
            hatLightState.For(5).Execute(i => lightHat2.RunEffect(new Effect2.Fader(1.0, 0.0), S(0.5)));

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

            ConfigureMusic1();
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
