using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Reactive;
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
    internal partial class Halloween2016 : BaseScene
    {
        const int SacnUniverseDMXCat = 4;
        const int SacnUniverseDMXLedmx = 10;
        const int SacnUniversePixel100 = 5;
        const int SacnUniversePixel50 = 6;

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
        Expander.OscServer oscServer = new Expander.OscServer(8000);
        AudioPlayer audio1 = new AudioPlayer();
        AudioPlayer audioCat = new AudioPlayer();
        AudioPlayer audioHifi = new AudioPlayer();
        AudioPlayer audio2 = new AudioPlayer();
        AudioPlayer audioPop = new AudioPlayer();
        AudioPlayer audioDIN = new AudioPlayer();
        AudioPlayer audioPicture = new AudioPlayer();
        VideoPlayer video3dfx = new VideoPlayer();
        VideoPlayer video2 = new VideoPlayer();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8899);
        Expander.MonoExpanderInstance expanderLedmx = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderAudio2 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderHifi = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderPicture = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderGhost = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderCat = new Expander.MonoExpanderInstance();
        //Expander.Raspberry raspberry3dfx = new Expander.Raspberry("192.168.240.226:5005", 3334);
        //Expander.Raspberry raspberryPop = new Expander.Raspberry("192.168.240.123:5005", 3335);
        //Expander.Raspberry raspberryDIN = new Expander.Raspberry("192.168.240.127:5005", 3337);
        //Expander.Raspberry raspberryVideo2 = new Expander.Raspberry("192.168.240.124:5005", 3336);
        Expander.AcnStream acnOutput = new Expander.AcnStream();

        VirtualPixel1D3 pixelsRoofEdge = new VirtualPixel1D3(150);
        AnalogInput3 faderR = new AnalogInput3(persistState: true);
        AnalogInput3 faderG = new AnalogInput3(persistState: true);
        AnalogInput3 faderB = new AnalogInput3(persistState: true);
        AnalogInput3 faderBright = new AnalogInput3(persistState: true);
        DigitalInput2 manualFader = new DigitalInput2(persistState: true);
        AnalogInput3 masterVolume = new AnalogInput3(persistState: true, defaultValue: 1.0);

        AnalogInput3 inputBrightness = new AnalogInput3(true, name: "Brightness");
        AnalogInput3 inputH = new AnalogInput3(true, name: "Hue");
        AnalogInput3 inputS = new AnalogInput3(true, name: "Saturation");

        Controller.Subroutine subFinal = new Controller.Subroutine();
        Controller.Subroutine subFirst = new Controller.Subroutine();
        Controller.Subroutine subPicture = new Controller.Subroutine();
        Controller.Subroutine subGhost = new Controller.Subroutine();
        Controller.Subroutine subVideo = new Controller.Subroutine();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 emergencyStop = new DigitalInput2(persistState: true);
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockMaster = new DigitalInput2(persistState: true);
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 blockCat = new DigitalInput2(persistState: true);

        Effect.Flicker flickerEffect = new Effect.Flicker(0.4, 0.6, false);
        Effect.Pulsating pulsatingCatLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Effect.Pulsating pulsatingCatHigh = new Effect.Pulsating(S(2), 0.5, 1.0, false);
        Effect.Pulsating pulsatingPumpkinLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Effect.Pulsating pulsatingPumpkinHigh = new Effect.Pulsating(S(2), 0.5, 1.0, false);
        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingGargoyle = new Effect.Pulsating(S(4), 0.5, 1.0, false);
        Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.4, 1.0, false);
        Effect.PopOut2 popOut1 = new Effect.PopOut2(S(0.3));
        Effect.PopOut2 popOut2 = new Effect.PopOut2(S(0.3));
        Effect.PopOut2 popOutAll = new Effect.PopOut2(S(1.2));

        DigitalOutput2 spiderCeiling = new DigitalOutput2("Spider Ceiling");
        DigitalOutput2 spiderCeilingDrop = new DigitalOutput2("Spider Ceiling Drop");
        DigitalInput2 pumpkinMotion = new DigitalInput2();
        DigitalInput2 catMotion = new DigitalInput2();
        DigitalInput2 firstBeam = new DigitalInput2();
        DigitalInput2 secondBeam = new DigitalInput2();
        DigitalInput2 ghostBeam = new DigitalInput2();
        DigitalInput2 motion2 = new DigitalInput2();
        DigitalOutput2 catAir = new DigitalOutput2(initial: true);
        DigitalOutput2 catMrsPumpkin = new DigitalOutput2(initial: true);
        DigitalOutput2 fog = new DigitalOutput2();
        DateTime? lastFogRun = DateTime.Now;
        DigitalOutput2 candyEyes = new DigitalOutput2();
        Dimmer3 catLights = new Dimmer3();
        Dimmer3 pumpkinLights = new Dimmer3();
        Dimmer3 spiderWebLights = new Dimmer3();
        Dimmer3 spiderEyes = new Dimmer3();
        Dimmer3 gargoyleLightsCrystal = new Dimmer3();
        Dimmer3 gargoyleLightsEyes = new Dimmer3();
        Dimmer3 flyingSkeletonEyes = new Dimmer3();
        Dimmer3 hazerFanSpeed = new Dimmer3();
        Dimmer3 hazerHazeOutput = new Dimmer3();
        Dimmer3 stairs1Light = new Dimmer3("Stairs 1");
        Dimmer3 stairs2Light = new Dimmer3("Stairs 2");
        Dimmer3 treeGhosts = new Dimmer3();
        Dimmer3 treeSkulls = new Dimmer3();
        DigitalOutput2 george1 = new DigitalOutput2();
        DigitalOutput2 george2 = new DigitalOutput2();
        DigitalOutput2 popper = new DigitalOutput2();
        DigitalOutput2 dropSpiderEyes = new DigitalOutput2();

        OperatingHours2 hoursSmall = new OperatingHours2("Hours Small");
        OperatingHours2 hoursFull = new OperatingHours2("Hours Full");

        GroupDimmer allLights = new GroupDimmer();
        GroupDimmer purpleLights = new GroupDimmer();

        StrobeColorDimmer3 spiderLight = new StrobeColorDimmer3("Spider");
        StrobeColorDimmer3 wall1Light = new StrobeColorDimmer3("Wall 1");
        StrobeColorDimmer3 wall2Light = new StrobeColorDimmer3("Wall 2");
        StrobeColorDimmer3 wall3Light = new StrobeColorDimmer3("Wall 3");
        StrobeColorDimmer3 wall4Light = new StrobeColorDimmer3("Wall 4");
        StrobeColorDimmer3 wall5Light = new StrobeColorDimmer3("Wall 5");
        StrobeDimmer3 underGeorge = new StrobeDimmer3("ADJ Flash");
        StrobeColorDimmer3 pinSpot = new StrobeColorDimmer3("Pin Spot");

        Controller.Sequence catSeq = new Controller.Sequence();
        Controller.Sequence pumpkinSeq = new Controller.Sequence();
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
        string currentVideoFile;

        string[] videoFiles = new string[]
        {
            "Beauty_Startler_TVHolo_Hor_HD.mp4",
            "FearTheReaper_Door_Horz_HD.mp4",
            "GatheringGhouls_Door_Horz_HD.mp4",
            "Girl_Startler_TVHolo_Hor_HD.mp4",
            "HeadOfHouse_Startler_TVHolo_Hor_HD.mp4",
            "JitteryBones_Door_Horz_HD.mp4",
            "PHA_Poltergeist_StartleScare_Holl_H.mp4",
            "PHA_Siren_StartleScare_Holl_H.mp4",
            "PHA_Spinster_StartleScare_Holl_H.mp4",
            "PHA_Wraith_StartleScare_Holl_H.mp4",
            "PopUpPanic_Door_Horz_HD.mp4",
            "SkeletonSurprise_Door_Horz_HD.mp4",
            "Wraith_Startler_TVHolo_Hor_HD.mp4",
            "JitteryBones_Win_Holo_HD.mp4"
        };

        public Halloween2016(IEnumerable<string> args)
        {
            hoursSmall.AddRange("6:00 pm", "8:30 pm",
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);
            hoursSmall.AddRange("5:00 pm", "9:00 pm",
                DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday);

            //            hoursFull.AddRange("5:00 pm", "7:00 pm");
            hoursFull.Disabled = true;

            // Logging
            hoursSmall.Output.Log("Hours small");
            hoursFull.Output.Log("Hours full");

            string expanderFilesFolder = string.Empty;
            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                {
                    expanderFilesFolder =
                    expanderServer.ExpanderSharedFiles = parts[1];
                }
            }

            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expanderLedmx);      // rpi-eb0092ca
            expanderServer.AddInstance("1583f686014345888c15d7fc9c55ca3c", expanderCat);        // rpi-eb81c94e
            expanderServer.AddInstance("4ea781ef257442edb524493da8f52220", expanderAudio2);     // rpi-eba6cbc7
            expanderServer.AddInstance("76d09e6032d54e77aafec90e1fc4b35b", expanderHifi);       // rpi-eb428ef1
            expanderServer.AddInstance("60023fcde5b549b89fa828d31741dd0c", expanderPicture);    // rpi-eb91bc26
            expanderServer.AddInstance("e41d2977931d4887a9417e8adcd87306", expanderGhost);      // rpi-eb6a047c

            masterVolume.ConnectTo(Exec.MasterVolume);

            hoursSmall
                .ControlsMasterPower(catAir)
                .ControlsMasterPower(catMrsPumpkin);
            hoursFull
                .ControlsMasterPower(catAir)
                .ControlsMasterPower(catMrsPumpkin);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    hoursSmall.SetForced(true);
                else
                    hoursSmall.SetForced(null);
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
                SetPixelColor();
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
                SetPixelColor();
            });

            popOut1.ConnectTo(wall1Light);
            popOut1.ConnectTo(wall4Light);
            popOut2.ConnectTo(wall2Light);
            popOutAll.ConnectTo(wall1Light);
            popOutAll.ConnectTo(wall2Light);
            popOutAll.ConnectTo(wall3Light);
            popOutAll.ConnectTo(wall4Light);
            popOutAll.ConnectTo(wall5Light);
            popOutAll.ConnectTo(underGeorge);
            popOutAll.ConnectTo(pixelsRoofEdge);
            popOutAll.ConnectTo(pinSpot);

            allLights.Add(wall1Light, wall2Light, wall3Light, wall4Light, wall5Light, underGeorge, pixelsRoofEdge, pinSpot, spiderLight,
                spiderWebLights, pumpkinLights, gargoyleLightsEyes);
            purpleLights.Add(wall1Light, wall2Light, wall3Light, wall4Light, wall5Light, pixelsRoofEdge);

            flickerEffect.ConnectTo(stairs1Light);
            flickerEffect.ConnectTo(stairs2Light);
            flickerEffect.ConnectTo(gargoyleLightsEyes);
            flickerEffect.ConnectTo(flyingSkeletonEyes);
            pulsatingGargoyle.ConnectTo(gargoyleLightsCrystal);
            pulsatingGargoyle.ConnectTo(treeSkulls);
            pulsatingGargoyle.ConnectTo(spiderEyes);
            pulsatingEffect1.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(0, 255, 0)));
            pulsatingEffect2.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(255, 0, 0)));

            pulsatingCatLow.ConnectTo(catLights);
            pulsatingCatHigh.ConnectTo(catLights);
            pulsatingPumpkinLow.ConnectTo(pumpkinLights);
            pulsatingPumpkinHigh.ConnectTo(pumpkinLights);
            pulsatingGargoyle.ConnectTo(spiderWebLights);

            stateMachine.For(States.BackgroundSmall)
                .Controls(1, flickerEffect, pulsatingGargoyle, pulsatingCatLow, pulsatingPumpkinLow)
                .Execute(i =>
                    {
                        treeGhosts.SetBrightness(1.0);
                        treeSkulls.SetBrightness(1.0);
                        audio2.SetBackgroundVolume(0.6);

                        var purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                            0.16470588235294117);

                        purpleLights.SetBrightness(purpleColor.Brightness, new Data(
                            Utils.AdditionalData(DataElements.Color, purpleColor.Color),
                            Utils.AdditionalData(DataElements.ColorUltraViolet, 1.0)));

                        i.WaitUntilCancel();
                    })
                .TearDown(instance =>
                    {
                        purpleLights.SetBrightness(0.0);
                        treeGhosts.SetBrightness(0.0);
                        treeSkulls.SetBrightness(0.0);
                    });

            stateMachine.For(States.BackgroundFull)
                .Execute(i =>
                {
                    subVideo.Run();
                    flickerEffect.Start();
                    treeGhosts.SetBrightness(1.0);
                    treeSkulls.SetBrightness(1.0);
                    audioHifi.PlayBackground();
                    audio2.SetBackgroundVolume(0.6);
                    audio2.PlayBackground();

                    var purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                        0.16470588235294117);

                    purpleLights.SetBrightness(purpleColor.Brightness, new Data(DataElements.Color, purpleColor.Color));

                    while (!i.IsCancellationRequested && stateMachine.CurrentState == States.BackgroundFull)
                    {
                        i.WaitFor(S(0.5));
                        if (!this.lastFogRun.HasValue || (DateTime.Now - this.lastFogRun.Value).TotalMinutes > 5)
                        {
                            // Run the fog for a little while
                            fog.Value = true;
                            i.WaitFor(S(4));
                            fog.Value = false;
                            this.lastFogRun = DateTime.Now;
                        }
                    }
                })
                .TearDown(instance =>
                {
                    purpleLights.SetBrightness(0.0);

                    Exec.Cancel(subVideo);
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

            //acnOutput.Connect(new Physical.SmallRGBStrobe(spiderLight, 1), 1);
            //acnOutput.Connect(new Physical.RGBStrobe(wall1Light, 60), 1);
            //acnOutput.Connect(new Physical.RGBStrobe(wall2Light, 70), 1);
            //acnOutput.Connect(new Physical.RGBStrobe(wall3Light, 40), 1);
            //acnOutput.Connect(new Physical.RGBStrobe(wall4Light, 80), 1);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall1Light, 310, 8), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.MarcGamutParH7(wall2Light, 300, 8), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(stairs1Light, 97), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(stairs2Light, 98), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(treeGhosts, 52), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(treeSkulls, 263), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(spiderEyes, 262), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.AmericanDJStrobe(underGeorge, 100), 1);
            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(pinSpot, 20), 1);

            acnOutput.Connect(new Physical.GenericDimmer(catAir, 10), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(catMrsPumpkin, 50), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(catLights, 96), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(spiderWebLights, 99), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(pumpkinLights, 51), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(gargoyleLightsCrystal, 128), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(gargoyleLightsEyes, 129), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(flyingSkeletonEyes, 130), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(hazerFanSpeed, 500), SacnUniverseDMXCat);
            acnOutput.Connect(new Physical.GenericDimmer(hazerHazeOutput, 501), SacnUniverseDMXCat);
            //            acnOutput.Connect(new Physical.RGBIS(testLight1, 260), 1);


            expanderLedmx.DigitalInputs[4].Connect(pumpkinMotion, false);
            expanderCat.DigitalInputs[4].Connect(catMotion, false);
            expanderCat.DigitalInputs[5].Connect(secondBeam);
            expanderCat.DigitalInputs[6].Connect(firstBeam);
            expanderLedmx.DigitalInputs[5].Connect(ghostBeam);
            //raspberryCat.DigitalOutputs[7].Connect(spiderCeilingDrop);
            expanderLedmx.Connect(audio1);
            expanderCat.Connect(audioCat);
            expanderHifi.Connect(audioHifi);
            expanderAudio2.Connect(audio2);
            expanderPicture.Connect(audioPicture);
            //raspberry3dfx.Connect(video3dfx);
            //raspberryVideo2.Connect(video2);
            //raspberryPop.Connect(audioPop);
            //raspberryDIN.Connect(audioDIN);
            //raspberryDIN.DigitalInputs[4].Connect(motion2);
            ////raspberryCat.DigitalOutputs[6].Connect(fog);
            //raspberryDIN.DigitalOutputs[1].Connect(candyEyes);
            //raspberryPop.DigitalOutputs[7].Connect(george1);
            //raspberryPop.DigitalOutputs[6].Connect(george2);
            //raspberryPop.DigitalOutputs[5].Connect(popper);
            //raspberryPop.DigitalOutputs[2].Connect(dropSpiderEyes);





            catMotion.Output.Subscribe(x =>
            {
                if (x && (hoursFull.IsOpen || hoursSmall.IsOpen) && !blockCat.Value)
                    Executor.Current.Execute(catSeq);

                oscServer.SendAllClients("/1/led1", x ? 1 : 0);
            });

            pumpkinMotion.Output.Subscribe(x =>
            {
                if (x && (hoursFull.IsOpen || hoursSmall.IsOpen))
                    Executor.Current.Execute(pumpkinSeq);

                //                oscServer.SendAllClients("/1/led1", x ? 1 : 0);
            });

            firstBeam.Output.Subscribe(x =>
            {
                UpdateOSC();

                //if (x)
                //    audio2.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav");

                if (x && /*hoursFull.IsOpen &&*/ !emergencyStop.Value && !blockMaster.Value)
                    subFirst.Run();
            });

            secondBeam.Output.Subscribe(x =>
            {
                UpdateOSC();

                if (x && hoursFull.IsOpen && !emergencyStop.Value && !blockMaster.Value)
                    subPicture.Run();
            });

            ghostBeam.Output.Subscribe(x =>
            {
                UpdateOSC();

                if (x && hoursFull.IsOpen && !emergencyStop.Value && !blockMaster.Value)
                    subGhost.Run();
            });

            motion2.Output.Subscribe(x =>
            {
                //if (x && hoursFull.IsOpen)
                //    Executor.Current.Execute(motionSeq);

                oscServer.SendAllClients("/1/led4", x ? 1 : 0);
            });

            welcomeSeq.WhenExecuted
                .Execute(i =>
                {
                    audioPop.PlayEffect("100471__robinhood76__01886-welcome-spell.wav");

                    i.WaitFor(S(3));
                });

            subFirst
                .LockWhenRunning(10, flyingSkeletonEyes)
                .RunAction(i =>
                {
                    flyingSkeletonEyes.SetBrightness(1.0, i.Token);

                    //                    audioPicture.PlayEffect("162 Blood Curdling Scream of Terror.wav");
                    audioPicture.PlayEffect("05 I'm a Little Teapot.wav", 0.3);

                    i.WaitFor(S(16.0));
                })
                .TearDown(() =>
                {
                    pulsatingEffect2.Stop();
                    Thread.Sleep(S(5));
                });

            subPicture
                .RunAction(i =>
                {
                    expanderPicture.SendSerial(0, new byte[] { 0x02 });
                    i.WaitFor(S(10.0));
                })
                .TearDown(() =>
                {
                });

            subGhost
                .RunAction(i =>
                {
                    expanderLedmx.SendSerial(0, new byte[] { 0x01 });
                    i.WaitFor(S(10.0));
                })
                .TearDown(() =>
                {
                });

            subVideo
                .RunAction(i =>
                {
                    while (!i.IsCancellationRequested)
                    {
                        string videoFile;
                        while (true)
                        {
                            videoFile = videoFiles[random.Next(videoFiles.Length)];
                            if (videoFiles.Length == 1 || videoFile != currentVideoFile)
                                break;
                        }

                        currentVideoFile = videoFile;
                        video2.PlayVideo(videoFile);
                        i.WaitFor(S(60));
                    }
                });

            subFinal
                .LockWhenRunning(10, candyEyes, underGeorge)
                .RunAction(i =>
                {
                    pulsatingEffect2.Stop();
                    pulsatingEffect1.Start();
                    candyEyes.Value = true;
                    underGeorge.SetStrobeSpeed(0.5, i.Token);
                    underGeorge.SetBrightness(1.0, i.Token);
                    audioPop.PlayEffect("laugh.wav", 1.0, 0.0);
                    for (int r = 0; r < 2; r++)
                    {
                        george1.Value = true;
                        i.WaitFor(S(0.2));
                        george1.Value = false;
                        i.WaitFor(S(0.2));
                    }
                    i.WaitFor(S(1));
                    audioDIN.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav");
                    underGeorge.SetStrobeSpeed(0.0, i.Token);
                    underGeorge.SetBrightness(0.0, i.Token);
                    i.WaitFor(S(1));

                    popper.Value = true;
                    audioPop.PlayEffect("180 Babbling Lunatic.wav", 0.0, 1.0);
                    i.WaitFor(S(3));
                    popper.Value = false;

                    i.WaitFor(S(8));

                    Exec.MasterEffect.Fade(underGeorge, 0.5, 0.0, 1000, token: i.Token);
                    //                    underGeorge.SetBrightness(0.3, i.Token);
                    i.WaitFor(S(0.5));
                    george2.Value = true;
                    i.WaitFor(S(1.0));
                    george2.Value = false;
                    i.WaitFor(S(1.0));
                    //                    underGeorge.SetBrightness(0.0, i.Token);
                })
                .TearDown(() =>
                {
                    candyEyes.Value = false;
                    pulsatingEffect1.Stop();
                    Thread.Sleep(S(5));
                });


            catSeq.WhenExecuted
                .Execute(instance =>
                {
                    var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                    pulsatingCatLow.Stop();
                    pulsatingCatHigh.Start();
                    //                catLights.SetBrightness(1.0, instance.Token);

                    while (true)
                    {
                        switch (random.Next(4))
                        {
                            case 0:
                                audioCat.PlayEffect("266 Monster Growl 7.wav", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.0));
                                break;
                            case 1:
                                audioCat.PlayEffect("285 Monster Snarl 2.wav", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                            case 2:
                                audioCat.PlayEffect("286 Monster Snarl 3.wav", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.5));
                                break;
                            case 3:
                                audioCat.PlayEffect("287 Monster Snarl 4.wav", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(1.5));
                                break;
                            default:
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                        }

                        instance.CancelToken.ThrowIfCancellationRequested();

                        if (maxRuntime.Elapsed.TotalSeconds > 10)
                            break;
                    }
                })
                .TearDown(instance =>
                {
                    //                Exec.MasterEffect.Fade(catLights, 1.0, 0.0, 1000, token: instance.Token);
                    //TODO: Fade out
                    pulsatingCatHigh.Stop();
                    pulsatingCatLow.Start();
                });

            pumpkinSeq.WhenExecuted
                .Execute(instance =>
                {
                    pulsatingPumpkinLow.Stop();
                    pulsatingPumpkinHigh.Start();
                    audio1.PlayEffect("Thriller2.wav");
                    instance.CancelToken.WaitHandle.WaitOne(40000);
                })
                .TearDown(instance =>
                {
                    pulsatingPumpkinHigh.Stop();
                    pulsatingPumpkinLow.Start();
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

            faderR.WhenOutputChanges(v => { SetPixelColor(); });
            faderG.WhenOutputChanges(v => { SetPixelColor(); });
            faderB.WhenOutputChanges(v => { SetPixelColor(); });
            faderBright.WhenOutputChanges(v => { SetPixelColor(); });

            ConfigureOSC();
            ConfigureMIDI();
        }

        Color GetFaderColor()
        {
            return HSV.ColorFromRGB(faderR.Value, faderG.Value, faderB.Value);
        }

        void SetPixelColor()
        {
            if (manualFader.Value)
            {
                pixelsRoofEdge.SetColor(GetFaderColor(), faderBright.Value);
                //                wall5Light.SetColor(GetFaderColor(), faderBright.Value);
            }
            else
                pixelsRoofEdge.SetColor(Color.Black);
        }

        void UpdateOSC()
        {
            oscServer.SendAllClients("/Beams/x",
                firstBeam.Value ? 1 : 0,
                secondBeam.Value ? 1 : 0,
                ghostBeam.Value ? 1 : 0,
                0);
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
            SetPixelColor();
        }

        public override void Stop()
        {
            audioHifi.PauseBackground();
            audio2.PauseBackground();
        }
    }
}
