using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;
using System.Threading.Tasks;

namespace Animatroller.Scenes
{
    internal partial class Halloween2017 : BaseScene
    {
        const int SacnUniverseDMXFogA = 3;
        const int SacnUniverseEdmx4A = 20;
        const int SacnUniverseEdmx4B = 21;
        const int SacnUniverseDMXCat = 4;
        const int SacnUniverseDMXLedmx = 10;
        const int SacnUniversePixel100 = 5;
        const int SacnUniversePixel50 = 6;
        const int SacnUniverseFrankGhost = 7;
        const int SacnUniverseFire = 99;

        public enum States
        {
            BackgroundSmall,
            BackgroundFull,
            EmergencyStop,
            Special1
        }

        const int midiChannel = 0;

        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();
        Expander.MidiInput2 midiInput = new Expander.MidiInput2("LPD8", ignoreMissingDevice: true);
        Expander.OscServer oscServer = new Expander.OscServer(8000, 9000, registerAutoHandlers: true);
        AudioPlayer audioPumpkin = new AudioPlayer();
        AudioPlayer audioFrankGhost = new AudioPlayer();
        AudioPlayer audioSpider = new AudioPlayer();
        AudioPlayer audioRocking = new AudioPlayer();
        AudioPlayer audioLocal = new AudioPlayer();
        AudioPlayer audioCat = new AudioPlayer();
        AudioPlayer audioHifi = new AudioPlayer();
        AudioPlayer audio2 = new AudioPlayer();
        AudioPlayer audioPop = new AudioPlayer();
        AudioPlayer audioDIN = new AudioPlayer();
        AudioPlayer audioFlying = new AudioPlayer();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8899);
        Expander.MonoExpanderInstance expanderLedmx = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderAudio2 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderHifi = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderPicture = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderGhost = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderCat = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderMrPumpkin = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderSpider = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderRocking = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance();
        Expander.AcnStream acnOutput = new Expander.AcnStream();

        VirtualPixel1D3 pixelsRoofEdge = new VirtualPixel1D3(150);
        VirtualPixel1D3 pixelsFrankGhost = new VirtualPixel1D3(5);
        AnalogInput3 faderR = new AnalogInput3(persistState: true);
        AnalogInput3 faderG = new AnalogInput3(persistState: true);
        AnalogInput3 faderB = new AnalogInput3(persistState: true);
        AnalogInput3 faderBright = new AnalogInput3(persistState: true);
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 manualFader = new DigitalInput2(persistState: true);
        AnalogInput3 masterVolume = new AnalogInput3(persistState: true, defaultValue: 1.0);
        DigitalInput2 flashBaby = new DigitalInput2();

        AnalogInput3 inputBrightness = new AnalogInput3(true, name: "Brightness");
        AnalogInput3 inputH = new AnalogInput3(true, name: "Hue");
        AnalogInput3 inputS = new AnalogInput3(true, name: "Saturation");

        Controller.Subroutine subFinal = new Controller.Subroutine();
        Controller.Subroutine subFirst = new Controller.Subroutine();
        Controller.Subroutine sub3dfxRandom = new Controller.Subroutine();
        Controller.Subroutine sub3dfxLady = new Controller.Subroutine();
        Controller.Subroutine sub3dfxMan = new Controller.Subroutine();
        Controller.Subroutine sub3dfxKids = new Controller.Subroutine();
        Controller.Subroutine subSpiderJump = new Controller.Subroutine();
        Controller.Subroutine subPicture = new Controller.Subroutine();
        Controller.Subroutine subGhost = new Controller.Subroutine();
        Controller.Subroutine subLast = new Controller.Subroutine();
        Controller.Subroutine subFog = new Controller.Subroutine();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 emergencyStop = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockMaster = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockCat = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockFire = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockFirst = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockPicture = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockGhost = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockLast = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockPumpkin = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockFrankGhost = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockSpiderDrop = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 floodLights = new DigitalInput2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 testButton1 = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 testButton2 = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 testButton3 = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 testButton4 = new DigitalInput2();

        Effect.Flicker flickerEffect = new Effect.Flicker(0.4, 0.6, false);
        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingGargoyle = new Effect.Pulsating(S(4), 0.5, 1.0, false);
        Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.4, 1.0, false);
        Effect.PopOut2 popOut1 = new Effect.PopOut2(S(0.3));
        Effect.PopOut2 popOut2 = new Effect.PopOut2(S(0.3));
        Effect.PopOut2 popOutAll = new Effect.PopOut2(S(1.2));

        DigitalInput2 frankGhostMotion = new DigitalInput2();
        DigitalInput2 mrPumpkinMotion = new DigitalInput2();
        DigitalInput2 rockingMotion = new DigitalInput2();
        DigitalInput2 catMotion = new DigitalInput2();
        DigitalInput2 spiderDropTrigger = new DigitalInput2();
        DigitalInput2 firstBeam = new DigitalInput2();
        DigitalInput2 secondBeam = new DigitalInput2();
        DigitalInput2 ghostBeam = new DigitalInput2();
        DigitalInput2 lastBeam = new DigitalInput2();
        DigitalOutput2 catAir = new DigitalOutput2();
        DigitalOutput2 fire = new DigitalOutput2();
        DigitalOutput2 mrPumpkinAir = new DigitalOutput2();
        DigitalOutput2 frankGhostAir = new DigitalOutput2();
        DigitalOutput2 fog = new DigitalOutput2();
        DigitalOutput2 popper = new DigitalOutput2();
        DigitalOutput2 spiderDropRelease = new DigitalOutput2();
        DigitalOutput2 spiderVenom = new DigitalOutput2();
        DigitalOutput2 spiderJump2 = new DigitalOutput2();
        DigitalOutput2 ladyMovingEyes = new DigitalOutput2();
        DigitalOutput2 rockingChair = new DigitalOutput2();
        DateTime? lastFogRun = DateTime.Now;
        ThroughputDevice fogStairsPump1 = new ThroughputDevice();
        ThroughputDevice fogStairsPump2 = new ThroughputDevice();
        CommandDevice pictureFrame1 = new CommandDevice();
        Dimmer3 catLights = new Dimmer3();
        Dimmer3 pumpkinLights = new Dimmer3();
        Dimmer3 spiderWebLights = new Dimmer3();
        Dimmer3 spiderEyes = new Dimmer3();
        Dimmer3 streetNumberEyes = new Dimmer3();
        Dimmer3 bigSpiderEyes = new Dimmer3();
        Dimmer3 gargoyleLightsCrystal = new Dimmer3();
        Dimmer3 gargoyleLightsEyes = new Dimmer3();
        Dimmer3 flyingSkeletonEyes = new Dimmer3();
        Dimmer3 hazerFanSpeed = new Dimmer3();
        Dimmer3 hazerHazeOutput = new Dimmer3();
        Dimmer3 stairs1Light = new Dimmer3("Stairs 1");
        Dimmer3 stairs2Light = new Dimmer3("Stairs 2");
        Dimmer3 treeGhosts = new Dimmer3();
        Dimmer3 treeSkulls = new Dimmer3();
        Dimmer3 popperEyes = new Dimmer3();

        OperatingHours2 hoursSmall = new OperatingHours2("Hours Small");
        OperatingHours2 hoursFull = new OperatingHours2("Hours Full");

        GroupDimmer allLights = new GroupDimmer();
        GroupDimmer purpleLights = new GroupDimmer();

        StrobeColorDimmer3 fogStairsLight1 = new StrobeColorDimmer3();
        StrobeColorDimmer3 fogStairsLight2 = new StrobeColorDimmer3();
        StrobeColorDimmer3 spiderLight = new StrobeColorDimmer3("Spider");
        StrobeColorDimmer3 wall1Light = new StrobeColorDimmer3("Wall 1 (cat)");
        StrobeColorDimmer3 wall2Light = new StrobeColorDimmer3("Wall 2 (flag)");
        StrobeColorDimmer3 wall3Light = new StrobeColorDimmer3("Wall 3");
        StrobeColorDimmer3 wall4Light = new StrobeColorDimmer3("Wall 4");
        StrobeColorDimmer3 wall5Light = new StrobeColorDimmer3("Wall 5");
        StrobeColorDimmer3 wall6Light = new StrobeColorDimmer3("Wall 6");
        StrobeColorDimmer3 rockingChairLight = new StrobeColorDimmer3("Rocking chair");
        //StrobeColorDimmer3 wall8Light = new StrobeColorDimmer3("Wall 8");
        //StrobeColorDimmer3 wall9Light = new StrobeColorDimmer3("Wall 9");
        StrobeDimmer3 flashTree = new StrobeDimmer3("ADJ Flash");
        StrobeDimmer3 flashUnderSpider = new StrobeDimmer3("Eliminator Flash");
        //        StrobeColorDimmer3 pinSpot = new StrobeColorDimmer3("Pin Spot");

        Controller.Sequence welcomeSeq = new Controller.Sequence();
        Controller.Sequence motionSeq = new Controller.Sequence();

        Controller.Timeline<string> timelineThunder1 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder2 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder3 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder4 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder5 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder6 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder7 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder8 = new Controller.Timeline<string>(1);

        IControlToken manualFaderToken;
        IControlToken bigSpiderEyesToken;
        int soundBoardOutputIndex = 0;
        byte last3dfxVideo = 0;

        // Modules
        Modules.HalloweenGrumpyCat grumpyCat;
        Modules.HalloweenMrPumpkin mrPumpkin;
        Modules.HalloweenFrankGhost frankGhost;
        Modules.HalloweenSpiderDrop spiderDrop;
        Modules.FireProjector fireProjector;
        Modules.HalloweenPictureFrame pictureFrame;

        public Halloween2017(IEnumerable<string> args)
        {
            hoursSmall.AddRange("5:00 pm", "8:30 pm",
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);
            hoursSmall.AddRange("5:00 pm", "9:00 pm",
                DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday);

            hoursFull.AddRange("6:00 pm", "8:30 pm");
            hoursFull.Disabled = true;
            //hoursSmall.Disabled = true;

            // Logging
            //hoursSmall.Output.Log("Hours small");
            //hoursFull.Output.Log("Hours full");

            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                {
                    Exec.ExpanderSharedFiles = parts[1];
                }
            }

            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expanderLedmx);      // rpi-eb0092ca
            expanderServer.AddInstance("1583f686014345888c15d7fc9c55ca3c", expanderCat);        // rpi-eb81c94e
            expanderServer.AddInstance("4ea781ef257442edb524493da8f52220", expanderAudio2);     // rpi-eba6cbc7
            expanderServer.AddInstance("d6fc4e752af04022bf3c1a1166a557bb", expanderHifi);       // rpi-eb428ef1
            expanderServer.AddInstance("60023fcde5b549b89fa828d31741dd0c", expanderPicture);    // rpi-eb91bc26
            expanderServer.AddInstance("e41d2977931d4887a9417e8adcd87306", expanderGhost);      // rpi-eb6a047c
            expanderServer.AddInstance("999861affa294fd7bbf0601505e9ae09", expanderMrPumpkin);  // rpi-ebd43a38
            expanderServer.AddInstance("992f8db68e874248b5ee667d23d74ac3", expanderSpider);     // rpi-eb9b3145
            expanderServer.AddInstance("db9b41a596cb4ed28e91f11a59afb95a", expanderRocking);    // rpi-eb32e5f9

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);      // HL-DEV

            masterVolume.ConnectTo(Exec.MasterVolume);


            grumpyCat = new Modules.HalloweenGrumpyCat(
                air: catAir,
                light: catLights,
                audioPlayer: audioCat,
                name: nameof(grumpyCat));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(grumpyCat.InputPower);

            pictureFrame = new Modules.HalloweenPictureFrame(
                medeaWizPlayer: pictureFrame1,
                name: nameof(pictureFrame));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(pictureFrame.InputPower);

            mrPumpkin = new Modules.HalloweenMrPumpkin(
                air: mrPumpkinAir,
                light: pumpkinLights,
                audioPlayer: audioPumpkin,
                name: nameof(mrPumpkin));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(mrPumpkin.InputPower);

            frankGhost = new Modules.HalloweenFrankGhost(
                air: frankGhostAir,
                light: pixelsFrankGhost,
                audioPlayer: audioFrankGhost,
                name: nameof(frankGhost));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(frankGhost.InputPower);

            spiderDrop = new Modules.HalloweenSpiderDrop(
                eyesLight: spiderEyes,
                drop: spiderDropRelease,
                venom: spiderVenom,
                strobeLight: flashUnderSpider,
                audioPlayer: audioSpider,
                name: nameof(spiderDrop));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(spiderDrop.InputPower);

            fireProjector = new Modules.FireProjector(
                fire: fire,
                name: nameof(fireProjector));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall, null).Controls(fireProjector.InputPower);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    hoursSmall.SetForced(true);
                else
                    hoursSmall.SetForced(null);
            });


            flashBaby.Output.Subscribe(x =>
            {
                // Flash
                if (x)
                {
                    allLights.TakeAndHoldControl(100, "FlashBaby");
                    allLights.SetData(null, Utils.Data(1.0), Utils.Data(Color.White));
                }
                else
                    allLights.ReleaseControl();
            });

            testButton1.Output.Subscribe(x =>
            {
                rockingChair.SetValue(x);
                if (x)
                    audioRocking.PlayBackground();
                else
                    audioRocking.PauseBackground();
            });
            testButton2.Output.Subscribe(x =>
            {
                ladyMovingEyes.SetValue(x);
                if (x)
                    audioRocking.PlayEffect("Maniacal Witches Laugh-SoundBible.com-262127569.wav");
            });
            testButton3.Output.Subscribe(x =>
            {
                if (x)
                    audioHifi.PlayBackground();
                else
                    audioHifi.PauseBackground();
            });

            testButton4.Output.Subscribe(x =>
            {
                //rockingChairLight.SetColor(Color.Yellow, x ? 1 : 0);
                //flashUnderSpider.SetBrightnessStrobeSpeed(x ? 1 : 0, 1.0);
                flashTree.SetBrightness(x);
            });

            floodLights.Output.Subscribe(x =>
            {
                // Flash
                if (x)
                {
                    allLights.TakeAndHoldControl(200, "FlashBaby");
                    allLights.SetData(null, Utils.Data(Color.White, 1.0));
                }
                else
                    allLights.ReleaseControl();
            });

            emergencyStop.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.GoToState(States.EmergencyStop);
                }
                else
                {
                    if (hoursFull.IsOpen || hoursSmall.IsOpen)
                        stateMachine.GoToDefaultState();
                    else
                        stateMachine.GoToIdle();
                }
            });

            hoursFull.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetDefaultState(States.BackgroundFull);

                    if (emergencyStop.Value)
                        stateMachine.GoToState(States.EmergencyStop);
                    else
                        stateMachine.GoToDefaultState();
                }
                else
                {
                    stateMachine.GoToIdle();
                    stateMachine.SetDefaultState(null);
                }
                SetManualColor();
            });

            hoursSmall.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetDefaultState(States.BackgroundSmall);
                    stateMachine.GoToDefaultState();
                }
                else
                {
                    stateMachine.GoToIdle();
                    stateMachine.SetDefaultState(null);
                }
                SetManualColor();
            });

            popOut1.ConnectTo(wall1Light);
            popOut1.ConnectTo(wall4Light);
            popOut1.ConnectTo(wall6Light);
            popOut1.ConnectTo(flashTree);
            popOut2.ConnectTo(wall2Light);
            //popOut2.ConnectTo(wall7Light);
            //popOut2.ConnectTo(flashUnderSpider);
            popOutAll.ConnectTo(wall1Light);
            popOutAll.ConnectTo(wall2Light);
            popOutAll.ConnectTo(wall3Light);
            popOutAll.ConnectTo(wall4Light);
            popOutAll.ConnectTo(wall5Light);
            popOutAll.ConnectTo(wall6Light);
            //popOutAll.ConnectTo(wall7Light);
            //popOutAll.ConnectTo(wall8Light);
            //popOutAll.ConnectTo(wall9Light);
            popOutAll.ConnectTo(flashTree);
            //popOutAll.ConnectTo(flashUnderSpider);
            popOutAll.ConnectTo(pixelsRoofEdge);
            popOutAll.ConnectTo(pixelsFrankGhost);
            //            popOutAll.ConnectTo(pinSpot);

            allLights.Add(
                wall1Light,
                wall2Light,
                wall3Light,
                wall4Light,
                wall5Light,
                wall6Light,
                rockingChairLight,
                //wall7Light,
                //wall8Light,
                //wall9Light,
                flashTree,
                flashUnderSpider,
                pixelsRoofEdge,
                pixelsFrankGhost,
                //                pinSpot,
                spiderLight,
                fogStairsLight1,
                fogStairsLight2,
                spiderWebLights,
                pumpkinLights);

            purpleLights.Add(
                wall1Light,
                wall2Light,
                wall3Light,
                wall4Light,
                wall5Light,
                wall6Light,
                //wall7Light,
                //wall8Light,
                //wall9Light,
                pixelsRoofEdge);

            flickerEffect.ConnectTo(stairs1Light);
            flickerEffect.ConnectTo(stairs2Light);
            flickerEffect.ConnectTo(gargoyleLightsEyes);
            flickerEffect.ConnectTo(flyingSkeletonEyes);
            flickerEffect.ConnectTo(streetNumberEyes);
            flickerEffect.ConnectTo(bigSpiderEyes);
            pulsatingGargoyle.ConnectTo(gargoyleLightsCrystal);
            pulsatingGargoyle.ConnectTo(treeSkulls);
            //pulsatingGargoyle.ConnectTo(spiderEyes);
            //pulsatingEffect1.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(0, 255, 0)));
            //pulsatingEffect2.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(255, 0, 0)));

            pulsatingGargoyle.ConnectTo(spiderWebLights);

            stateMachine.For(States.BackgroundSmall)
                .Controls(1, flickerEffect, pulsatingGargoyle)
                .Execute(i =>
                    {
                        treeGhosts.SetBrightness(1.0);
                        treeSkulls.SetBrightness(1.0);
                        audio2.SetBackgroundVolume(0.5);

                        var purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                            0.16470588235294117);

                        purpleLights.SetData(null,
                            Utils.Data(purpleColor.Brightness),
                            Utils.Data(purpleColor.Color),
                            Utils.Data(DataElements.ColorUltraViolet, 1.0));

                        i.WaitUntilCancel();
                    })
                .TearDown(instance =>
                    {
                        purpleLights.SetBrightness(0.0);
                        treeGhosts.SetBrightness(0.0);
                        treeSkulls.SetBrightness(0.0);
                    });

            stateMachine.For(States.BackgroundFull)
                .Controls(1, flickerEffect, pulsatingGargoyle)
                .Execute(i =>
                {
                    treeGhosts.SetBrightness(1.0);
                    treeSkulls.SetBrightness(1.0);
                    audioHifi.PlayBackground();
                    audio2.SetBackgroundVolume(0.5);
                    audio2.PlayBackground();

                    var purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                        0.16470588235294117);

                    purpleLights.SetData(null, Utils.Data(purpleColor.Color, purpleColor.Brightness));

                    while (!i.IsCancellationRequested && stateMachine.CurrentState == States.BackgroundFull)
                    {
                        i.WaitFor(S(0.5));
                        if (!this.lastFogRun.HasValue || (DateTime.Now - this.lastFogRun.Value).TotalMinutes > 5)
                        {
                            // Run the fog for a little while
                            fog.SetValue(true);
                            i.WaitFor(S(4));
                            fog.SetValue(false);
                            this.lastFogRun = DateTime.Now;
                        }
                    }
                })
                .TearDown(instance =>
                {
                    purpleLights.SetBrightness(0.0);
                    treeGhosts.SetBrightness(0.0);
                    treeSkulls.SetBrightness(0.0);

                    audioHifi.PauseBackground();
                    audio2.PauseBackground();

                    timelineThunder1.Stop();
                    timelineThunder2.Stop();
                    timelineThunder3.Stop();
                    timelineThunder4.Stop();
                    timelineThunder5.Stop();
                    timelineThunder6.Stop();
                    timelineThunder7.Stop();
                    timelineThunder8.Stop();

                    flickerEffect.Stop();
                    treeGhosts.SetBrightness(0.0);
                    treeSkulls.SetBrightness(0.0);
                });

            stateMachine.For(States.EmergencyStop)
                .Execute(i =>
                {
                    // Do nothing
                    i.WaitUntilCancel();
                });

            stateMachine.For(States.Special1)
                .Execute(i =>
                {
                    audio2.PlayNewEffect("640 The Demon Exorcised.wav");

                    i.WaitUntilCancel();
                });

            inputBrightness.Output.Subscribe(x =>
            {
                //                testLight1.SetBrightness(x);
            });

            inputH.WhenOutputChanges(x =>
            {
                //                testLight1.SetColor(HSV.ColorFromHSV(x.GetByteScale(), inputS.Value, 1.0));
            });

            inputS.Output.Subscribe(x =>
            {
                //                testLight1.SetColor(HSV.ColorFromHSV(inputH.Value.GetByteScale(), x, 1.0));
            });


            expanderHifi.AudioTrackStart.Subscribe(x =>
            {
                // Next track
                switch (x.Item2)
                {
                    case "Thunder1.wav":
                        timelineThunder1.Start();
                        audio2.PlayEffect("scream.wav");
                        break;

                    case "Thunder2.wav":
                        timelineThunder2.Start();
                        break;

                    case "Thunder3.wav":
                        timelineThunder3.Start();
                        break;

                    case "Thunder4.wav":
                        timelineThunder4.Start();
                        audio2.PlayEffect("424 Coyote Howling.wav");
                        break;

                    case "Thunder5.wav":
                        timelineThunder5.Start();
                        //                        audioEeebox.PlayEffect("sixthsense-deadpeople.wav");
                        break;

                    case "Thunder6.wav":
                        timelineThunder6.Start();
                        break;

                    case "Thunder7.wav":
                        timelineThunder7.Start();
                        break;

                    case "Thunder8.wav":
                        timelineThunder8.Start();
                        break;

                    default:
                        log.Debug("Unknown track {0}", x);
                        break;
                }
            });

            timelineThunder1.AddMs(500, "A");
            timelineThunder1.AddMs(3500, "B");
            timelineThunder1.AddMs(4500, "C");
            timelineThunder1.TimelineTrigger += TriggerThunderTimeline;

            timelineThunder2.AddMs(500, "A");
            timelineThunder2.AddMs(1500, "B");
            timelineThunder2.AddMs(1600, "C");
            timelineThunder2.AddMs(3700, "C");
            timelineThunder2.TimelineTrigger += TriggerThunderTimeline;

            timelineThunder3.AddMs(100, "A");
            timelineThunder3.AddMs(200, "B");
            timelineThunder3.AddMs(300, "C");
            timelineThunder3.TimelineTrigger += TriggerThunderTimeline;

            timelineThunder4.AddMs(0, "A");
            timelineThunder4.AddMs(3500, "B");
            timelineThunder4.AddMs(4500, "C");
            timelineThunder4.TimelineTrigger += TriggerThunderTimeline;

            timelineThunder5.AddMs(1100, "A");
            timelineThunder5.AddMs(3500, "B");
            timelineThunder5.AddMs(4700, "C");
            timelineThunder5.TimelineTrigger += TriggerThunderTimeline;

            timelineThunder6.AddMs(1000, "A");
            timelineThunder6.AddMs(1800, "B");
            timelineThunder6.AddMs(6200, "C");
            timelineThunder6.TimelineTrigger += TriggerThunderTimeline;

            timelineThunder7.AddMs(0, "A");
            timelineThunder7.AddMs(200, "B");
            timelineThunder7.AddMs(300, "C");
            timelineThunder7.TimelineTrigger += TriggerThunderTimeline;

            timelineThunder8.AddMs(500, "A");
            timelineThunder8.AddMs(4000, "B");
            timelineThunder8.AddMs(4200, "C");
            timelineThunder8.TimelineTrigger += TriggerThunderTimeline;

            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 0, 50, true), SacnUniversePixel50, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 50, 100), SacnUniversePixel100, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsFrankGhost, 0, 5), SacnUniverseFrankGhost, 1);

            acnOutput.Connect(new Physical.FogMachineA(fogStairsPump1, fogStairsLight1, 1), SacnUniverseDMXFogA);
            acnOutput.Connect(new Physical.FogMachineA(fogStairsPump2, fogStairsLight2, 10), SacnUniverseDMXFogA);

            //acnOutput.Connect(new Physical.SmallRGBStrobe(spiderLight, 1), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.RGBStrobe(wall6Light, 60), SacnUniverseEdmx4A);
            //acnOutput.Connect(new Physical.RGBStrobe(wall9Light, 70), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.RGBStrobe(rockingChairLight, 40), SacnUniverseEdmx4A);
            //acnOutput.Connect(new Physical.RGBStrobe(wall7Light, 80), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.DMXCommandOutput(pictureFrame1, 1, TimeSpan.FromMilliseconds(500), 0), SacnUniverseEdmx4B);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall1Light, 340, 8), SacnUniverseEdmx4A);
            acnOutput.Connect(new Physical.RGBStrobe(wall2Light, 80), SacnUniverseEdmx4A);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall3Light, 330, 8), SacnUniverseEdmx4A);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall4Light, 310, 8), SacnUniverseEdmx4A);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall5Light, 300, 8), SacnUniverseEdmx4A);
            //            acnOutput.Connect(new Physical.MarcGamutParH7(wall6Light, 350, 8), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(stairs1Light, 66), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(stairs2Light, 51), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(treeGhosts, 67), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(treeSkulls, 131), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(spiderEyes, 128), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(popperEyes, 132), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(popper, 133), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.AmericanDJStrobe(flashTree, 100), SacnUniverseEdmx4A);
            acnOutput.Connect(new Physical.EliminatorFlash192(flashUnderSpider, 110), SacnUniverseDMXFogA);
            //            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(pinSpot, 20), 1);

            acnOutput.Connect(new Physical.GenericDimmer(fire, 1), SacnUniverseFire);

            acnOutput.Connect(new Physical.GenericDimmer(frankGhostAir, 10), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(mrPumpkinAir, 11), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(spiderWebLights, 99), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(catAir, 64), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(catLights, 65), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(pumpkinLights, 50), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(gargoyleLightsCrystal, 128), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(gargoyleLightsEyes, 129), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(flyingSkeletonEyes, 130), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(streetNumberEyes, 131), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(bigSpiderEyes, 132), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(hazerFanSpeed, 500), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(hazerHazeOutput, 501), SacnUniverseDMXCat);
            //            acnOutput.Connect(new Physical.RGBIS(testLight1, 260), 1);


            expanderLedmx.DigitalInputs[4].Connect(frankGhostMotion, false);
            expanderLedmx.DigitalInputs[5].Connect(mrPumpkinMotion, false);
            expanderLedmx.DigitalInputs[6].Connect(rockingMotion, false);
            expanderCat.DigitalInputs[7].Connect(catMotion);
            expanderCat.DigitalInputs[6].Connect(secondBeam);
            //            expanderCat.DigitalInputs[6].Connect(firstBeam);
            //expanderCat.DigitalInputs[4].Connect(spiderDropTrigger);
            expanderCat.DigitalInputs[4].Connect(firstBeam);
            expanderLedmx.DigitalInputs[5].Connect(ghostBeam);
            expanderLedmx.DigitalInputs[6].Connect(lastBeam);
            //expanderMrPumpkin.DigitalOutputs[7].Connect(popper);
            //expanderMrPumpkin.DigitalOutputs[6].Connect(fog);
            expanderMrPumpkin.DigitalOutputs[7].Connect(rockingChair);
            expanderMrPumpkin.DigitalOutputs[4].Connect(ladyMovingEyes);
            expanderLedmx.DigitalOutputs[0].Connect(spiderDropRelease);
            expanderLedmx.DigitalOutputs[1].Connect(spiderVenom);
            expanderCat.DigitalOutputs[6].Connect(spiderJump2);
            expanderLedmx.Connect(audioFrankGhost);
            expanderLocal.Connect(audioLocal);
            expanderCat.Connect(audioCat);
            expanderHifi.Connect(audioHifi);
            expanderMrPumpkin.Connect(audioPumpkin);
            expanderAudio2.Connect(audio2);
            expanderPicture.Connect(audioFlying);
            expanderSpider.Connect(audioSpider);
            expanderRocking.Connect(audioRocking);

            expanderHifi.BackgroundAudioFiles = new string[]
            {
                "Thunder1.wav",
                "Thunder2.wav",
                "Thunder3.wav",
                "Thunder4.wav",
                "Thunder5.wav",
                "Thunder6.wav",
                "Thunder7.wav",
                "Thunder8.wav"
            };
            expanderRocking.BackgroundAudioFiles = new string[]
            {
                "68 Creaky Wooden Floorboards.wav"
            };
            expanderLocal.BackgroundAudioFiles = new string[]
            {
                "68 Creaky Wooden Floorboards.wav",
                "Thunder2.wav"
            };

            blockMaster.WhenOutputChanges(x => UpdateOSC());
            blockCat.WhenOutputChanges(x => UpdateOSC());
            blockFirst.WhenOutputChanges(x => UpdateOSC());
            blockPicture.WhenOutputChanges(x => UpdateOSC());
            blockGhost.WhenOutputChanges(x => UpdateOSC());
            blockLast.WhenOutputChanges(x => UpdateOSC());
            blockPumpkin.WhenOutputChanges(x => UpdateOSC());

            Utils.ReactiveOr(blockCat, blockMaster).Controls(grumpyCat.InputTriggerBlock);
            Utils.ReactiveOr(blockPumpkin, blockMaster).Controls(mrPumpkin.InputTriggerBlock);
            Utils.ReactiveOr(blockFrankGhost, blockMaster).Controls(frankGhost.InputTriggerBlock);
            Utils.ReactiveOr(blockSpiderDrop, blockMaster).Controls(spiderDrop.InputTriggerBlock);
            Utils.ReactiveOr(blockFire, blockMaster).Controls(fireProjector.InputTriggerBlock);
            Utils.ReactiveOr(blockPicture, blockMaster).Controls(pictureFrame.InputTriggerBlock);

            catMotion.Controls(grumpyCat.InputTrigger);
            secondBeam.Controls(pictureFrame.InputTrigger);
            mrPumpkinMotion.Controls(mrPumpkin.InputTrigger);
            frankGhostMotion.Controls(frankGhost.InputTrigger);
            spiderDropTrigger.Controls(spiderDrop.InputTrigger);

            rockingMotion.Output.Subscribe(x =>
            {
                if (x && hoursSmall.IsOpen)
                    audioRocking.PlayEffect("Evil-Laugh.wav", 0.15);
            });

            firstBeam.Output.Subscribe(x =>
            {
                if (x && hoursSmall.IsOpen && !emergencyStop.Value && !blockMaster.Value && !blockFirst.Value)
                    subFirst.Run();
            });

            //secondBeam.Output.Subscribe(x =>
            //{
            //    //UpdateOSC();

            //    //if (x && (hoursFull.IsOpen || hoursSmall.IsOpen) && !emergencyStop.Value && !blockMaster.Value && !blockPicture.Value)
            //    //    subPicture.Run();
            //});

            ghostBeam.Output.Subscribe(x =>
            {
                UpdateOSC();

                if (x && hoursFull.IsOpen && !emergencyStop.Value && !blockMaster.Value && !blockGhost.Value)
                    subGhost.Run();
            });

            lastBeam.Output.Subscribe(x =>
            {
                UpdateOSC();

                if (x && hoursFull.IsOpen && !emergencyStop.Value && !blockMaster.Value && !blockLast.Value)
                    subLast.Run();
            });

            subFog
                .RunAction(i =>
                {
                    fog.SetValue(true);
                    lastFogRun = DateTime.Now;
                    i.WaitFor(S(6));
                    fog.SetValue(false);
                });

            subFirst
                .AutoAddDevices(lockPriority: 100)
                .RunAction(i =>
                {
                    //flyingSkeletonEyes.SetBrightness(1.0);
                    //wall3Light.SetColor(Color.FromArgb(255, 0, 100), 0.5);
                    //wall3Light.SetStrobeSpeed(1.0, i.Token);

                    //                    audioPicture.PlayEffect("162 Blood Curdling Scream of Terror.wav");
                    //audioFlying.PlayEffect("05 I'm a Little Teapot.wav", 0.05);
                    //audioFlying.PlayEffect("Who is that knocking.wav");
                    //i.WaitFor(S(3.0));
                    //audioFlying.PlayEffect("Evil-Laugh.wav");

                    i.WaitFor(S(0.5));

                    audioSpider.PlayEffect("Short Laugh.wav");

                    fogStairsPump1.SetThroughput(0.4);
                    fogStairsLight1.SetColor(Color.Purple, 1.0);

                    fogStairsPump2.SetThroughput(0.4);
                    fogStairsLight2.SetColor(Color.Purple, 1.0);

                    i.WaitFor(S(2.0));

                    fogStairsPump1.SetThroughput(0);
                    fogStairsPump2.SetThroughput(0);

                    i.WaitFor(S(2.0));
                    fogStairsLight1.SetBrightness(0);
                    fogStairsLight2.SetBrightness(0);

                    i.WaitFor(S(2.0));
                })
                .TearDown(i =>
                {
                    //Exec.MasterEffect.Fade(wall3Light, 0.5, 0.0, 2000, token: i.Token).Wait();
                });

            subPicture
                .RunAction(ins =>
                {
                    //pictureFrame1.SendCommand(null, 0x01);

                    //ins.WaitFor(S(4.0));
                    //if (bigSpiderEyesToken != null)
                    //    bigSpiderEyesToken.Dispose();

                    //wall8Light.SetColor(Color.White, 1, bigSpiderEyesToken);
                    //wall8Light.SetStrobeSpeed(1, bigSpiderEyesToken);
                    //bigSpiderEyesToken = bigSpiderEyes.TakeControl(100);
                    //bigSpiderEyes.SetBrightness(0, bigSpiderEyesToken);
                    //audioHifi.PlayNewEffect("Happy Halloween.wav");
                    //expanderPicture.SendSerial(0, new byte[] { 0x02 });
                    //i.WaitFor(S(4.0));
                    //sub3dfxRandom.Run();
                    //i.WaitFor(S(10.0));
                    //wall8Light.SetBrightness(0);
                    //subSpiderJump.Run();
                    //i.WaitFor(S(4.0));
                })
                .TearDown(ins =>
                {
                });

            subSpiderJump
                .RunAction(i =>
                {
                    if (bigSpiderEyesToken == null)
                        bigSpiderEyesToken = bigSpiderEyes.TakeControl(100);

                    audio2.PlayNewEffect("348 Spider Hiss.wav", 0, 1);
                    bigSpiderEyes.SetBrightness(1, bigSpiderEyesToken);
                    //spiderDrop.SetValue(true);
                    i.WaitFor(S(0.5));
                    spiderJump2.SetValue(true);
                    i.WaitFor(S(2.0));
                    //spiderDrop.SetValue(false);
                    spiderJump2.SetValue(false);
                })
                .TearDown(i =>
                {
                    bigSpiderEyesToken?.Dispose();
                    bigSpiderEyesToken = null;
                });

            sub3dfxRandom
                .RunAction(i =>
                {
                    byte video;
                    do
                    {
                        video = (byte)(random.Next(3) + 1);
                    } while (video == last3dfxVideo);
                    last3dfxVideo = video;

                    expanderLedmx.SendSerial(0, new byte[] { video });
                    i.WaitFor(S(12.0));
                })
                .TearDown(i =>
                {
                });

            sub3dfxLady
                .RunAction(i =>
                {
                    expanderLedmx.SendSerial(0, new byte[] { 0x02 });
                    i.WaitFor(S(12.0));
                })
                .TearDown(i =>
                {
                });

            sub3dfxMan
                .RunAction(i =>
                {
                    expanderLedmx.SendSerial(0, new byte[] { 0x03 });
                    i.WaitFor(S(12.0));
                })
                .TearDown(i =>
                {
                });

            sub3dfxKids
                .RunAction(i =>
                {
                    expanderLedmx.SendSerial(0, new byte[] { 0x01 });
                    i.WaitFor(S(12.0));
                })
                .TearDown(i =>
                {
                });

            subGhost
                .RunAction(i =>
                {
                    expanderGhost.SendSerial(0, new byte[] { 0x01 });
                    i.WaitFor(S(12.0));
                })
                .TearDown(i =>
                {
                });

            subLast
                .RunAction(i =>
                {
                    popperEyes.SetBrightness(1.0);
                    fog.SetValue(true);
                    lastFogRun = DateTime.Now;
                    audioPop.PlayEffect("Short Laugh.wav");
                    i.WaitFor(S(1.0));
                    popper.SetValue(true);
                    i.WaitFor(S(2.0));
                    audioPop.PlayEffect("Leave Now.wav");
                    i.WaitFor(S(3.0));
                    var tsk = Exec.MasterEffect.Fade(popperEyes, 1.0, 0.0, 2000, token: i.Token);
                    popper.SetValue(false);
                    tsk.Wait();
                })
                .TearDown(i =>
                {
                    fog.SetValue(false);
                    i.WaitFor(S(1.0));
                });

            motionSeq.WhenExecuted
                .Execute(instance =>
                {
                    //video2.PlayVideo("DancingDead_Wall_HD.mp4");

                    //                    instance.WaitFor(S(10));
                })
                .TearDown(instance =>
                {
                });

            faderR.WhenOutputChanges(v => { SetManualColor(); });
            faderG.WhenOutputChanges(v => { SetManualColor(); });
            faderB.WhenOutputChanges(v => { SetManualColor(); });
            faderBright.WhenOutputChanges(v => { SetManualColor(); });

            manualFader.WhenOutputChanges(v =>
            {
                if (v)
                    this.manualFaderToken = pixelsRoofEdge.TakeControl(200);
                else
                {
                    this.manualFaderToken?.Dispose();
                    this.manualFaderToken = null;
                }

                oscServer.SendAllClients("/ManualFader/x", v ? 1 : 0);

                SetManualColor();
            });

            ConfigureOSC();
            ConfigureMIDI();
        }

        Color GetFaderColor()
        {
            return HSV.ColorFromRGB(faderR.Value, faderG.Value, faderB.Value);
        }

        void SetManualColor()
        {
            if (manualFaderToken != null)
            {
                pixelsRoofEdge.SetColor(GetFaderColor(), faderBright.Value, manualFaderToken);
                wall3Light.SetColor(GetFaderColor(), faderBright.Value, manualFaderToken);
            }
        }

        void UpdateOSC()
        {
            oscServer.SendAllClients("/Beams/x",
                firstBeam.Value ? 1 : 0,
                secondBeam.Value ? 1 : 0,
                ghostBeam.Value ? 1 : 0,
                lastBeam.Value ? 1 : 0);

            oscServer.SendAllClients("/Blocks/x",
                blockMaster.Value ? 1 : 0,
                blockCat.Value ? 1 : 0,
                blockFirst.Value ? 1 : 0,
                blockPicture.Value ? 1 : 0,
                blockGhost.Value ? 1 : 0,
                blockLast.Value ? 1 : 0,
                blockPumpkin.Value ? 1 : 0);
        }

        void TriggerThunderTimeline(object sender, Animatroller.Framework.Controller.Timeline<string>.TimelineEventArgs e)
        {
            switch (e.Code)
            {
                case "A":
                    popOutAll.Pop(1.0, color: Color.White);
                    break;

                case "B":
                    popOut2.Pop(0.5, color: Color.White);
                    break;

                case "C":
                    popOut1.Pop(1.0, color: Color.White);
                    break;
            }
        }

        public override void Run()
        {
            SetManualColor();
        }

        public override void Stop()
        {
            audioHifi.PauseBackground();
            audio2.PauseBackground();
        }
    }
}
