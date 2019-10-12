using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Simulator;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Expander = Animatroller.Framework.Expander;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Scenes
{
    internal partial class Halloween2018 : BaseScene
    {
        //const int SacnUniverseDMXFogA = 3;
        //const int SacnUniverseEdmx4A = 20;
        //const int SacnUniverseEdmx4B = 21;
        //const int SacnUniverseDMXCat = 4;
        const int SacnUniverseDMXLedmx = 10;
        const int SacnUniversePixel100 = 5;
        const int SacnUniversePixel50 = 6;
        const int SacnUniverseFrankGhost = 3;
        //const int SacnUniverseFire = 99;

        // E6804 12V - 192.168.240.247

        public enum States
        {
            BackgroundSmall,
            BackgroundFull,
            EmergencyStop,
            Special1,
            Setup
        }

        const int midiChannel = 0;

        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();
        Expander.MidiInput2 midiInput = new Expander.MidiInput2("LPD8", ignoreMissingDevice: true);
        Expander.OscServer oscServer = new Expander.OscServer(8000, forcedClientPort: 8000, registerAutoHandlers: true);
        AudioPlayer audioLocal = new AudioPlayer();
        AudioPlayer audioFrankGhost = new AudioPlayer();
        AudioPlayer audioHead = new AudioPlayer();
        AudioPlayer audioFlying = new AudioPlayer();
        AudioPlayer audioRocking = new AudioPlayer();
        AudioPlayer audioSpider = new AudioPlayer();
        AudioPlayer audioHifi = new AudioPlayer();
        AudioPlayer audioBigEye = new AudioPlayer();
        AudioPlayer audioPopSkull = new AudioPlayer();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8899);
        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.None);
        Expander.MonoExpanderInstance expanderLedmx = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderHifi = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.None);
        Expander.MonoExpanderInstance expanderCat = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderFrankGhost = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderHead = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.None);
        Expander.MonoExpanderInstance expanderFlying = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.None);
        Expander.MonoExpanderInstance expanderRocking = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderBigEye = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.None);
        Expander.MonoExpanderInstance expanderEeebox = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.None);
        Expander.AcnStream acnOutput = new Expander.AcnStream();
        Expander.OscClient bigEyeSender = new Expander.OscClient("192.168.240.155", 8000);

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

        Controller.Subroutine subFog = new Controller.Subroutine();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 emergencyStop = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockMaster = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockFirst = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockPicture = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockRocking = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockBigEye = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockFrankGhost = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockSpiderSquirt = new DigitalInput2(persistState: true);

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

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 setupMode = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 fullOn = new DigitalInput2(persistState: true);

        Effect.Flicker flickerEffect = new Effect.Flicker(0.4, 0.6, false);
        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingGargoyle = new Effect.Pulsating(S(4), 0.1, 0.8, false);
        Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.4, 1.0, false);
        Effect.PopOut2 popOut1 = new Effect.PopOut2(S(0.3));
        Effect.PopOut2 popOut2 = new Effect.PopOut2(S(0.3));
        Effect.PopOut2 popOutAll = new Effect.PopOut2(S(1.2));

        NewGroup grumpyCatGroup = new NewGroup("Grumpy Cat");
        DigitalInput2 catMotion = new DigitalInput2();
        DigitalOutput2 catAir = new DigitalOutput2();
        Dimmer3 catLights = new Dimmer3("Lights in Grumpy Cat");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockCat = new DigitalInput2(persistState: true);
        AudioPlayer audioCat = new AudioPlayer();

        NewGroup rockingChairGroup = new NewGroup("Rocking Chair");
        DigitalInput2 rockingMotion = new DigitalInput2();
        DigitalOutput2 rockingChairMotor = new DigitalOutput2();
        DigitalOutput2 rockingChairEyes = new DigitalOutput2();

        NewGroup theRest = new NewGroup("");

        DigitalInput2 frankGhostMotion = new DigitalInput2();
        DigitalInput2 spiderSquirtBeam = new DigitalInput2();
        DigitalInput2 firstBeam = new DigitalInput2();
        DigitalInput2 pictureBeam = new DigitalInput2();
        DigitalInput2 bigEyeBeam = new DigitalInput2();

        DigitalOutput2 blackLights = new DigitalOutput2();
        DigitalOutput2 fire = new DigitalOutput2();
        DigitalOutput2 waterMist = new DigitalOutput2();
        DigitalOutput2 mrPumpkinAir = new DigitalOutput2();
        DigitalOutput2 frankGhostAir = new DigitalOutput2();
        DigitalOutput2 lowLyingFogMachine = new DigitalOutput2();
        DateTime? lastFogRun = DateTime.Now;
        ThroughputDevice fogStairsPump1 = new ThroughputDevice();
        ThroughputDevice fogStairsPump2 = new ThroughputDevice();
        CommandDevice pictureFrameSender = new CommandDevice();
        Dimmer3 hangingSpiderEyes = new Dimmer3();
        Dimmer3 underFlagSkulls = new Dimmer3();
        Dimmer3 headEyes = new Dimmer3();
        Dimmer3 popSkullEyes = new Dimmer3();
        Dimmer3 mrPumpkinLights = new Dimmer3();
        Dimmer3 catSkeletonEyes = new Dimmer3();
        Dimmer3 gargoyleLightsCrystal = new Dimmer3();
        Dimmer3 gargoyleLightsEyes = new Dimmer3();
        Dimmer3 flyingSkeletonEyes = new Dimmer3();
        Dimmer3 stairs1Light = new Dimmer3("Stairs 1");
        Dimmer3 stairs2Light = new Dimmer3("Stairs 2");
        Dimmer3 hazerFanSpeed = new Dimmer3();
        Dimmer3 hazerHazeOutput = new Dimmer3();

        OperatingHours2 mainSchedule = new OperatingHours2("Hours");

        GroupDimmer allLights = new GroupDimmer();
        GroupDimmer purpleLights = new GroupDimmer();

        StrobeColorDimmer3 fogStairsLight1 = new StrobeColorDimmer3();
        StrobeColorDimmer3 fogStairsLight2 = new StrobeColorDimmer3();
        StrobeColorDimmer3 gargoyleSpotLight = new StrobeColorDimmer3();
        StrobeColorDimmer3 wall1Light = new StrobeColorDimmer3("Wall 1 (cat)");
        //StrobeColorDimmer3 wall2Light = new StrobeColorDimmer3("Wall 2 (flag)");
        //StrobeColorDimmer3 wall3Light = new StrobeColorDimmer3("Wall 3");
        StrobeColorDimmer3 wall4Light = new StrobeColorDimmer3("Wall 4");
        StrobeColorDimmer3 wall5Light = new StrobeColorDimmer3("Wall 5");
        StrobeColorDimmer3 wall6Light = new StrobeColorDimmer3("Wall 6");
        StrobeColorDimmer3 rockingChairLight = new StrobeColorDimmer3("Rocking chair");
        //StrobeColorDimmer3 wall8Light = new StrobeColorDimmer3("Wall 8");
        //StrobeColorDimmer3 wall9Light = new StrobeColorDimmer3("Wall 9");
        StrobeDimmer3 flashFlag = new StrobeDimmer3("Eliminator Flash");
        StrobeDimmer3 flashUnderSpider = new StrobeDimmer3("ADJ Flash");
        StrobeColorDimmer3 pinSpot = new StrobeColorDimmer3("Pin Spot");

        Controller.Timeline<string> timelineThunder1 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder2 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder3 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder4 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder5 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder6 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder7 = new Controller.Timeline<string>(1);
        Controller.Timeline<string> timelineThunder8 = new Controller.Timeline<string>(1);

        GroupControlToken manualFaderToken;
        int soundBoardOutputIndex = 0;

        // Modules
        Modules.HalloweenGrumpyCat grumpyCat;
        Modules.HalloweenFrankGhost frankGhost;
        Modules.HalloweenSpiderSquirt spiderSquirt;
        Modules.HalloweenPictureFrame pictureFrame;
        Modules.HalloweenFlying flyingSkeleton;
        Modules.HalloweenRocker rockingChair;
        Modules.HalloweenBigEye bigEyeModule;

        public Halloween2018(IEnumerable<string> args)
        {
            mainSchedule.AddRange("5:00 pm", "9:00 pm",
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Sunday);
            mainSchedule.AddRange("5:00 pm", "9:00 pm",
                DayOfWeek.Friday, DayOfWeek.Saturday);

            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                {
                    Exec.ExpanderSharedFiles = parts[1];
                }
            }
            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);

            //expanderServer.AddInstance("4ea781ef257442edb524493da8f52220", expanderAudio2);     // rpi-eba6cbc7
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expanderLedmx);      // rpi-eb0092ca
            expanderServer.AddInstance("1583f686014345888c15d7fc9c55ca3c", expanderCat);        // rpi-eb81c94e
            expanderServer.AddInstance("d6fc4e752af04022bf3c1a1166a557bb", expanderHifi);       // rpi-eb428ef1
            //expanderServer.AddInstance("60023fcde5b549b89fa828d31741dd0c", expanderPicture);    // rpi-eb91bc26
            expanderServer.AddInstance("e41d2977931d4887a9417e8adcd87306", expanderRocking);    // rpi-eb6a047c
            expanderServer.AddInstance("999861affa294fd7bbf0601505e9ae09", expanderFrankGhost); // rpi-ebd43a38
            expanderServer.AddInstance("992f8db68e874248b5ee667d23d74ac3", expanderFlying);     // rpi-eb9b3145
            expanderServer.AddInstance("db9b41a596cb4ed28e91f11a59afb95a", expanderHead);      // rpi-eb32e5f9
            expanderServer.AddInstance("acbfada45c674077b9154f6a0e0df359", expanderBigEye);     // rpi-eb35666e
            //expanderServer.AddInstance("2e105175a66549d4a0ab7f8d446c2e29", expanderPopper);     // rpi-eb997095
            expanderServer.AddInstance("4fabc4931566424c870ccb83984b3ffb", expanderEeebox);     // videoplayer1

            masterVolume.ConnectTo(Exec.MasterVolume);


            grumpyCat = new Modules.HalloweenGrumpyCat(
                air: catAir,
                light: catLights,
                audioPlayer: audioCat,
                name: nameof(grumpyCat));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(grumpyCat.InputPower);

            pictureFrame = new Modules.HalloweenPictureFrame(
                medeaWizPlayer: pictureFrameSender,
                name: nameof(pictureFrame));
            stateMachine.WhenStates(States.BackgroundFull).Controls(pictureFrame.InputPower);

            flyingSkeleton = new Modules.HalloweenFlying(
                eyes: flyingSkeletonEyes,
                fogStairsLight1: fogStairsLight1,
                fogStairsLight2: fogStairsLight2,
                fogStairsPump1: fogStairsPump1,
                fogStairsPump2: fogStairsPump2,
                audioPlayer: audioFlying,
                name: nameof(flyingSkeleton));
            stateMachine.WhenStates(States.BackgroundFull).Controls(flyingSkeleton.InputPower);
            firstBeam.Output.Controls(flyingSkeleton.InputTrigger);

            rockingChair = new Modules.HalloweenRocker(
                rockingMotor: rockingChairMotor,
                ladyEyes: rockingChairEyes,
                eyesPopSkull: popSkullEyes,
                strobeLight: rockingChairLight,
                audioPlayerRocker: audioRocking,
                audioPlayerExit: audioPopSkull,
                name: nameof(rockingChair));
            stateMachine.WhenStates(States.BackgroundFull).Controls(rockingChair.InputPower);
            rockingMotion.Output.Controls(rockingChair.InputTrigger);

            bigEyeModule = new Modules.HalloweenBigEye(
                oscSender: bigEyeSender,
                audioPlayer: audioBigEye,
                name: nameof(bigEyeModule));
            stateMachine.WhenStates(States.BackgroundFull).Controls(bigEyeModule.InputPower);
            bigEyeBeam.Output.Controls(bigEyeModule.InputTrigger);

            frankGhost = new Modules.HalloweenFrankGhost(
                air: frankGhostAir,
                light: pixelsFrankGhost,
                audioPlayer: audioFrankGhost,
                name: nameof(frankGhost));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(frankGhost.InputPower);

            spiderSquirt = new Modules.HalloweenSpiderSquirt(
                spiderEyesLight: hangingSpiderEyes,
                headEyesLight: headEyes,
                venom: waterMist,
                strobeLight: flashUnderSpider,
                audioPlayerSpider: audioSpider,
                audioPlayerHead: audioHead,
                name: nameof(spiderSquirt));
            stateMachine.WhenStates(States.BackgroundFull).Controls(spiderSquirt.InputPower);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    mainSchedule.SetForced(true);
                else
                    mainSchedule.SetForced(null);
            });


            flashBaby.Output.Subscribe(x =>
            {
                // Flash
                if (x)
                {
                    allLights.TakeAndHoldControl(priority: 100, name: "FlashBaby");
                    allLights.SetData(Channel.Main, null, Utils.Data(1.0), Utils.Data(Color.White));
                }
                else
                    allLights.ReleaseControl();
            });

            testButton1.Output.Subscribe(x =>
            {
            });

            testButton2.Output.Subscribe(x =>
            {
            });

            testButton3.Output.Subscribe(x =>
            {
            });

            testButton4.Output.Subscribe(x =>
            {
            });

            setupMode.Output.Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.Setup);
                else
                    stateMachine.GoToDefaultState();
            });

            fullOn.Output.Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.BackgroundFull);
                else
                    stateMachine.GoToDefaultState();
            });

            floodLights.Output.Subscribe(x =>
            {
                // Flash
                if (x)
                {
                    allLights.TakeAndHoldControl(priority: 200, name: "FlashBaby");
                    allLights.SetData(Channel.Main, null, Utils.Data(Color.White, 1.0));
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
                    if (mainSchedule.IsOpen)
                        stateMachine.GoToDefaultState();
                    else
                        stateMachine.GoToIdle();
                }
            });

            mainSchedule.Output.Subscribe(x =>
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
            popOut1.ConnectTo(flashFlag);
            popOut2.ConnectTo(wall5Light);
            //popOut2.ConnectTo(wall7Light);
            //popOut2.ConnectTo(flashUnderSpider);
            popOutAll.ConnectTo(wall1Light);
            //popOutAll.ConnectTo(wall2Light);
            //popOutAll.ConnectTo(wall3Light);
            popOutAll.ConnectTo(wall4Light);
            popOutAll.ConnectTo(wall5Light);
            popOutAll.ConnectTo(wall6Light);
            //popOutAll.ConnectTo(wall7Light);
            //popOutAll.ConnectTo(wall8Light);
            //popOutAll.ConnectTo(wall9Light);
            popOutAll.ConnectTo(flashFlag);
            //popOutAll.ConnectTo(flashUnderSpider);
            popOutAll.ConnectTo(pixelsRoofEdge);
            popOutAll.ConnectTo(pixelsFrankGhost);
            //            popOutAll.ConnectTo(pinSpot);

            allLights.Add(
                wall1Light,
                wall4Light,
                wall5Light,
                wall6Light,
                rockingChairLight,
                flashFlag,
                flashUnderSpider,
                pixelsRoofEdge,
                pixelsFrankGhost,
                pinSpot,
                gargoyleSpotLight,
                fogStairsLight1,
                fogStairsLight2);

            purpleLights.Add(
                wall1Light,
                wall4Light,
                wall5Light,
                wall6Light,
                pixelsRoofEdge);

            flickerEffect.ConnectTo(stairs1Light);
            flickerEffect.ConnectTo(stairs2Light);
            flickerEffect.ConnectTo(gargoyleLightsEyes);
            flickerEffect.ConnectTo(mrPumpkinLights);
            pulsatingGargoyle.ConnectTo(gargoyleLightsCrystal);
            pulsatingGargoyle.ConnectTo(gargoyleSpotLight, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(255, 0, 255)));
            pulsatingEffect1.ConnectTo(underFlagSkulls);
            //pulsatingEffect1.ConnectTo(headEyes);
            //pulsatingEffect1.ConnectTo(popSkullEyes);
            //pulsatingEffect1.ConnectTo(hangingSpiderEyes);
            pulsatingEffect1.ConnectTo(catSkeletonEyes);
            pulsatingEffect1.ConnectTo(flyingSkeletonEyes);
            pulsatingEffect1.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(0, 255, 0)));

            stateMachine.For(States.Setup)
                .Controls(1, flickerEffect, pulsatingGargoyle, pulsatingEffect1)
                .Execute(ins =>
                {
                    ins.WaitUntilCancel();
                });

            stateMachine.For(States.BackgroundSmall)
                .Controls(1, flickerEffect, pulsatingGargoyle, pulsatingEffect1)
                .SetUp(ins =>
                {
                    pulsatingGargoyle.ConnectTo(flashFlag);
                })
                .Execute(ins =>
                    {
                        bigEyeSender.Send("/eyecontrol", 1);

                        blackLights.SetValue(true);
                        //                        audioHifi.SetBackgroundVolume(0.5);
                        //                        audioHifi.PlayBackground();
                        //                        ladyMovingEyes.SetValue(true);

                        var purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                            0.16470588235294117);

                        purpleLights.SetData(Channel.Main, null,
                            Utils.Data(purpleColor.Brightness),
                            Utils.Data(purpleColor.Color),
                            Utils.Data(DataElements.ColorUltraViolet, 1.0));

                        ins.WaitUntilCancel();
                    })
                .TearDown(ins =>
                    {
                        pulsatingGargoyle.Disconnect(flashFlag);
                        bigEyeSender.Send("/eyecontrol", 0);
                        audioHifi.PauseBackground();
                        purpleLights.SetBrightness(0.0);
                        blackLights.SetValue(false);
                    });

            stateMachine.For(States.BackgroundFull)
                .Controls(1, flickerEffect, pulsatingGargoyle, pulsatingEffect1)
                .Execute(i =>
                {
                    blackLights.SetValue(true);
                    audioHifi.PlayBackground();
                    audioHifi.SetBackgroundVolume(0.7);

                    var purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                        0.16470588235294117);

                    purpleLights.SetData(Channel.Main, null, Utils.Data(purpleColor.Color, purpleColor.Brightness));

                    while (!i.IsCancellationRequested && stateMachine.CurrentState == States.BackgroundFull)
                    {
                        i.WaitFor(S(0.5));
                        if (!this.lastFogRun.HasValue || (DateTime.Now - this.lastFogRun.Value).TotalMinutes > 5)
                        {
                            // Run the fog for a little while
                            lowLyingFogMachine.SetValue(true);
                            i.WaitFor(S(4));
                            lowLyingFogMachine.SetValue(false);
                            this.lastFogRun = DateTime.Now;
                        }
                    }
                })
                .TearDown(ins =>
                {
                    purpleLights.SetBrightness(0.0);

                    audioHifi.PauseBackground();

                    timelineThunder1.Stop();
                    timelineThunder2.Stop();
                    timelineThunder3.Stop();
                    timelineThunder4.Stop();
                    timelineThunder5.Stop();
                    timelineThunder6.Stop();
                    timelineThunder7.Stop();
                    timelineThunder8.Stop();

                    blackLights.SetValue(false);
                });

            stateMachine.For(States.EmergencyStop)
                .Execute(ins =>
                {
                    // Do nothing
                    ins.WaitUntilCancel();
                });

            stateMachine.For(States.Special1)
                .Execute(ins =>
                {
                    //audio2.PlayNewEffect("640 The Demon Exorcised.wav");

                    ins.WaitUntilCancel();
                });


            audioHifi.AudioTrackStart.Subscribe(x =>
            {
                // Next track
                switch (x.Filename)
                {
                    case "Thunder1.wav":
                        timelineThunder1.Start();
                        audioHifi.PlayEffect("scream.wav");
                        break;

                    case "Thunder2.wav":
                        timelineThunder2.Start();
                        break;

                    case "Thunder3.wav":
                        timelineThunder3.Start();
                        break;

                    case "Thunder4.wav":
                        timelineThunder4.Start();
                        audioHifi.PlayEffect("424 Coyote Howling.wav");
                        break;

                    case "Thunder5.wav":
                        timelineThunder5.Start();
                        audioHifi.PlayEffect("Raven.wav", 0.5);
                        break;

                    case "Thunder6.wav":
                        timelineThunder6.Start();
                        audioHifi.PlayEffect("Tolling Bell.wav", 0.5);
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

            acnOutput.Connect(new Physical.FogMachineA(fogStairsPump1, fogStairsLight1, 70), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.FogMachineA(fogStairsPump2, fogStairsLight2, 80), SacnUniverseDMXLedmx);

            acnOutput.Connect(new Physical.SmallRGBStrobe(gargoyleSpotLight, 4), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.RGBStrobe(wall6Light, 60), SacnUniverseEdmx4A);
            ////acnOutput.Connect(new Physical.RGBStrobe(wall9Light, 70), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.MFL7x10WPar(rockingChairLight, 320), SacnUniverseDMXLedmx);
            ////acnOutput.Connect(new Physical.RGBStrobe(wall7Light, 80), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.DMXCommandOutput(pictureFrameSender, 1, TimeSpan.FromMilliseconds(500), 0), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.DMXCommandOutput(lady3dfx, 1, TimeSpan.FromMilliseconds(500), 0), SacnUniverseEdmx4A);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall1Light, 340, 8), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.RGBStrobe(wall2Light, 80), SacnUniverseEdmx4A);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall4Light, 330, 8), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall5Light, 300, 8), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall6Light, 310, 8), SacnUniverseDMXLedmx);
            //            acnOutput.Connect(new Physical.MarcGamutParH7(wall6Light, 350, 8), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(stairs1Light, 65), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(stairs2Light, 202), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(treeGhosts, 67), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(headEyes, 180), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(underFlagSkulls, 128), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(hangingSpiderEyes, 256), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(catSkeletonEyes, 257), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(popSkullEyes, 261), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(spiderEyes, 257), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(popperEyes, 132), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(popper, 133), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.AmericanDJStrobe(flashUnderSpider, 100), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.EliminatorFlash192(flashFlag, 110), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(pinSpot, 20), SacnUniverseDMXLedmx);

            //acnOutput.Connect(new Physical.GenericDimmer(fire, 1), SacnUniverseFire);

            acnOutput.Connect(new Physical.GenericDimmer(frankGhostAir, 200), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(mrPumpkinAir, 11), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(spiderWebLights, 99), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(catAir, 10), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(blackLights, 11), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(catLights, 64), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(mrPumpkinLights, 66), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(gargoyleLightsCrystal, 260), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(gargoyleLightsEyes, 259), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(flyingSkeletonEyes, 258), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(smallSpiderEyes, 135), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(streetNumberEyes, 131), SacnUniverseDMXCat);
            //acnOutput.Connect(new Physical.GenericDimmer(bigSpiderEyes, 132), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(hazerFanSpeed, 500), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(hazerHazeOutput, 501), SacnUniverseDMXLedmx);
            //            acnOutput.Connect(new Physical.RGBIS(testLight1, 260), 1);


            expanderLedmx.InvertedInputs[4] = true;
            expanderLedmx.InvertedInputs[6] = true;

            expanderCat.DigitalInputs[5].Connect(frankGhostMotion);
            expanderLedmx.DigitalInputs[5].Connect(rockingMotion);
            expanderCat.DigitalInputs[4].Connect(catMotion);
            expanderFrankGhost.DigitalInputs[4].Connect(firstBeam);
            expanderFrankGhost.DigitalInputs[5].Connect(pictureBeam);
            expanderLedmx.DigitalInputs[4].Connect(spiderSquirtBeam);
            expanderCat.DigitalOutputs[7].Connect(waterMist);
            expanderLedmx.DigitalInputs[6].Connect(bigEyeBeam);
            expanderLedmx.DigitalOutputs[0].Connect(lowLyingFogMachine);
            expanderRocking.DigitalOutputs[0].Connect(rockingChairMotor);
            expanderRocking.DigitalOutputs[1].Connect(rockingChairEyes);

            expanderLedmx.Connect(audioSpider);
            expanderCat.Connect(audioCat);
            expanderHifi.Connect(audioHifi);
            expanderLocal.Connect(audioLocal);
            expanderFrankGhost.Connect(audioFrankGhost);
            expanderHead.Connect(audioHead);
            expanderBigEye.Connect(audioBigEye);
            expanderEeebox.Connect(audioPopSkull);
            expanderFlying.Connect(audioFlying);
            expanderRocking.Connect(audioRocking);
            //expanderPopper.Connect(audioPopper);

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

            blockMaster.WhenOutputChanges(x => UpdateOSC());
            blockCat.WhenOutputChanges(x => UpdateOSC());
            blockFirst.WhenOutputChanges(x => UpdateOSC());
            blockPicture.WhenOutputChanges(x => UpdateOSC());
            blockBigEye.WhenOutputChanges(x => UpdateOSC());

            Utils.ReactiveOr(blockCat, blockMaster).Controls(grumpyCat.InputTriggerBlock);
            Utils.ReactiveOr(blockFrankGhost, blockMaster).Controls(frankGhost.InputTriggerBlock);
            Utils.ReactiveOr(blockSpiderSquirt, blockMaster).Controls(spiderSquirt.InputTriggerBlock);
            Utils.ReactiveOr(blockPicture, blockMaster).Controls(pictureFrame.InputTriggerBlock);
            Utils.ReactiveOr(blockRocking, blockMaster).Controls(rockingChair.InputTriggerBlock);
            Utils.ReactiveOr(blockBigEye, blockMaster).Controls(bigEyeModule.InputTriggerBlock);
            Utils.ReactiveOr(blockFirst, blockMaster).Controls(flyingSkeleton.InputTriggerBlock);

            catMotion.Controls(grumpyCat.InputTrigger);
            pictureBeam.Controls(pictureFrame.InputTrigger);
            frankGhostMotion.Controls(frankGhost.InputTrigger);
            spiderSquirtBeam.Controls(spiderSquirt.InputTrigger);

            subFog
                .RunAction(i =>
                {
                    lowLyingFogMachine.SetValue(true);
                    lastFogRun = DateTime.Now;
                    i.WaitFor(S(4));
                    lowLyingFogMachine.SetValue(false);
                });

            faderR.WhenOutputChanges(v => { SetManualColor(); });
            faderG.WhenOutputChanges(v => { SetManualColor(); });
            faderB.WhenOutputChanges(v => { SetManualColor(); });
            faderBright.WhenOutputChanges(v => { SetManualColor(); });

            manualFader.WhenOutputChanges(v =>
            {
                if (v)
                {
                    this.manualFaderToken = new GroupControlToken(priority: 200);
                    this.manualFaderToken.AddRange(channel: Channel.Main, pixelsRoofEdge, wall4Light, wall6Light, rockingChairLight);
                }
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
                pixelsRoofEdge.SetColor(GetFaderColor(), faderBright.Value, token: manualFaderToken);
                wall4Light.SetColor(GetFaderColor(), faderBright.Value, token: manualFaderToken);
                wall6Light.SetColor(GetFaderColor(), faderBright.Value, token: manualFaderToken);
                rockingChairLight.SetColor(GetFaderColor(), faderBright.Value, token: manualFaderToken);
                //popSkullEyes.SetBrightness(faderBright.Value, token: manualFaderToken);
            }
        }

        void UpdateOSC()
        {
            /*
                        oscServer.SendAllClients("/Blocks/x",
                            blockMaster.Value ? 1 : 0,
                            blockCat.Value ? 1 : 0,
                            blockFirst.Value ? 1 : 0,
                            blockPicture.Value ? 1 : 0,
                            blockGhost.Value ? 1 : 0,
                            blockLast.Value ? 1 : 0,
                            blockPumpkin.Value ? 1 : 0);*/
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
        }
    }
}
