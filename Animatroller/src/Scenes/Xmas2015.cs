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

namespace Animatroller.SceneRunner
{
    internal class Xmas2015 : BaseScene
    {
        const int SacnUniverseDMX = 1;
        const int SacnUniverseRenardBig = 20;
        const int SacnUniverseRenardSmall = 21;
        const int midiChannel = 0;

        public enum States
        {
            Background,
            Music1,
            SantaVideo,
            DarthVader
        }

        OperatingHours2 hours = new OperatingHours2();
        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();

        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander1 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander2 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander3 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8088, lighthousePort: 8899);
        AudioPlayer audio1 = new AudioPlayer();
        AudioPlayer audio2 = new AudioPlayer();
        AudioPlayer audioDarthVader = new AudioPlayer();
        VideoPlayer video3 = new VideoPlayer();

        Expander.AcnStream acnOutput = new Expander.AcnStream(priority: 150);
        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingEffect3 = new Effect.Pulsating(S(2), 0.1, 1.0, false);

        DigitalInput2 inOlaf = new DigitalInput2();
        DigitalInput2 inR2D2 = new DigitalInput2();

        DigitalInput2 in1 = new DigitalInput2();
        DigitalOutput2 out1 = new DigitalOutput2();

        DigitalOutput2 laser = new DigitalOutput2();
        DigitalOutput2 airR2D2 = new DigitalOutput2();
        DigitalOutput2 airSanta1 = new DigitalOutput2();
        DigitalOutput2 airOlaf = new DigitalOutput2();
        DigitalOutput2 airReindeer = new DigitalOutput2();

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
        //        StrobeColorDimmer3 spiderLight = new StrobeColorDimmer3("Spider");
        StrobeColorDimmer3 lightVader = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightWall1 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightWall2 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightWall3 = new StrobeColorDimmer3();

        Dimmer3 lightOlaf = new Dimmer3();
        Dimmer3 lightR2D2 = new Dimmer3();
        VirtualPixel1D2 pixelsRoofEdge = new VirtualPixel1D2(150);
        VirtualPixel1D2 pixelsMatrix = new VirtualPixel1D2(200);
        VirtualPixel1D2 saberPixels = new VirtualPixel1D2(32);
        Expander.MidiInput2 midiAkai = new Expander.MidiInput2("LPD8", true);
        Subject<bool> inflatablesRunning = new Subject<bool>();
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();
        DigitalInput2 buttonStartInflatables = new DigitalInput2();

        Import.LorImport2 lorFeelTheLight = new Import.LorImport2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        Controller.Subroutine subCandyCane = new Controller.Subroutine();
        Controller.Subroutine subStarWarsCane = new Controller.Subroutine();
        Controller.Subroutine subBackground = new Controller.Subroutine();
        Controller.Subroutine subSantaVideo = new Controller.Subroutine();
        Controller.Subroutine subOlaf = new Controller.Subroutine();
        Controller.Subroutine subR2D2 = new Controller.Subroutine();
        Controller.Subroutine subStarWars = new Controller.Subroutine();

        public Xmas2015(IEnumerable<string> args)
        {
            hours.AddRange("4:00 pm", "10:00 pm");

            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                    expanderServer.ExpanderSharedFiles = parts[1];
            }

            pulsatingEffect1.ConnectTo(lightOlaf);
            pulsatingEffect2.ConnectTo(lightR2D2);
            pulsatingEffect3.ConnectTo(pixelsRoofEdge, Utils.AdditionalData(Color.Red));

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expander1);
            expanderServer.AddInstance("10520fdcf14d47cba31da8b6e05d01d8", expander2);
            expanderServer.AddInstance("59ebb8e925c94182a0f6e0ef09180200", expander3);

            expander1.DigitalInputs[5].Connect(inR2D2);
            expander1.DigitalInputs[4].Connect(inOlaf);
            expander1.DigitalInputs[6].Connect(in1);
            expander1.DigitalOutputs[7].Connect(out1);
            expanderLocal.Connect(audioDarthVader);
            expander1.Connect(audio1);
            expander2.Connect(audio2);
            expander3.Connect(video3);

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            midiAkai.Controller(midiChannel, 1).Subscribe(x => blackOut.Value = x.Value);
            midiAkai.Controller(midiChannel, 2).Subscribe(x => whiteOut.Value = x.Value);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    hours.SetForced(true);
                else
                    hours.SetForced(null);
            });

            inflatablesRunning.Subscribe(x =>
            {
                airReindeer.Value = x;

                Exec.SetKey("InflatablesRunning", x.ToString());
            });

            // Read from storage
            inflatablesRunning.OnNext(Exec.GetSetKey("InflatablesRunning", false));

            //            hours.Output.Log("Hours inside");

            stateMachine.ForFromSubroutine(States.Background, subBackground);

            stateMachine.For(States.Music1).Execute(i =>
            {
                audio2.PlayTrack("08 Feel the Light.wav");
                i.WaitFor(S(240));
            }).TearDown(() =>
            {
                lorFeelTheLight.Stop();
                audio2.PauseTrack();
            });

            stateMachine.ForFromSubroutine(States.SantaVideo, subSantaVideo);
            stateMachine.ForFromSubroutine(States.DarthVader, subStarWars);

            airR2D2.Value = true;
            airSanta1.Value = true;
            airOlaf.Value = true;
            laser.Value = true;

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

            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 0, 50), 6, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 50, 100), 5, 1);

            acnOutput.Connect(new Physical.PixelRope(pixelsMatrix, 0, 170), 10, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsMatrix, 170, 30), 11, 1);

            acnOutput.Connect(new Physical.PixelRope(saberPixels, 0, 32), 12, 1);

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

            acnOutput.Connect(new Physical.GenericDimmer(lightR2D2, 16), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail2, 10), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 11), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 19), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(airSanta1, 20), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 22), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightReindeer1, 32), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightReindeer2, 29), SacnUniverseRenardBig);

            acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 1), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 25), SacnUniverseRenardBig);
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
                    stateMachine.SetBackgroundState(States.Background);
                    stateMachine.SetState(States.Background);
                    lightOlaf.SetBrightness(1.0);
                    lightR2D2.SetBrightness(1.0);
                    saberPixels.SetColor(Color.Red, 0.4);
                    //lightTreeUp.SetColor(Color.Red, 1.0);
                    //lightSnow1.SetBrightness(1.0);
                    //lightSnow2.SetBrightness(1.0);
                }
                else
                {
                    //if (buttonOverrideHours.Active)
                    //    return;

                    stateMachine.Hold();
                    stateMachine.SetBackgroundState(null);
                    lightOlaf.SetBrightness(0);
                    lightR2D2.SetBrightness(0);
                    saberPixels.SetColor(Color.Black, 0);
                    //EverythingOff();
                    System.Threading.Thread.Sleep(200);
                    /*
                                        airR2D2.Power = false;
                                        airSanta.Power = false;
                                        airSnowman.Power = false;
                                        airReindeer.Power = false;*/

                    //switchDeerHuge.TurnOff();
                    //switchSanta.TurnOff();
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
                    lightWall3)
                .RunAction(i =>
                {
                    lightNet1.SetBrightness(1, i.Token);
                    lightNet2.SetBrightness(1, i.Token);
                    lightNet3.SetBrightness(1, i.Token);
                    lightNet4.SetBrightness(1, i.Token);
                    lightNet5.SetBrightness(1, i.Token);
                    lightNet6.SetBrightness(1, i.Token);
                    lightNet7.SetBrightness(1, i.Token);
                    lightNet8.SetBrightness(1, i.Token);
                    lightTopper1.SetBrightness(1, i.Token);
                    lightTopper2.SetBrightness(1, i.Token);
                    lightStairs1.SetBrightness(1, i.Token);
                    lightStairs2.SetBrightness(1, i.Token);
                    lightRail1.SetBrightness(1, i.Token);
                    lightRail2.SetBrightness(1, i.Token);
                    lightSanta.SetBrightness(1, i.Token);
                    lightSnowman.SetBrightness(1, i.Token);
                    lightHat1.SetBrightness(1, i.Token);
                    lightHat2.SetBrightness(1, i.Token);
                    lightHat3.SetBrightness(1, i.Token);
                    lightHat4.SetBrightness(1, i.Token);
                    lightReindeer1.SetBrightness(1, i.Token);
                    lightReindeer2.SetBrightness(1, i.Token);
                    lightVader.SetColor(Color.Red, 1, i.Token);
                    lightWall1.SetColor(Color.Red, 1, i.Token);
                    lightWall2.SetColor(Color.Red, 1, i.Token);
                    lightWall3.SetColor(Color.Red, 1, i.Token);
                    subCandyCane.Run();
                    i.WaitUntilCancel();
                    Exec.Cancel(subCandyCane);
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
                            pixelsRoofEdge.Inject((x % spacing) == 0 ? Color.Red : Color.White, 0.5);

                            i.WaitFor(S(0.30), true);
                        }
                    }
                })
                .TearDown(() =>
                {
                    //TODO: We should write to a frame buffer and it should automatically reset when the lock is released
                    pixelsRoofEdge.SetColor(Color.Black, 0.0);
                });

            subStarWarsCane
                .LockWhenRunning(pixelsRoofEdge)
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
                                    pixelsRoofEdge.InjectRev(Color.Yellow, 1.0);
                                    break;
                                case 2:
                                case 3:
                                    pixelsRoofEdge.InjectRev(Color.Orange, 0.2);
                                    break;
                            }

                            instance.WaitFor(S(0.1));

                            if (instance.IsCancellationRequested)
                                break;
                        }
                    }
                })
                .TearDown(() =>
                {
                    //TODO: We should write to a frame buffer and it should automatically reset when the lock is released
                    pixelsRoofEdge.SetColor(Color.Black, 0.0);
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
                .RunAction(instance =>
                {
                    //Exec.Cancel(subCandyCane);
                    subStarWarsCane.Run();
                    lightR2D2.SetBrightness(1.0, instance.Token);

                    audioDarthVader.PlayTrack("01. Star Wars - Main Title.wav");

                    instance.WaitFor(S(16));

                    audioDarthVader.PauseTrack();
                    Exec.Cancel(subStarWarsCane);
                    instance.WaitFor(S(0.5));
                    /*
                        elJesus.SetPower(true);
                        pulsatingStar.Start();
                        lightJesus.SetColor(Color.White, 0.3);
                        light3wise.SetOnlyColor(Color.LightYellow);
                        light3wise.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));*/

                    Exec.MasterEffect.Fade(lightVader, 0.0, 1.0, 1000, additionalData: Utils.AdditionalData(Color.Red));

                    instance.WaitFor(S(2.5));

                    audioDarthVader.PlayEffect("saberon.wav");
                    for (int sab = 00; sab < 32; sab++)
                    {
                        saberPixels.Inject(Color.Red, 0.5);
                        instance.WaitFor(S(0.01));
                    }
                    instance.WaitFor(S(1));

                    lightVader.SetColor(Color.Red, 1.0, instance.Token);
                    audioDarthVader.PlayEffect("father.wav");
                    instance.WaitFor(S(5));

                    lightVader.SetBrightness(0.0, instance.Token);
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
                .TearDown(() =>
                {
                    audioDarthVader.PauseTrack();
                    lightR2D2.SetBrightness(0.0);
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
                    stateMachine.SetState(States.Music1);
                //                    audio2.PlayTrack("08 Feel the Light.wav");
            });

            midiAkai.Note(midiChannel, 39).Subscribe(x =>
            {
                if (x)
                {
                    lorFeelTheLight.Stop();
                    audio2.PauseTrack();
                }
            });

            midiAkai.Note(midiChannel, 40).Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetState(States.SantaVideo);
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

            audio2.AudioTrackStart += (o, e) =>
            {
                switch (e.FileName)
                {
                    case "08 Feel the Light.wav":
                        lorFeelTheLight.Start(27830);
                        break;
                }
            };

            audio2.AudioTrackDone += (o, e) =>
            {
                //                Thread.Sleep(5000);
                //    audio2.PlayTrack("08 Feel the Light.wav");
            };

            in1.Output.Subscribe(x =>
            {
                if (x)
                    subStarWars.Run();

                //if (x)
                //    video3.PlayVideo("NBC_DeckTheHalls_Holl_H.mp4");

                //                    audio2.PlayTrack("02. Frozen - Do You Want to Build a Snowman.wav");
                //                    audio1.PlayEffect("WarmHugs.wav");
                //                    audio2.PlayTrack("08 Feel the Light.wav");
                //                    audioLocal.PlayEffect("WarmHugs.wav");

                //                out1.Value = x;
                //if (x)
                //    audio2.PlayTrack("08 Feel the Light.wav");
                //                    lorFeelTheLight.Start(27830);
            });

            ImportAndMapFeelTheLight();
        }

        private void ImportAndMapFeelTheLight()
        {
            lorFeelTheLight.LoadFromFile(Path.Combine(expanderServer.ExpanderSharedFiles, "Seq", "Feel The Light, Jennifer Lopez.lms"));

            lorFeelTheLight.Progress.Subscribe(x =>
            {
                log.Trace("Feel the Light {0:N0} ms", x);
            });

            //            lorFeelTheLight.Dump();

            //lorFeelTheLight.MapDeviceRGB("E - 1", "D# - 2", "D - 3", lightStraigthAhead);
            //lorFeelTheLight.MapDeviceRGB("C# - 4", "C - 5", "B - 6", lightRightColumn);
            //lorFeelTheLight.MapDeviceRGB("A# - 7", "A - 8", "G# - 9", lightNote3);
            //lorFeelTheLight.MapDeviceRGB("G - 10", "F# - 11", "F - 12", lightNote4);
            //lorFeelTheLight.MapDeviceRGB("E - 13", "D# - 14", "D - 15", lightNote5);
            //lorFeelTheLight.MapDeviceRGB("C# - 16", "C - 1", "B - 2", lightUpTree);
            //lorFeelTheLight.MapDeviceRGB("A# - 3", "A - 4", "G# - 5", lightNote7);
            //lorFeelTheLight.MapDeviceRGB("G - 6", "F# - 7", "F - 8", lightNote8);
            //lorFeelTheLight.MapDeviceRGB("E - 9", "D# - 10", "D - 11", lightNote9);
            //lorFeelTheLight.MapDeviceRGB("C# - 12", "C - 13", "B - 14", lightVader);
            //lorFeelTheLight.MapDevice("A# - 15", lightNote11);
            //lorFeelTheLight.MapDevice("A - 16", lightNote12);

            //lorFeelTheLight.MapDevice("Unit 01.1 arch 1.1", lightNet1);
            //lorFeelTheLight.MapDevice("Unit 01.2 arch 1.2", lightNet2);
            //lorFeelTheLight.MapDevice("Unit 01.3 arch 1.3", lightNet3);
            //lorFeelTheLight.MapDevice("Unit 01.4 arch 1.4", lightNet4);
            //lorFeelTheLight.MapDevice("Unit 01.5 arch 1.5", lightNet5);
            //lorFeelTheLight.MapDevice("Unit 01.6 arch 1.6", lightNet6);
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

            lorFeelTheLight.MapDevice("03.9 mini tree 08", lightWall1, Utils.AdditionalData(Color.Red));
            lorFeelTheLight.MapDevice("03.8 mini tree 07", lightWall2, Utils.AdditionalData(Color.Red));
            lorFeelTheLight.MapDevice("03.7 mini tree 06", lightWall3, Utils.AdditionalData(Color.Red));
            lorFeelTheLight.MapDevice("03.6 mini tree 05", lightSanta);
            lorFeelTheLight.MapDevice("03.5 mini tree 04", lightSnowman);
            lorFeelTheLight.MapDevice("03.4 mini tree 03", lightVader, Utils.AdditionalData(Color.Red));
            lorFeelTheLight.MapDevice("03.3 mini tree 02",
                new VirtualDevice((b, t) => saberPixels.SetColorRange(Color.Red, b, 0, 32, t)));

            //

            //lorFeelTheLight.MapDevice("Sky 1", lightNet1);
            //lorFeelTheLight.MapDevice("Sky 2", lightNet2);
            //lorFeelTheLight.MapDevice("Sky 3", lightNet3);
            //lorFeelTheLight.MapDevice("Sky 4", lightNet4);
            //lorFeelTheLight.MapDevice("Sky 5", lightNet5);

            //            lorFeelTheLight.MapDevice("Sky 1", lightNet10);
            //            lorFeelTheLight.MapDevice("Sky 2", lightNet9);
            //lorFeelTheLight.MapDevice("Sky 3", lightNet8);
            //lorFeelTheLight.MapDevice("Sky 4", lightNet7);
            //lorFeelTheLight.MapDevice("Sky 5", lightNet6);

            //            lorFeelTheLight.MapDevice("Rooftop", snowmanKaggen);

            //lorFeelTheLight.MapDevice("Star1", lightNet11);
            //lorFeelTheLight.MapDevice("Star2", lightStairsLeft);
            //lorFeelTheLight.MapDevice("Star3", lightFenceLeft);
            //lorFeelTheLight.MapDevice("Star extra", lightStar);

            lorFeelTheLight.ControlDevice(pixelsMatrix);
            lorFeelTheLight.ControlDevice(saberPixels);
            lorFeelTheLight.MapDevice("Unit 02.1 Mega tree 1",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.2 Mega tree 2",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 20, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.3 Mege tree 3",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 40, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.4 Mega tree 4",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 60, 20, t)));
            //lorFeelTheLight.MapDevice("Unit 02.5 Mega tree 5",
            //    new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 20, t)));
            //lorFeelTheLight.MapDevice("Unit 02.6 Mega tree 6",
            //    new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 20, t)));
            //lorFeelTheLight.MapDevice("Unit 02.7 Mega tree 7",
            //    new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 20, t)));
            //lorFeelTheLight.MapDevice("Unit 02.8 Mega tree 8",
            //    new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.9 Mega tree 9",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 80, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.10 Mega tree 10",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 100, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.11 Mega tree 11",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 120, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.12 Mega tree 12",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 140, 20, t)));
            //lorFeelTheLight.MapDevice("Unit 02.13 Mega tree 13",
            //    new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 20, t)));
            //lorFeelTheLight.MapDevice("Unit 02.14 Mega tree 14",
            //    new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.15 Mega tree 15",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 160, 20, t)));
            lorFeelTheLight.MapDevice("Unit 02.16 Mega tree 16",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 180, 20, t)));
            lorFeelTheLight.MapDevice("03.1 mega tree topper 01", pixelsRoofEdge, Utils.AdditionalData(Color.White));

            /*
                        lightBottom.OutputBrightness.Subscribe(x =>
                        {
                            //                    pixelsVideo.SetBrightness(x, null);
                        });
                        lightBottom.OutputColor.Subscribe(x =>
                        {
                            //                    pixelsVideo.SetAllOnlyColor(x);
                        });
            */
            //            lorFeelTheLight.MapDeviceRGBW("R-Edge R", "R-Edge G", "R-Edge B", "R-Edge W", lightRoofEdge);
            //            lorFeelTheLight.MapDeviceRGBW("R-Bottom", "G-Bottom", "B-Bottom", "W-Bottom", lightNotUsed1);
            //            lorFeelTheLight.MapDeviceRGBW("Garage R", "Garage G", "Garage B", "Garage W", lightWhiteStrobe);
            //            lorFeelTheLight.MapDeviceRGBW("Rwindo R", "Rwindo G", "Rwindo B", "Rwindo W", lightMetalReindeer);
            //lorFeelTheLight.MapDeviceRGBW("Cwindo R", "Cwindo G", "Cwindo B", "Cwindo W", lightCWindow);
            //lorFeelTheLight.MapDeviceRGBW("Lwindo R", "Lwindo G", "Lwindo B", "Lwindo W", lightLWindow);
            //lorFeelTheLight.MapDeviceRGBW("Ft door R", "Ft door G", "Ft door B", "FT door W", lightFrontDoor);
            //lorFeelTheLight.MapDeviceRGBW("Bush - red", "Bush - green", "Bush - blue", "Bush - white", lightBush);

            //lorFeelTheLight.MapDevice("Tree - A", lightSnowman);
            //lorFeelTheLight.MapDevice("Tree - B", lightSanta);

            //lorFeelTheLight.MapDevice("Spoke 1a", lightHat1);
            //lorFeelTheLight.MapDevice("Spoke 2a", lightHat2);
            //lorFeelTheLight.MapDevice("Spoke 3a", lightHat3);
            //lorFeelTheLight.MapDevice("Spoke  4a", lightHat4);
            //lorFeelTheLight.MapDevice("Spoke 5a", lightR2D2);
            //lorFeelTheLight.MapDevice("Spoke 6a", lightNet11);

            //lorFeelTheLight.MapDevice("Spoke 7a", lightStairsLeft);
            //lorFeelTheLight.MapDevice("Spoke 8a", lightFenceLeft);
            //lorFeelTheLight.MapDevice("Spoke 9a", lightFenceMid);
            //lorFeelTheLight.MapDevice("Spoike 10a", lightFenceRight);
            //lorFeelTheLight.MapDevice("Spoke  11a", lightStairsRight);
            //lorFeelTheLight.MapDevice("Spoke  12a", lightNet11);
            //lorFeelTheLight.MapDevice("Spoke  13a", lightBushes);
            // lorImport.MapDevice("Spoke  14a", light);
            // lorImport.MapDevice("Spoke  15a", light);
            // lorImport.MapDevice("Spoke  16a", light);
            // lorImport.MapDevice("Pillar L8", light);
            // lorImport.MapDevice("Pillar L7", light);
            // lorImport.MapDevice("Pillar L6", light);
            // lorImport.MapDevice("Pillar L5", light);
            // lorImport.MapDevice("Pillar L4", light);
            // lorImport.MapDevice("Pillar L3", light);
            // lorImport.MapDevice("Pillar L2", light);
            // lorImport.MapDevice("Pillar L1", light);
            // lorImport.MapDevice("Pillar R8", light);
            // lorImport.MapDevice("Pillar R7", light);
            // lorImport.MapDevice("Pillar R6", light);
            // lorImport.MapDevice("Pillar R5", light);
            // lorImport.MapDevice("Pillar R4", light);
            // lorImport.MapDevice("Pillar R3", light);
            // lorImport.MapDevice("Pillar R2", light);
            // lorImport.MapDevice("Pillar R1", light);
            // lorImport.MapDevice("8  MiniTree 1r", light);
            // lorImport.MapDevice("8  MiniTree 2r", light);
            // lorImport.MapDevice("8  MiniTree 3r", light);
            // lorImport.MapDevice("8  MiniTree 4r", light);
            // lorImport.MapDevice("8  MiniTree 5r", light);
            // lorImport.MapDevice("8  MiniTree 6r", light);
            // lorImport.MapDevice("8  MiniTree 7r", light);
            // lorImport.MapDevice("8  MiniTree 8r", light);
            // lorImport.MapDevice("8  MiniTree 9r", light);
            // lorImport.MapDevice("8  MiniTree 10r", light);
            // lorImport.MapDevice("8  MiniTree 11r", light);
            // lorImport.MapDevice("8  MiniTree 12r", light);
            // lorImport.MapDevice("8  MiniTree 13r", light);
            // lorImport.MapDevice("8  MiniTree 14r", light);
            // lorImport.MapDevice("8  MiniTree 15r", light);
            // lorImport.MapDevice("8  MiniTree 16r", light);
            // lorImport.MapDevice("MiniTree 1g", light);
            // lorImport.MapDevice("MiniTree 2g", light);
            // lorImport.MapDevice("MiniTree 3g", light);
            // lorImport.MapDevice("MiniTree 4g", light);
            // lorImport.MapDevice("MiniTree 5g", light);
            // lorImport.MapDevice("MiniTree 6g", light);
            // lorImport.MapDevice("MiniTree 7g", light);
            // lorImport.MapDevice("MiniTree 8g", light);
            // lorImport.MapDevice("MiniTree 9g", light);
            // lorImport.MapDevice("MiniTree 10g", light);
            // lorImport.MapDevice("MiniTree 11g", light);
            // lorImport.MapDevice("MiniTree 12g", light);
            // lorImport.MapDevice("MiniTree 13g", light);
            // lorImport.MapDevice("MiniTree 14g", light);
            // lorImport.MapDevice("MiniTree 15g", light);
            // lorImport.MapDevice("MiniTree 16g", light);
            // lorImport.MapDevice("Hray B1", light);
            // lorImport.MapDevice("Hray B2", light);
            // lorImport.MapDevice("Hray B3", light);
            // lorImport.MapDevice("Hray B4", light);
            // lorImport.MapDevice("Hray B5", light);
            // lorImport.MapDevice("Hray B6", light);
            // lorImport.MapDevice("Hray B7", light);
            // lorImport.MapDevice("Hray B8", light);
            // lorImport.MapDevice("Hray R1", light);
            // lorImport.MapDevice("Hray R2", light);
            // lorImport.MapDevice("Hray R3", light);
            // lorImport.MapDevice("Hray R4", light);
            // lorImport.MapDevice("Hray R5", light);
            // lorImport.MapDevice("Hray R6", light);
            // lorImport.MapDevice("Hray R7", light);
            // lorImport.MapDevice("Hray R8", light);
            // lorImport.MapDevice("Vray 1", light);
            // lorImport.MapDevice("Vray 2", light);
            // lorImport.MapDevice("Vray 3", light);
            // lorImport.MapDevice("Vray 4", light);
            // lorImport.MapDevice("Vray 5", light);
            // lorImport.MapDevice("Vray 6", light);
            // lorImport.MapDevice("Vray 7", light);
            // lorImport.MapDevice("Vray 8", light);
            // lorImport.MapDevice("Vray 9", light);
            // lorImport.MapDevice("Vray 10", light);
            // lorImport.MapDevice("Vray 11", light);
            // lorImport.MapDevice("Vray 12", light);
            // lorImport.MapDevice("Vray 13", light);
            // lorImport.MapDevice("Vray 14", light);
            // lorImport.MapDevice("Vray 15", light);
            // lorImport.MapDevice("Vray 16", light);
            // lorImport.MapDevice("Vray 17", light);
            // lorImport.MapDevice("Vray 18", light);
            // lorImport.MapDevice("Vray 19", light);
            // lorImport.MapDevice("Vray 20", light);
            // lorImport.MapDevice("Vray 21", light);
            // lorImport.MapDevice("Vray 22", light);
            // lorImport.MapDevice("Vray 23", light);
            // lorImport.MapDevice("Vray 24", light);
            // lorImport.MapDevice("Vray 25", light);
            // lorImport.MapDevice("Vray 26", light);
            // lorImport.MapDevice("Vray 27", light);
            // lorImport.MapDevice("Vray 28", light);
            // lorImport.MapDevice("Vray 29", light);
            // lorImport.MapDevice("Vray 30", light);
            // lorImport.MapDevice("Vray 31", light);
            // lorImport.MapDevice("Vray 32", light);
            // lorImport.MapDevice("Arch 1-1", light);
            // lorImport.MapDevice("Arch 1-2", light);
            // lorImport.MapDevice("Arch 1-3", light);
            // lorImport.MapDevice("Arch 1-4", light);
            // lorImport.MapDevice("Arch 1-5", light);
            // lorImport.MapDevice("Arch 1-6", light);
            // lorImport.MapDevice("Arch 1-7", light);
            // lorImport.MapDevice("Arch 1-8", light);
            // lorImport.MapDevice("Arch 2-1", light);
            // lorImport.MapDevice("Arch 2-2", light);
            // lorImport.MapDevice("Arch 2-3", light);
            // lorImport.MapDevice("Arch 2-4", light);
            // lorImport.MapDevice("Arch 2-5", light);
            // lorImport.MapDevice("Arch 2-6", light);
            // lorImport.MapDevice("Arch 2-7", light);
            // lorImport.MapDevice("Arch 2-8", light);
            // lorImport.MapDevice("Arch 3-1", light);
            // lorImport.MapDevice("Arch 3-2", light);
            // lorImport.MapDevice("Arch 3-3", light);
            // lorImport.MapDevice("Arch 3-4", light);
            // lorImport.MapDevice("Arch 3-5", light);
            // lorImport.MapDevice("Arch 3-6", light);
            // lorImport.MapDevice("Arch 3-7", light);
            // lorImport.MapDevice("Arch 3-8", light);
            // lorImport.MapDevice("Arch 4-1", light);
            // lorImport.MapDevice("Arch 4-2", light);
            // lorImport.MapDevice("Arch 4-3", light);
            // lorImport.MapDevice("Arch 4-4", light);
            // lorImport.MapDevice("Arch 4-5", light);
            // lorImport.MapDevice("Arch 4-6", light);
            // lorImport.MapDevice("Arch 4-7", light);
            // lorImport.MapDevice("Arch 4-8", light);

            lorFeelTheLight.Prepare();
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
