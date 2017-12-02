using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Physical = Animatroller.Framework.PhysicalDevice;
using Effect = Animatroller.Framework.Effect;
using Import = Animatroller.Framework.Import;
using System.IO;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes
{
    internal class Xmas2015 : BaseScene
    {
        const int SacnUniverseDMX = 1;
        const int SacnUniverseRenardBig = 20;
        const int SacnUniverseRenardSmall = 21;
        const int SacnUniverse5 = 5;
        const int SacnUniverse6 = 6;
        const int SacnUniverse10 = 10;
        const int SacnUniverse11 = 11;
        const int SacnUniverse12 = 12;

        const int midiChannel = 0;

        public enum States
        {
            Background,
            Music1,
            Music2,
            SantaVideo,
            DarthVader
        }

        OperatingHours2 hours = new OperatingHours2();
        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>(States.Background);

        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander1 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander2 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander3 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander4 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8899);
        AudioPlayer audio1 = new AudioPlayer();
        AudioPlayer audioMain = new AudioPlayer();
        AudioPlayer audioDarthVader = new AudioPlayer();
        VideoPlayer video3 = new VideoPlayer();

        Expander.AcnStream acnOutput = new Expander.AcnStream(priority: 150);
        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingEffect3 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingEffect4 = new Effect.Pulsating(S(2), 0.1, 1.0, false);

        DigitalInput2 inOlaf = new DigitalInput2();
        DigitalInput2 inR2D2 = new DigitalInput2();
        DigitalInput2 inBlueButton = new DigitalInput2();
        DigitalInput2 inRedButton = new DigitalInput2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 inShowMachine = new DigitalInput2();
        DigitalInput2 in2 = new DigitalInput2();
        DigitalInput2 in3 = new DigitalInput2();
        DigitalOutput2 out1 = new DigitalOutput2();

        DigitalOutput2 laser = new DigitalOutput2();
        DigitalOutput2 airR2D2 = new DigitalOutput2();
        DigitalOutput2 airSanta1 = new DigitalOutput2();
        DigitalOutput2 airOlaf = new DigitalOutput2();
        DigitalOutput2 airReindeer = new DigitalOutput2();
        DigitalOutput2 snowMachine = new DigitalOutput2();

        Dimmer3 lightNet1 = new Dimmer3();
        Dimmer3 lightNet2 = new Dimmer3();
        Dimmer3 lightNet3 = new Dimmer3();
        Dimmer3 lightNet4 = new Dimmer3();
        Dimmer3 lightNet5 = new Dimmer3();
        Dimmer3 lightNet6 = new Dimmer3();
        Dimmer3 lightNet7 = new Dimmer3();
        Dimmer3 lightNet8 = new Dimmer3();
        Dimmer3 lightTopper1 = new Dimmer3();
        Dimmer3 lightTopper2 = new Dimmer3();
        Dimmer3 lightXmasTree = new Dimmer3();
        Dimmer3 lightStairs1 = new Dimmer3();
        Dimmer3 lightStairs2 = new Dimmer3();
        Dimmer3 lightRail1 = new Dimmer3();
        Dimmer3 lightRail2 = new Dimmer3();

        Dimmer3 lightSanta = new Dimmer3();
        Dimmer3 lightSnowman = new Dimmer3();

        Dimmer3 lightHat1 = new Dimmer3();
        Dimmer3 lightHat2 = new Dimmer3();
        Dimmer3 lightHat3 = new Dimmer3();
        Dimmer3 lightHat4 = new Dimmer3();
        Dimmer3 lightReindeer1 = new Dimmer3();
        Dimmer3 lightReindeer2 = new Dimmer3();
        Dimmer3 lightBlueButton = new Dimmer3();
        Dimmer3 lightRedButton = new Dimmer3();
        //        StrobeColorDimmer3 spiderLight = new StrobeColorDimmer3("Spider");
        StrobeColorDimmer3 lightVader = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightWall1 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightWall2 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightWall3 = new StrobeColorDimmer3();

        Dimmer3 lightOlaf = new Dimmer3();
        Dimmer3 lightR2D2 = new Dimmer3();
        VirtualPixel1D3 pixelsRoofEdge = new VirtualPixel1D3(150);
        VirtualPixel2D3 pixelsMatrix = new VirtualPixel2D3(20, 10);
        VirtualPixel1D3 saberPixels = new VirtualPixel1D3(32);
        Expander.MidiInput2 midiAkai = new Expander.MidiInput2("LPD8", true);
        Subject<bool> inflatablesRunning = new Subject<bool>();
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();
        DigitalInput2 buttonStartInflatables = new DigitalInput2();

        Import.LorImport2 lorFeelTheLight = new Import.LorImport2();
        Import.LorImport2 lorBelieve = new Import.LorImport2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        Controller.Subroutine subCandyCane = new Controller.Subroutine();
        Controller.Subroutine subStarWarsCane = new Controller.Subroutine();
        Controller.Subroutine subBackground = new Controller.Subroutine();
        Controller.Subroutine subSantaVideo = new Controller.Subroutine();
        Controller.Subroutine subMusic1 = new Controller.Subroutine();
        Controller.Subroutine subMusic2 = new Controller.Subroutine();
        Controller.Subroutine subOlaf = new Controller.Subroutine();
        Controller.Subroutine subR2D2 = new Controller.Subroutine();
        Controller.Subroutine subStarWars = new Controller.Subroutine();
        Controller.Subroutine subSnow = new Controller.Subroutine();

        Import.DmxPlayback dmxPlayback = new Import.DmxPlayback();

        public Xmas2015(IEnumerable<string> args)
        {
            hours.AddRange("4:00 pm", "10:00 pm");

            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                {
                    Exec.ExpanderSharedFiles = parts[1];
                }
            }

            pulsatingEffect1.ConnectTo(lightOlaf);
            pulsatingEffect2.ConnectTo(lightR2D2);
            pulsatingEffect3.ConnectTo(pixelsRoofEdge, Utils.Data(Color.Red));
            pulsatingEffect4.ConnectTo(lightBlueButton);
            pulsatingEffect4.ConnectTo(lightRedButton);

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expander1);      // rpi-eb0092ca
            expanderServer.AddInstance("10520fdcf14d47cba31da8b6e05d01d8", expander2);      // rpi-eb428ef1
            expanderServer.AddInstance("59ebb8e925c94182a0f6e0ef09180200", expander3);      // rpi-eba6cbc7
            expanderServer.AddInstance("1583f686014345888c15d7fc9c55ca3c", expander4);      // rpi-eb81c94e

            expander1.DigitalInputs[5].Connect(inR2D2);
            expander1.DigitalInputs[4].Connect(inOlaf);
            expander1.DigitalInputs[6].Connect(inShowMachine);
            expander1.DigitalOutputs[7].Connect(out1);
            expander4.DigitalOutputs[7].Connect(snowMachine);

            expander4.DigitalInputs[5].Connect(inBlueButton);
            expander4.DigitalInputs[4].Connect(inRedButton);

            expander1.Connect(audio1);
            expander2.Connect(audioMain);
            expander3.Connect(video3);
            expander4.Connect(audioDarthVader);

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            midiAkai.Controller(midiChannel, 1).Subscribe(x => blackOut.Value = x.Value);
            midiAkai.Controller(midiChannel, 2).Subscribe(x => whiteOut.Value = x.Value);

            dmxPlayback.Load(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "XmasLoop.bin"), 15);
            dmxPlayback.Loop = true;

            var pixelMapping = Framework.Utility.PixelMapping.GeneratePixelMappingFromGlediatorPatch(
                Path.Combine(Exec.ExpanderSharedFiles, "Glediator", "ArtNet 14-15 20x10.patch.glediator"));
            dmxPlayback.SetOutput(pixelsMatrix, pixelMapping);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    hours.SetForced(true);
                else
                    hours.SetForced(null);
            });

            inflatablesRunning.Subscribe(x =>
            {
                airReindeer.SetValue(x);

                Exec.SetKey("InflatablesRunning", x.ToString());
            });

            // Read from storage
            inflatablesRunning.OnNext(Exec.GetSetKey("InflatablesRunning", false));

            //            hours.Output.Log("Hours inside");

            stateMachine.ForFromSubroutine(States.Background, subBackground);
            stateMachine.ForFromSubroutine(States.Music1, subMusic1);
            stateMachine.ForFromSubroutine(States.Music2, subMusic2);
            stateMachine.ForFromSubroutine(States.SantaVideo, subSantaVideo);
            stateMachine.ForFromSubroutine(States.DarthVader, subStarWars);

            airR2D2.SetValue(true);
            airSanta1.SetValue(true);
            airOlaf.SetValue(true);
            laser.SetValue(true);

            hours
            //    .ControlsMasterPower(packages)
            //    .ControlsMasterPower(airSnowman)
                .ControlsMasterPower(airOlaf)
                .ControlsMasterPower(laser)
                .ControlsMasterPower(airSanta1)
                .ControlsMasterPower(airR2D2);
            //    .ControlsMasterPower(airSanta);

            buttonStartInflatables.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen)
                {
                    inflatablesRunning.OnNext(true);
                }
            });

            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 0, 50), SacnUniverse6, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 50, 100), SacnUniverse5, 1);

            var pixelMapping2D = Framework.Utility.PixelMapping.GeneratePixelMapping(20, 10, pixelOrder: Framework.Utility.PixelOrder.HorizontalSnakeBottomLeft);
            acnOutput.Connect(new Physical.Pixel2D(pixelsMatrix, pixelMapping2D), SacnUniverse10, 1);

            acnOutput.Connect(new Physical.Pixel1D(saberPixels, 0, 32), SacnUniverse12, 1);

            acnOutput.Connect(new Physical.GenericDimmer(airOlaf, 10), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airReindeer, 12), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airR2D2, 11), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 96), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 97), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 98), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 99), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightVader, 60), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightWall1, 70), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightWall2, 40), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightWall3, 80), SacnUniverseDMX);

            acnOutput.Connect(new Physical.GenericDimmer(laser, 4), SacnUniverseRenardBig);

            acnOutput.Connect(new Physical.GenericDimmer(lightOlaf, 128), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightSanta, 131), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightSnowman, 132), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightSnowman, 132), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightBlueButton, 262), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightRedButton, 263), SacnUniverseDMX);

            acnOutput.Connect(new Physical.GenericDimmer(lightR2D2, 16), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail2, 10), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 11), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 19), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(airSanta1, 20), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 22), SacnUniverseRenardBig);

            acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 25), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightXmasTree, 26), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightReindeer2, 29), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightReindeer1, 32), SacnUniverseRenardBig);

            acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 1), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 2), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet1, 3), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet3, 4), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet4, 5), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail1, 6), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightTopper1, 7), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightTopper2, 8), SacnUniverseRenardSmall);

            hours.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.GoToDefaultState();
                }
                else
                {
                    //if (buttonOverrideHours.Active)
                    //    return;

                    stateMachine.GoToIdle();

                    // Needed?
                    System.Threading.Thread.Sleep(200);

                    inflatablesRunning.OnNext(false);
                }
            });

            subBackground
                .LockWhenRunning(
                    lightNet1,
                    lightNet2,
                    lightNet3,
                    lightNet4,
                    lightNet5,
                    lightNet6,
                    lightNet7,
                    lightNet8,
                    lightTopper1,
                    lightTopper2,
                    lightXmasTree,
                    lightStairs1,
                    lightStairs2,
                    lightRail1,
                    lightRail2,
                    lightSanta,
                    lightSnowman,
                    lightHat1,
                    lightHat2,
                    lightHat3,
                    lightHat4,
                    lightReindeer1,
                    lightReindeer2,
                    lightVader,
                    lightWall1,
                    lightWall2,
                    lightWall3,
                    lightR2D2,
                    lightOlaf,
                    saberPixels)
                .RunAction(i =>
                {
                    pulsatingEffect4.Start();
                    lightR2D2.SetBrightness(1, token: i.Token);
                    lightOlaf.SetBrightness(1, token: i.Token);
                    lightNet1.SetBrightness(1, token: i.Token);
                    lightNet2.SetBrightness(1, token: i.Token);
                    lightNet3.SetBrightness(1, token: i.Token);
                    lightNet4.SetBrightness(1, token: i.Token);
                    lightNet5.SetBrightness(1, token: i.Token);
                    lightNet6.SetBrightness(1, token: i.Token);
                    lightNet7.SetBrightness(1, token: i.Token);
                    lightNet8.SetBrightness(1, token: i.Token);
                    lightTopper1.SetBrightness(1, token: i.Token);
                    lightTopper2.SetBrightness(1, token: i.Token);
                    lightXmasTree.SetBrightness(1, token: i.Token);
                    lightStairs1.SetBrightness(1, token: i.Token);
                    lightStairs2.SetBrightness(1, token: i.Token);
                    lightRail1.SetBrightness(1, token: i.Token);
                    lightRail2.SetBrightness(1, token: i.Token);
                    lightSanta.SetBrightness(1, token: i.Token);
                    lightSnowman.SetBrightness(1, token: i.Token);
                    lightHat1.SetBrightness(1, token: i.Token);
                    lightHat2.SetBrightness(1, token: i.Token);
                    lightHat3.SetBrightness(1, token: i.Token);
                    lightHat4.SetBrightness(1, token: i.Token);
                    lightReindeer1.SetBrightness(1, token: i.Token);
                    lightReindeer2.SetBrightness(1, token: i.Token);
                    lightVader.SetColor(Color.Red, 1, token: i.Token);
                    lightWall1.SetColor(Color.Red, 1, token: i.Token);
                    lightWall2.SetColor(Color.Red, 1, token: i.Token);
                    lightWall3.SetColor(Color.Red, 1, token: i.Token);

                    saberPixels.SetColor(Color.Red, 0.4, token: i.Token);

                    subCandyCane.Run();
                    dmxPlayback.Run();

                    i.WaitUntilCancel();

                    dmxPlayback.Stop();
                    Exec.Cancel(subCandyCane);
                    pulsatingEffect4.Stop();
                });

            subCandyCane
                .LockWhenRunning(pixelsRoofEdge)
                .RunAction(i =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int x = 0; x < spacing; x++)
                        {
                            pixelsRoofEdge.Inject((x % spacing) == 0 ? Color.Red : Color.White, 0.5, token: i.Token);

                            i.WaitFor(S(0.30), true);
                        }
                    }
                });

            subSnow
                .RunAction(ins =>
                {
                    snowMachine.SetValue(true);

                    ins.WaitFor(S(30));
                })
                .TearDown(i =>
                {
                    snowMachine.SetValue(false);
                });

            subStarWarsCane
                .LockWhenRunning(
                    pixelsRoofEdge,
                    pixelsMatrix)
                .RunAction(instance =>
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
                                    pixelsRoofEdge.InjectRev(Color.Yellow, 1.0, token: instance.Token);
                                    break;
                                case 2:
                                case 3:
                                    pixelsRoofEdge.InjectRev(Color.Orange, 0.2, token: instance.Token);
                                    break;
                            }

                            instance.WaitFor(S(0.1));

                            if (instance.IsCancellationRequested)
                                break;
                        }
                    }
                });

            subMusic1
                .RunAction(ins =>
                    {
                        audioMain.PlayTrack("08 Feel the Light.wav");
                        ins.WaitFor(S(240));
                    }).TearDown(i =>
                    {
                        lorFeelTheLight.Stop();
                        audioMain.PauseTrack();
                    });

            subMusic2
                .RunAction(ins =>
                {
                    snowMachine.SetValue(true);
                    audioMain.PlayTrack("T.P.E. - 04 - Josh Groban - Believe.flac");
                    ins.WaitFor(S(260));
                }).TearDown(i =>
                {
                    snowMachine.SetValue(false);
                    lorBelieve.Stop();
                    audioMain.PauseTrack();
                });

            subSantaVideo
                .RunAction(i =>
                {
                    pulsatingEffect3.Start();
                    switch (random.Next(6))
                    {
                        case 0:
                            video3.PlayVideo("NBC_DeckTheHalls_Holl_H.mp4");
                            break;

                        case 1:
                            video3.PlayVideo("NBC_AllThruHouse_Part1_Holl_H.mp4");
                            break;

                        case 2:
                            video3.PlayVideo("NBC_AllThruHouse_Part2_Holl_H.mp4");
                            break;

                        case 3:
                            video3.PlayVideo("NBC_AllThruHouse_Part3_Holl_H.mp4");
                            break;

                        case 4:
                            video3.PlayVideo("NBC_JingleBells_Holl_H.mp4");
                            break;

                        case 5:
                            video3.PlayVideo("NBC_WeWishYou_Holl_H.mp4");
                            break;
                    }

                    i.WaitFor(S(120), false);
                    pulsatingEffect3.Stop();
                });

            subOlaf
                .RunAction(i =>
                {
                    pulsatingEffect1.Start();
                    audio1.PlayNewEffect("WarmHugs.wav", 0.0, 1.0);
                    i.WaitFor(S(10));
                    pulsatingEffect1.Stop();
                });

            subR2D2
                .RunAction(i =>
                {
                    pulsatingEffect2.Start();
                    audio1.PlayNewEffect("Im C3PO.wav", 1.0, 0.0);
                    i.WaitFor(S(4));
                    audio1.PlayNewEffect("Processing R2D2.wav", 0.5, 0.0);
                    i.WaitFor(S(5));
                    pulsatingEffect2.Stop();
                });

            subStarWars
                .LockWhenRunning(
                    saberPixels,
                    lightVader,
                    lightR2D2)
                .RunAction(instance =>
                {
                    //Exec.Cancel(subCandyCane);
                    subStarWarsCane.Run();
                    lightR2D2.SetBrightness(1.0, token: instance.Token);

                    audioMain.PlayTrack("01. Star Wars - Main Title.wav");

                    instance.WaitFor(S(16));

                    /*
                        elJesus.SetPower(true);
                        pulsatingStar.Start();
                        lightJesus.SetColor(Color.White, 0.3);
                        light3wise.SetOnlyColor(Color.LightYellow);
                        light3wise.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));*/

                    Exec.MasterEffect.Fade(lightVader, 0.0, 1.0, 1000, token: instance.Token, additionalData: Utils.Data(Color.Red));
                    instance.WaitFor(S(2.5));

                    Exec.Cancel(subStarWarsCane);
                    instance.WaitFor(S(0.5));

                    audioDarthVader.PlayEffect("saberon.wav");
                    for (int sab = 00; sab < 32; sab++)
                    {
                        saberPixels.Inject(Color.Red, 0.5, token: instance.Token);
                        instance.WaitFor(S(0.01));
                    }
                    instance.WaitFor(S(1));
                    audioMain.PauseTrack();

                    lightVader.SetColor(Color.Red, 1.0, token: instance.Token);
                    audioDarthVader.PlayEffect("father.wav");
                    instance.WaitFor(S(5));

                    lightVader.SetBrightness(0.0, token: instance.Token);
                    //light3wise.TurnOff();
                    //lightJesus.TurnOff();
                    //pulsatingStar.Stop();
                    //elJesus.TurnOff();

                    audioDarthVader.PlayEffect("force1.wav");
                    instance.WaitFor(S(4));

                    audioDarthVader.PlayEffect("saberoff.wav");
                    instance.WaitFor(S(0.7));
                    for (int sab = 0; sab < 16; sab++)
                    {
                        saberPixels.InjectRev(Color.Black, 0, token: instance.Token);
                        saberPixels.InjectRev(Color.Black, 0, token: instance.Token);
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
                .TearDown(i =>
                {
                    audioMain.PauseTrack();
                });


            midiAkai.Note(midiChannel, 36).Subscribe(x =>
            {
                if (x)
                    subOlaf.Run();
            });

            midiAkai.Note(midiChannel, 37).Subscribe(x =>
            {
                if (x)
                    subR2D2.Run();
            });

            midiAkai.Note(midiChannel, 38).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.Music1);
                //                    audio2.PlayTrack("08 Feel the Light.wav");
            });

            midiAkai.Note(midiChannel, 39).Subscribe(x =>
            {
                if (x)
                {
                    lorFeelTheLight.Stop();
                    audioMain.PauseTrack();
                }
            });

            midiAkai.Note(midiChannel, 40).Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.GoToState(States.SantaVideo);
                }
            });

            inOlaf.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen)
                    subOlaf.Run();
            });

            inR2D2.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen)
                    subR2D2.Run();
            });

            inRedButton.Output.Subscribe(x =>
            {
                if (x)
                {
                    if (hours.IsOpen)
                    {
                        if (stateMachine.CurrentState == States.Background)
                            stateMachine.GoToState(States.DarthVader);
                    }
                    else
                        audioDarthVader.PlayEffect("darthvader_lackoffaith.wav");
                }
            });

            inBlueButton.Output.Subscribe(x =>
            {
                if (x)
                {
                    if (hours.IsOpen)
                    {
                        subSnow.Run();
                        if (stateMachine.CurrentState == States.Background)
                            stateMachine.GoToState(States.Music2);
                    }
                    else
                        audioDarthVader.PlayEffect("darkside.wav");
                }
            });

            audioMain.AudioTrackStart += (o, e) =>
            {
                switch (e.FileName)
                {
                    case "08 Feel the Light.wav":
                        lorFeelTheLight.Start(27830);
                        break;

                    case "T.P.E. - 04 - Josh Groban - Believe.flac":
                        lorBelieve.Start();
                        break;
                }
            };

            audioMain.AudioTrackDone += (o, e) =>
            {
                //                Thread.Sleep(5000);
                //    audio2.PlayTrack("08 Feel the Light.wav");
            };

            inShowMachine.Output.Subscribe(x =>
            {
                snowMachine.SetValue(x);
                //                lightRedButton.SetBrightness(x ? 1.0 : 0.0);
                //if (x)
                //    stateMachine.GoToState(States.Music1);
            });

            in2.Output.Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToIdle();
            });

            in3.Output.Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToDefaultState();
            });

            ImportAndMapFeelTheLight();
            ImportAndMapBelieve();
        }

        private void ImportAndMapFeelTheLight()
        {
            lorFeelTheLight.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Feel The Light, Jennifer Lopez.lms"));

            lorFeelTheLight.Progress.Subscribe(x =>
            {
                this.log.Verbose("Feel the Light {0:N0} ms", x);
            });

            //            lorFeelTheLight.Dump();

            lorFeelTheLight.MapDevice("Unit 01.7 arch 1.7", lightNet8);
            lorFeelTheLight.MapDevice("Unit 01.8 arch 2.1", lightNet7);
            lorFeelTheLight.MapDevice("Unit 01.9 arch 2.2", lightNet6);
            lorFeelTheLight.MapDevice("Unit 01.10 arch 2.3", lightNet5);
            lorFeelTheLight.MapDevice("Unit 01.11 arch 2.4", lightNet4);
            lorFeelTheLight.MapDevice("Unit 01.12 arch 2.5", lightNet3);
            lorFeelTheLight.MapDevice("Unit 01.13arch 2.6", lightNet2);
            lorFeelTheLight.MapDevice("Unit 01.14 arch 2.7", lightNet1);

            lorFeelTheLight.MapDevice("windows 01", lightStairs1);
            lorFeelTheLight.MapDevice("windows 02", lightStairs2);

            lorFeelTheLight.MapDevice("04.01 Sing tree outline", lightHat1);
            lorFeelTheLight.MapDevice("04.09  Sing tree outline", lightHat2);
            lorFeelTheLight.MapDevice("05.01 Sing tree outline", lightHat3);
            lorFeelTheLight.MapDevice("05.09 Sing tree Outline", lightHat4);

            lorFeelTheLight.MapDevice("03.15 candy cane lane", lightTopper1);
            lorFeelTheLight.MapDevice("03.13 deer rudolf", lightTopper2);
            lorFeelTheLight.MapDevice("03.10 house eve 01", lightRail1);
            lorFeelTheLight.MapDevice("03.11 house eve 02", lightRail2);
            lorFeelTheLight.MapDevice("03.12 house eve 03", lightReindeer1);
            lorFeelTheLight.MapDevice("03.14 deer 02", lightReindeer2);

            lorFeelTheLight.MapDevice("03.9 mini tree 08", lightWall1, Utils.Data(Color.Red));
            lorFeelTheLight.MapDevice("03.8 mini tree 07", lightWall2, Utils.Data(Color.Red));
            lorFeelTheLight.MapDevice("03.7 mini tree 06", lightWall3, Utils.Data(Color.Red));
            lorFeelTheLight.MapDevice("03.6 mini tree 05", lightSanta);
            lorFeelTheLight.MapDevice("03.5 mini tree 04", lightSnowman);
            lorFeelTheLight.MapDevice("03.4 mini tree 03", lightVader, Utils.Data(Color.Red));
            lorFeelTheLight.MapDevice("03.3 mini tree 02",
                new VirtualDevice(b => saberPixels.SetColorRange(Color.Red, b, 0, 32, token: lorFeelTheLight.Token)));

            lorFeelTheLight.ControlDevice(pixelsMatrix);
            lorFeelTheLight.ControlDevice(saberPixels);
            lorFeelTheLight.MapDevice("Unit 02.1 Mega tree 1",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 0, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.2 Mega tree 2",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 1, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.3 Mege tree 3",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 2, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.4 Mega tree 4",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 3, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.9 Mega tree 9",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 4, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.10 Mega tree 10",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 5, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.11 Mega tree 11",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 6, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.12 Mega tree 12",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 7, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.15 Mega tree 15",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 8, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("Unit 02.16 Mega tree 16",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 9, 20, 1, token: lorFeelTheLight.Token)));
            lorFeelTheLight.MapDevice("03.1 mega tree topper 01", pixelsRoofEdge, Utils.Data(Color.White));

            lorFeelTheLight.Prepare();
        }

        private void ImportAndMapBelieve()
        {
            lorBelieve.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Believe - Josh Groban 64 chns.lms"));

            lorBelieve.Progress.Subscribe(x =>
            {
                this.log.Verbose("Believe {0:N0} ms", x);
            });

            //            lorBelieve.Dump();

            lorBelieve.MapDevice("Yard 1", lightNet1);
            lorBelieve.MapDevice("Yard 2", lightNet2);
            lorBelieve.MapDevice("Yard 3", lightNet3);
            lorBelieve.MapDevice("Yard 4", lightNet4);
            lorBelieve.MapDevice("Yard 5", lightNet5);
            lorBelieve.MapDevice("Yard 6", lightNet6);
            lorBelieve.MapDevice("Yard 7", lightNet7);
            lorBelieve.MapDevice("Yard 8", lightNet8);
            lorBelieve.MapDevice("Yard 9", lightHat1);
            lorBelieve.MapDevice("Yard 10", lightHat2);
            lorBelieve.MapDevice("Yard 7", lightHat3);
            lorBelieve.MapDevice("Yard 8", lightHat4);

            lorBelieve.MapDevice("House 1", lightR2D2);
            lorBelieve.MapDevice("House 2", lightOlaf);

            lorBelieve.MapDevice("Wreath W", lightStairs1);
            lorBelieve.MapDevice("Wreath R", lightStairs2);
            lorBelieve.MapDevice("Mega Star", lightXmasTree);

            lorBelieve.MapDevice("Floods B", lightWall1, Utils.Data(Color.Blue));
            lorBelieve.MapDevice("Floods G", lightWall2, Utils.Data(Color.Green));
            lorBelieve.MapDevice("Floods R", lightWall3, Utils.Data(Color.Red));

            lorBelieve.MapDevice("Ferris Wheel 1", lightTopper1);
            lorBelieve.MapDevice("Ferris Wheel 2", lightTopper2);
            lorBelieve.MapDevice("Ferris Wheel 3", lightRail1);
            lorBelieve.MapDevice("Ferris Wheel 4", lightRail2);
            lorBelieve.MapDevice("Ferris Wheel 5", lightReindeer1);
            lorBelieve.MapDevice("Ferris Wheel 6", lightReindeer2);
            lorBelieve.MapDevice("Ferris Wheel 7", lightSanta);
            lorBelieve.MapDevice("Ferris Wheel 8", lightSnowman);

            lorBelieve.MapDevice("NATIVITY", lightVader, Utils.Data(Color.Red));
            lorBelieve.MapDevice("House 3",
                new VirtualDevice(b => saberPixels.SetColorRange(Color.Red, b, 0, 32, token: lorBelieve.Token)));

            lorBelieve.ControlDevice(pixelsMatrix);
            lorBelieve.ControlDevice(saberPixels);
            lorBelieve.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 0, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 1, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 2, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 3, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 4, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 5, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 6, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 7, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 8, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 9, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Star", pixelsRoofEdge, Utils.Data(Color.Red));

            lorBelieve.Prepare();
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
