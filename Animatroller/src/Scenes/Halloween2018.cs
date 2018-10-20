using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Expander = Animatroller.Framework.Expander;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Scenes
{
    internal partial class Halloween2018 : BaseScene
    {
        const int SacnUniverseDMXFogA = 3;
        const int SacnUniverseEdmx4A = 20;
        const int SacnUniverseEdmx4B = 21;
        const int SacnUniverseDMXCat = 4;
        const int SacnUniverseDMXLedmx = 10;
        const int SacnUniversePixel100 = 5;
        const int SacnUniversePixel50 = 6;
        const int SacnUniverseFrankGhost = 3;
        const int SacnUniverseFire = 99;

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
        AudioPlayer audioPumpkin = new AudioPlayer();
        AudioPlayer audioFrankGhost = new AudioPlayer();
        AudioPlayer audioSpider = new AudioPlayer();
        AudioPlayer audioRocking = new AudioPlayer();
        AudioPlayer audioCat = new AudioPlayer();
        AudioPlayer audioHifi = new AudioPlayer();
        AudioPlayer audioPopper = new AudioPlayer();
        AudioPlayer audioFlying = new AudioPlayer();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8899);
        Expander.MonoExpanderInstance expanderLedmx = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderHifi = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderPicture = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderGhost = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderCat = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderFrankGhost = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderSpider = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderRocking = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderFlying = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderPopper = new Expander.MonoExpanderInstance();
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

        Controller.Subroutine sub3dfxLady = new Controller.Subroutine();
        Controller.Subroutine subSpiderJump = new Controller.Subroutine();
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
        DigitalInput2 blockRocking = new DigitalInput2(persistState: true);

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

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 setupMode = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 fullOn = new DigitalInput2(persistState: true);

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
        DigitalInput2 lastBeam = new DigitalInput2();
        DigitalOutput2 catAir = new DigitalOutput2();
        DigitalOutput2 fire = new DigitalOutput2();
        DigitalOutput2 mrPumpkinAir = new DigitalOutput2();
        DigitalOutput2 frankGhostAir = new DigitalOutput2();
        DigitalOutput2 lastFog = new DigitalOutput2();
        DigitalOutput2 popper = new DigitalOutput2();
        DigitalOutput2 spiderDropRelease = new DigitalOutput2();
        DigitalOutput2 spiderVenom = new DigitalOutput2();
        DigitalOutput2 spiderJump2 = new DigitalOutput2();
        DigitalOutput2 ladyMovingEyes = new DigitalOutput2();
        DigitalOutput2 rockingChairMotor = new DigitalOutput2();
        DateTime? lastFogRun = DateTime.Now;
        ThroughputDevice fogStairsPump1 = new ThroughputDevice();
        ThroughputDevice fogStairsPump2 = new ThroughputDevice();
        CommandDevice pictureFrame1 = new CommandDevice();
        CommandDevice lady3dfx = new CommandDevice();
        Dimmer3 catLights = new Dimmer3("Lights in Grumpy Cat");
        Dimmer3 pumpkinLights = new Dimmer3();
        Dimmer3 spiderWebLights = new Dimmer3();
        Dimmer3 spiderEyes = new Dimmer3();
        Dimmer3 streetNumberEyes = new Dimmer3();
        Dimmer3 bigSpiderEyes = new Dimmer3();
        Dimmer3 gargoyleLightsCrystal = new Dimmer3();
        Dimmer3 gargoyleLightsEyes = new Dimmer3();
        Dimmer3 flyingSkeletonEyes = new Dimmer3();
        Dimmer3 smallSpiderEyes = new Dimmer3();
        Dimmer3 hazerFanSpeed = new Dimmer3();
        Dimmer3 hazerHazeOutput = new Dimmer3();
        Dimmer3 stairs1Light = new Dimmer3("Stairs 1");
        Dimmer3 stairs2Light = new Dimmer3("Stairs 2");
        Dimmer3 treeGhosts = new Dimmer3();
        Dimmer3 treeSkulls = new Dimmer3();
        Dimmer3 popperEyes = new Dimmer3();

        OperatingHours2 mainSchedule = new OperatingHours2("Hours");

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

        GroupControlToken manualFaderToken;
        int soundBoardOutputIndex = 0;

        // Modules
        Modules.HalloweenGrumpyCat grumpyCat;
        Modules.HalloweenMrPumpkin mrPumpkin;
        Modules.HalloweenFrankGhost frankGhost;
        Modules.HalloweenSpiderDrop spiderDrop;
        Modules.FireProjector fireProjector;
        Modules.HalloweenPictureFrame pictureFrame;
        Modules.HalloweenFlying flyingSkeleton;
        Modules.HalloweenRocker rockingChair;

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

            //expanderServer.AddInstance("4ea781ef257442edb524493da8f52220", expanderAudio2);     // rpi-eba6cbc7
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expanderLedmx);      // rpi-eb0092ca
            expanderServer.AddInstance("1583f686014345888c15d7fc9c55ca3c", expanderCat);        // rpi-eb81c94e
            expanderServer.AddInstance("d6fc4e752af04022bf3c1a1166a557bb", expanderHifi);       // rpi-eb428ef1
            expanderServer.AddInstance("60023fcde5b549b89fa828d31741dd0c", expanderPicture);    // rpi-eb91bc26
            expanderServer.AddInstance("e41d2977931d4887a9417e8adcd87306", expanderGhost);      // rpi-eb6a047c
            expanderServer.AddInstance("999861affa294fd7bbf0601505e9ae09", expanderFrankGhost); // rpi-ebd43a38
            expanderServer.AddInstance("992f8db68e874248b5ee667d23d74ac3", expanderSpider);     // rpi-eb9b3145
            expanderServer.AddInstance("db9b41a596cb4ed28e91f11a59afb95a", expanderRocking);    // rpi-eb32e5f9
            expanderServer.AddInstance("acbfada45c674077b9154f6a0e0df359", expanderFlying);     // rpi-eb35666e
            expanderServer.AddInstance("2e105175a66549d4a0ab7f8d446c2e29", expanderPopper);     // rpi-eb997095

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
                ladyEyes: ladyMovingEyes,
                strobeLight: rockingChairLight,
                audioPlayer: audioRocking,
                name: nameof(pictureFrame));
            stateMachine.WhenStates(States.BackgroundFull).Controls(rockingChair.InputPower);

            rockingMotion.Output.Controls(rockingChair.InputTrigger);

            /*            mrPumpkin = new Modules.HalloweenMrPumpkin(
                            air: mrPumpkinAir,
                            light: pumpkinLights,
                            audioPlayer: audioPumpkin,
                            name: nameof(mrPumpkin));
                        stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(mrPumpkin.InputPower);*/

            frankGhost = new Modules.HalloweenFrankGhost(
                air: frankGhostAir,
                light: pixelsFrankGhost,
                audioPlayer: audioFrankGhost,
                name: nameof(frankGhost));
            stateMachine.WhenStates(States.BackgroundFull, States.BackgroundSmall).Controls(frankGhost.InputPower);

            spiderDrop = new Modules.HalloweenSpiderDrop(
                smallSpiderEyes: smallSpiderEyes,
                eyesLight: spiderEyes,
                drop: spiderDropRelease,
                venom: spiderVenom,
                strobeLight: flashUnderSpider,
                audioPlayer: audioSpider,
                name: nameof(spiderDrop));
            stateMachine.WhenStates(States.BackgroundFull).Controls(spiderDrop.InputPower);

            fireProjector = new Modules.FireProjector(
                fire: fire,
                name: nameof(fireProjector));
            stateMachine.WhenStates(States.BackgroundFull, States.Setup).Controls(fireProjector.InputPower);

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
            //flickerEffect.ConnectTo(flyingSkeletonEyes);
            flickerEffect.ConnectTo(streetNumberEyes);
            flickerEffect.ConnectTo(bigSpiderEyes);
            pulsatingGargoyle.ConnectTo(gargoyleLightsCrystal);
            pulsatingGargoyle.ConnectTo(treeSkulls);
            //pulsatingGargoyle.ConnectTo(spiderEyes);
            //pulsatingEffect1.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(0, 255, 0)));
            //pulsatingEffect2.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(255, 0, 0)));

            pulsatingGargoyle.ConnectTo(spiderWebLights);

            stateMachine.For(States.Setup)
                .Controls(1, flickerEffect, pulsatingGargoyle)
                .Execute(ins =>
                {
                    ins.WaitUntilCancel();
                });

            stateMachine.For(States.BackgroundSmall)
                .Controls(1, flickerEffect, pulsatingGargoyle)
                .Execute(i =>
                    {
                        treeGhosts.SetBrightness(1.0);
                        treeSkulls.SetBrightness(1.0);
                        //                        audioHifi.SetBackgroundVolume(0.5);
                        //                        audioHifi.PlayBackground();
                        //                        ladyMovingEyes.SetValue(true);

                        var purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                            0.16470588235294117);

                        purpleLights.SetData(Channel.Main, null,
                            Utils.Data(purpleColor.Brightness),
                            Utils.Data(purpleColor.Color),
                            Utils.Data(DataElements.ColorUltraViolet, 1.0));

                        i.WaitUntilCancel();
                    })
                .TearDown(instance =>
                    {
                        ladyMovingEyes.SetValue(false);
                        Exec.Cancel(sub3dfxLady);
                        audioHifi.PauseBackground();
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
                    audioHifi.SetBackgroundVolume(0.7);
                    //Exec.Execute(sub3dfxLady);

                    var purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                        0.16470588235294117);

                    purpleLights.SetData(Channel.Main, null, Utils.Data(purpleColor.Color, purpleColor.Brightness));

                    while (!i.IsCancellationRequested && stateMachine.CurrentState == States.BackgroundFull)
                    {
                        i.WaitFor(S(0.5));
                        if (!this.lastFogRun.HasValue || (DateTime.Now - this.lastFogRun.Value).TotalMinutes > 5)
                        {
                            // Run the fog for a little while
                            lastFog.SetValue(true);
                            i.WaitFor(S(4));
                            lastFog.SetValue(false);
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

                    timelineThunder1.Stop();
                    timelineThunder2.Stop();
                    timelineThunder3.Stop();
                    timelineThunder4.Stop();
                    timelineThunder5.Stop();
                    timelineThunder6.Stop();
                    timelineThunder7.Stop();
                    timelineThunder8.Stop();

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
                    //audio2.PlayNewEffect("640 The Demon Exorcised.wav");

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

            //acnOutput.Connect(new Physical.FogMachineA(fogStairsPump1, fogStairsLight1, 1), SacnUniverseDMXFogA);
            //acnOutput.Connect(new Physical.FogMachineA(fogStairsPump2, fogStairsLight2, 10), SacnUniverseDMXFogA);

            //acnOutput.Connect(new Physical.SmallRGBStrobe(spiderLight, 1), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.RGBStrobe(wall6Light, 60), SacnUniverseEdmx4A);
            ////acnOutput.Connect(new Physical.RGBStrobe(wall9Light, 70), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.RGBStrobe(rockingChairLight, 40), SacnUniverseEdmx4A);
            ////acnOutput.Connect(new Physical.RGBStrobe(wall7Light, 80), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.DMXCommandOutput(pictureFrame1, 1, TimeSpan.FromMilliseconds(500), 0), SacnUniverseEdmx4B);
            //acnOutput.Connect(new Physical.DMXCommandOutput(lady3dfx, 1, TimeSpan.FromMilliseconds(500), 0), SacnUniverseEdmx4A);
            //acnOutput.Connect(new Physical.MarcGamutParH7(wall1Light, 340, 8), SacnUniverseEdmx4A);
            //acnOutput.Connect(new Physical.RGBStrobe(wall2Light, 80), SacnUniverseEdmx4A);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall4Light, 330, 8), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall6Light, 310, 8), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.MarcGamutParH7(wall5Light, 300, 8), SacnUniverseEdmx4A);
            //            acnOutput.Connect(new Physical.MarcGamutParH7(wall6Light, 350, 8), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(stairs1Light, 65), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(stairs2Light, 202), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(treeGhosts, 67), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(treeSkulls, 128), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(spiderEyes, 128), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(popperEyes, 132), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(popper, 133), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.AmericanDJStrobe(flashTree, 100), SacnUniverseEdmx4A);
            //acnOutput.Connect(new Physical.EliminatorFlash192(flashUnderSpider, 110), SacnUniverseDMXFogA);
            //            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(pinSpot, 20), 1);

            //acnOutput.Connect(new Physical.GenericDimmer(fire, 1), SacnUniverseFire);

            acnOutput.Connect(new Physical.GenericDimmer(frankGhostAir, 200), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(mrPumpkinAir, 11), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(spiderWebLights, 99), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(catAir, 10), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(catLights, 64), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(pumpkinLights, 50), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(gargoyleLightsCrystal, 128), SacnUniverseDMXCat);
            //acnOutput.Connect(new Physical.GenericDimmer(gargoyleLightsEyes, 129), SacnUniverseDMXCat);
            //acnOutput.Connect(new Physical.GenericDimmer(flyingSkeletonEyes, 134), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(smallSpiderEyes, 135), SacnUniverseDMXLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(streetNumberEyes, 131), SacnUniverseDMXCat);
            //acnOutput.Connect(new Physical.GenericDimmer(bigSpiderEyes, 132), SacnUniverseDMXCat);
            //acnOutput.Connect(new Physical.GenericDimmer(hazerFanSpeed, 500), SacnUniverseDMXFogA);
            //acnOutput.Connect(new Physical.GenericDimmer(hazerHazeOutput, 501), SacnUniverseDMXFogA);
            //            acnOutput.Connect(new Physical.RGBIS(testLight1, 260), 1);


            expanderCat.DigitalInputs[5].Connect(frankGhostMotion, false);
            //expanderLedmx.DigitalInputs[5].Connect(mrPumpkinMotion, false);
            //expanderLedmx.DigitalInputs[6].Connect(rockingMotion, false);
            expanderCat.DigitalInputs[4].Connect(catMotion);
            //expanderCat.DigitalInputs[6].Connect(secondBeam);
            //expanderCat.DigitalInputs[5].Connect(spiderDropTrigger, inverted: true);
            expanderFrankGhost.DigitalInputs[0].Connect(firstBeam);
            //expanderLedmx.DigitalInputs[7].Connect(lastBeam);
            ////expanderMrPumpkin.DigitalOutputs[7].Connect(popper);
            //expanderLedmx.DigitalOutputs[2].Connect(lastFog, inverted: true);
            //expanderMrPumpkin.DigitalOutputs[7].Connect(rockingChairMotor);
            //expanderMrPumpkin.DigitalOutputs[4].Connect(ladyMovingEyes);
            //expanderLedmx.DigitalOutputs[0].Connect(spiderDropRelease);
            //expanderLedmx.DigitalOutputs[1].Connect(spiderVenom);
            //expanderCat.DigitalOutputs[6].Connect(spiderJump2);

            //expanderLedmx.Connect(audioFrankGhost);
            expanderCat.Connect(audioCat);
            expanderHifi.Connect(audioHifi);
            expanderFrankGhost.Connect(audioFrankGhost);
            //expanderAudio2.Connect(audio2);
            expanderFlying.Connect(audioFlying);
            expanderSpider.Connect(audioSpider);
            expanderRocking.Connect(audioRocking);
            expanderPopper.Connect(audioPopper);

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
            blockGhost.WhenOutputChanges(x => UpdateOSC());
            blockLast.WhenOutputChanges(x => UpdateOSC());
            blockPumpkin.WhenOutputChanges(x => UpdateOSC());

            Utils.ReactiveOr(blockCat, blockMaster).Controls(grumpyCat.InputTriggerBlock);
            //Utils.ReactiveOr(blockPumpkin, blockMaster).Controls(mrPumpkin.InputTriggerBlock);
            Utils.ReactiveOr(blockFrankGhost, blockMaster).Controls(frankGhost.InputTriggerBlock);
            Utils.ReactiveOr(blockSpiderDrop, blockMaster).Controls(spiderDrop.InputTriggerBlock);
            Utils.ReactiveOr(blockFire, blockMaster).Controls(fireProjector.InputTriggerBlock);
            Utils.ReactiveOr(blockPicture, blockMaster).Controls(pictureFrame.InputTriggerBlock);
            Utils.ReactiveOr(blockRocking, blockMaster).Controls(rockingChair.InputTriggerBlock);
            Utils.ReactiveOr(blockFirst, blockMaster).Controls(flyingSkeleton.InputTriggerBlock);

            catMotion.Controls(grumpyCat.InputTrigger);
            secondBeam.Controls(pictureFrame.InputTrigger);
            //mrPumpkinMotion.Controls(mrPumpkin.InputTrigger);
            frankGhostMotion.Controls(frankGhost.InputTrigger);
            spiderDropTrigger.Controls(spiderDrop.InputTrigger);

            spiderDropTrigger.Output.Subscribe(x =>
            {
                if (x)
                    Exec.Execute(sub3dfxLady);
            });

            lastBeam.Output.Subscribe(x =>
            {
                UpdateOSC();

                if (x && (stateMachine.CurrentState == States.BackgroundFull || stateMachine.CurrentState == States.Setup) && !emergencyStop.Value && !blockMaster.Value && !blockLast.Value)
                    subLast.Run();
            });

            subFog
                .RunAction(i =>
                {
                    lastFog.SetValue(true);
                    lastFogRun = DateTime.Now;
                    i.WaitFor(S(4));
                    lastFog.SetValue(false);
                });

            sub3dfxLady
                .RunAction(i =>
                {
                    i.WaitFor(S(1));

                    if (random.Next(2) == 0)
                    {
                        lady3dfx.SendCommand(null, 99);
                        i.WaitFor(S(30));
                    }
                    else
                    {
                        lady3dfx.SendCommand(null, (byte)(random.Next(4) + 1));
                        i.WaitFor(S(60 * 2.0));
                    }
                })
                .TearDown(i =>
                {
                    lady3dfx.SendCommand(null, 255);
                });

            subLast
                .RunAction(ins =>
                {
                    popperEyes.SetBrightness(1.0);
                    lastFog.SetValue(true);
                    lastFogRun = DateTime.Now;
                    audioPopper.PlayEffect("Short Laugh.wav");
                    ins.WaitFor(S(1.0));
                    popper.SetValue(true);
                    ins.WaitFor(S(2.0));
                    audioPopper.PlayEffect("Leave Now.wav");
                    ins.WaitFor(S(3.0));
                    var tsk = Exec.MasterEffect.Fade(popperEyes, 1.0, 0.0, 2000, token: ins.Token);
                    popper.SetValue(false);
                    tsk.Wait();
                })
                .TearDown(ins =>
                {
                    lastFog.SetValue(false);
                    ins.WaitFor(S(1.0));
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
                {
                    this.manualFaderToken = new GroupControlToken(priority: 200);
                    this.manualFaderToken.AddRange(channel: Channel.Main, pixelsRoofEdge, wall4Light, wall6Light);
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
            }
        }

        void UpdateOSC()
        {
/*            oscServer.SendAllClients("/Beams/x",
                firstBeam.Value ? 1 : 0,
                secondBeam.Value ? 1 : 0,
                spiderDropTrigger.Value ? 1 : 0,
                lastBeam.Value ? 1 : 0);

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
