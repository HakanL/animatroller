using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;
using Import = Animatroller.Framework.Import;
using CSCore;
using CSCore.SoundOut;
using CSCore.Codecs;

namespace Animatroller.SceneRunner
{
    internal class Xmas2014 : BaseScene
    {
        const int SacnUniverseDMX = 20;
        const int SacnUniverseRenard1 = 21;
        const int SacnUniverseRenard2 = 22;
        const int SacnUniverseArduino = 23;
        const int midiChannel = 1;

        public enum States
        {
            Background,
            Music1,
            DarthVader
        }

        ISoundOut soundOut = new WasapiOut();

        Expander.AcnStream acnOutput = new Expander.AcnStream();
        Expander.OscServer oscServer = new Expander.OscServer(8000);
        OperatingHours2 hours = new OperatingHours2();
        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();
        AudioPlayer audioDarthVader = new AudioPlayer();
        Expander.Raspberry raspberryDarth = new Expander.Raspberry("192.168.240.115:5005", 3333);

        VirtualPixel1D pixelsRoofEdge = new VirtualPixel1D(150);
        VirtualPixel1D saberPixels = new VirtualPixel1D(32);
        VirtualPixel1D saberSidePixels = new VirtualPixel1D(18);
        DigitalInput2 buttonTest = new DigitalInput2();
        DigitalInput2 buttonTest2 = new DigitalInput2();
        DigitalInput buttonStartInflatables = new DigitalInput();

        //        Dimmer3 lightSpiral1 = new Dimmer3();
        //        Dimmer3 lightSpiral2 = new Dimmer3();

        Effect.Pulsating pulsatingStar = new Effect.Pulsating(S(2), 0.2, 0.4, false);

        ColorDimmer3 lightNote1 = new ColorDimmer3();
        ColorDimmer3 lightNote2 = new ColorDimmer3();
        ColorDimmer3 lightNote3 = new ColorDimmer3();
        ColorDimmer3 lightNote4 = new ColorDimmer3();
        ColorDimmer3 lightNote5 = new ColorDimmer3();
        ColorDimmer3 lightNote6 = new ColorDimmer3();
        ColorDimmer3 lightNote7 = new ColorDimmer3();
        ColorDimmer3 lightNote8 = new ColorDimmer3();
        ColorDimmer3 lightNote9 = new ColorDimmer3();
        ColorDimmer3 lightVader = new ColorDimmer3();
        ColorDimmer3 lightNote11 = new ColorDimmer3();
        ColorDimmer3 lightNote12 = new ColorDimmer3();

        Dimmer3 lightHat1 = new Dimmer3();
        Dimmer3 lightHat2 = new Dimmer3();
        Dimmer3 lightHat3 = new Dimmer3();
        Dimmer3 lightHat4 = new Dimmer3();
        Dimmer3 snowmanKaggen = new Dimmer3();
        DigitalOutput2 airSnowman = new DigitalOutput2();
        DigitalOutput2 airR2D2 = new DigitalOutput2();
        DigitalOutput2 airSanta = new DigitalOutput2();
        DigitalOutput2 airReindeer = new DigitalOutput2();
        DigitalOutput2 packages = new DigitalOutput2();
        Dimmer3 lightSnowman = new Dimmer3();
        Dimmer3 lightSanta = new Dimmer3();
        Dimmer3 lightR2D2 = new Dimmer3();
        Dimmer3 lightNet1 = new Dimmer3();
        Dimmer3 lightNet2 = new Dimmer3();
        Dimmer3 lightNet3 = new Dimmer3();
        Dimmer3 lightNet4 = new Dimmer3();
        Dimmer3 lightNet5 = new Dimmer3();
        Dimmer3 lightNet6 = new Dimmer3();
        Dimmer3 lightNet7 = new Dimmer3();
        Dimmer3 lightNet8 = new Dimmer3();
        Dimmer3 lightNet9 = new Dimmer3();
        Dimmer3 lightNet10 = new Dimmer3();
        Dimmer3 lightMetalReindeer = new Dimmer3();

        Dimmer3 lightStarExtra = new Dimmer3();

        Dimmer3 lightX1 = new Dimmer3();
        Dimmer3 lightX2 = new Dimmer3();
        Dimmer3 lightX3 = new Dimmer3();
        Dimmer3 lightX4 = new Dimmer3();
        Dimmer3 lightX5 = new Dimmer3();
        Dimmer3 lightX6 = new Dimmer3();
        Dimmer3 lightX7 = new Dimmer3();
        Dimmer3 lightBushes = new Dimmer3();

        ColorDimmer3 lightREdge = new ColorDimmer3();
        ColorDimmer3 lightBottom = new ColorDimmer3();
        ColorDimmer3 lightGarage = new ColorDimmer3();
        ColorDimmer3 lightRWindow = new ColorDimmer3();
        ColorDimmer3 lightCWindow = new ColorDimmer3();
        ColorDimmer3 lightLWindow = new ColorDimmer3();
        ColorDimmer3 lightFrontDoor = new ColorDimmer3();
        ColorDimmer3 lightBush = new ColorDimmer3();

        MovingHead movingHead = new MovingHead();
        GroupDimmer allLights = new GroupDimmer();

        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);

        Expander.MidiInput2 midiInput = new Expander.MidiInput2();
        Controller.Sequence candyCane = new Controller.Sequence();
        Controller.Sequence fatherSeq = new Controller.Sequence();
        Controller.Sequence starwarsCane = new Controller.Sequence();
        Controller.Sequence music1Seq = new Controller.Sequence();
        Controller.Sequence backgroundLoop = new Controller.Sequence();
        Subject<bool> inflatablesRunning = new Subject<bool>();

        Import.LorImport2 lorImport = new Import.LorImport2();

        public Xmas2014(IEnumerable<string> args)
        {
            hours.AddRange("4:00 pm", "9:00 pm");
            //            hours.SetForced(true);

            inflatablesRunning.Subscribe(x =>
                {
                    airReindeer.Power = x;

                    Exec.SetKey("InflatablesRunning", x.ToString());
                });

            // Read from storage
            inflatablesRunning.OnNext(Exec.GetSetKey("InflatablesRunning", false));

            hours.Output.Log("Hours inside");
            movingHead.InputPan.Log("Pan");
            movingHead.InputTilt.Log("Tilt");

            raspberryDarth.Connect(audioDarthVader);

            packages.Power = true;
            airSnowman.Power = true;
            airR2D2.Power = true;
            airSanta.Power = true;

            hours
                .ControlsMasterPower(packages)
                .ControlsMasterPower(airSnowman)
                .ControlsMasterPower(airR2D2)
                .ControlsMasterPower(airSanta);

            midiInput.Controller(midiChannel, 1).Controls(
                Observer.Create<double>(x => { allLights.Brightness = x; }));

            //            pulsatingEffect1.ConnectTo(lightStar.GetBrightnessObserver());

            //            pulsatingStar.ConnectTo()

            buttonStartInflatables.ActiveChanged += (o, e) =>
            {
                if (e.NewState && hours.IsOpen)
                {
                    inflatablesRunning.OnNext(true);
                }
            };

            hours.Output.Subscribe(pulsatingEffect1.InputRun);

            hours.Output.Subscribe(x =>
                {
                    packages.Power = x;
/*
                    lightHat1.Brightness = x ? 1.0 : 0.0;
                    lightHat2.Brightness = x ? 1.0 : 0.0;
                    lightHat3.Brightness = x ? 1.0 : 0.0;
                    lightHat4.Brightness = x ? 1.0 : 0.0;
                    snowmanKaggen.Brightness = x ? 1.0 : 0.0;
                    lightSnowman.Brightness = x ? 1.0 : 0.0;
                    lightSanta.Brightness = x ? 1.0 : 0.0;
                    lightR2D2.Brightness = x ? 1.0 : 0.0;
                    lightStarExtra.Brightness = x ? 1.0 : 0.0;

                    lightNet1.Brightness = x ? 1.0 : 0.0;
                    lightNet2.Brightness = x ? 1.0 : 0.0;
                    lightNet3.Brightness = x ? 1.0 : 0.0;
                    lightNet4.Brightness = x ? 1.0 : 0.0;
                    lightNet5.Brightness = x ? 1.0 : 0.0;
                    lightNet6.Brightness = x ? 1.0 : 0.0;
                    lightNet7.Brightness = x ? 1.0 : 0.0;
                    lightNet8.Brightness = x ? 1.0 : 0.0;
                    lightNet9.Brightness = x ? 1.0 : 0.0;
                    lightNet10.Brightness = x ? 1.0 : 0.0;

                    lightX1.Brightness = x ? 1.0 : 0.0;
                    lightX2.Brightness = x ? 1.0 : 0.0;
                    lightX3.Brightness = x ? 1.0 : 0.0;
                    lightX4.Brightness = x ? 1.0 : 0.0;
                    lightX5.Brightness = x ? 1.0 : 0.0;
                    lightX6.Brightness = x ? 1.0 : 0.0;
                    lightX7.Brightness = x ? 1.0 : 0.0;
                    lightBushes.Brightness = x ? 1.0 : 0.0;*/
                });

            lightSanta.SetOutputFilter(new Effect.Blackout());

            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 0, 50), 4, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 50, 100), 5, 1);
            acnOutput.Connect(new Physical.PixelRope(saberPixels, 0, 32), 14, 85);
            acnOutput.Connect(new Physical.PixelRope(saberSidePixels, 0, 18), 14, 1);

            acnOutput.Connect(new Physical.GenericDimmer(airReindeer, 10), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airSnowman, 11), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airSanta, 12), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airR2D2, 13), SacnUniverseDMX);

            acnOutput.Connect(new Physical.GenericDimmer(lightStarExtra, 50), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightBushes, 51), SacnUniverseDMX);

            acnOutput.Connect(new Physical.AmericanDJStrobe(lightGarage, 5), SacnUniverseDMX);
            //            acnOutput.Connect(new Physical.SmallRGBStrobe(lightBottom, 1), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightNote1, 60), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightNote2, 80), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightNote6, 40), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightVader, 70), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 1), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 2), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 3), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 4), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(movingHead, 200), SacnUniverseDMX);

            acnOutput.Connect(new Physical.GenericDimmer(packages, 1), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(snowmanKaggen, 2), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightR2D2, 3), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 4), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet4, 5), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet3, 6), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet1, 7), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 8), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 5), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 6), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet8, 7), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet9, 8), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet10, 9), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightRWindow, 10), SacnUniverseRenard1);     // Metal reindeers

            acnOutput.Connect(new Physical.GenericDimmer(lightX1, 10), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightX2, 11), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightX3, 12), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightX4, 13), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightX5, 14), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightX6, 15), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightX7, 16), SacnUniverseRenard1);


            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 1), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 2), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 3), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 4), 22);

            acnOutput.Connect(new Physical.GenericDimmer(lightSanta, 1), SacnUniverseArduino);
            acnOutput.Connect(new Physical.GenericDimmer(lightSnowman, 2), SacnUniverseArduino);

            stateMachine.ForFromSequence(States.Background, backgroundLoop);
            stateMachine.ForFromSequence(States.Music1, music1Seq);
            stateMachine.ForFromSequence(States.DarthVader, fatherSeq);

            /*            oscServer.RegisterActionSimple<double>("/1/faderA", (msg, data) =>
                        {
                            lightSnowman.Brightness = data;
                        });

                        oscServer.RegisterActionSimple<double>("/1/faderD", (msg, data) =>
                        {
                            Exec.Blackout.OnNext(data);
                        });*/

            oscServer.RegisterActionSimple<int>("/2/push1", (msg, data) =>
                {
                    if (data == 1)
                    {
                        stateMachine.SetState(States.Music1);
                    }
                });

            oscServer.RegisterActionSimple<int>("/2/push2", (msg, data) =>
            {
                if (data == 1)
                {
                    stateMachine.SetState(States.DarthVader);
                }
            });


            music1Seq
                .WhenExecuted
                .SetUp(() =>
                {
                    //                    audioPlayer.CueTrack("21. Christmas Canon Rock");
                    // Make sure it's ready
                    //                    System.Threading.Thread.Sleep(800);

                    //                    EverythingOff();
                    soundOut.Stop();
                    soundOut.WaitForStopped();
                })
                .Execute(instance =>
                {
                    using (var waveCarol = CodecFactory.Instance.GetCodec(@"C:\Projects\Other\ChristmasSounds\trk\09 Carol of the Bells (Instrumental).wav"))
                    {
                        soundOut.Initialize(waveCarol);
                        soundOut.Play();
                        var task = lorImport.Start();
                        task.Wait(instance.CancelToken);

                        instance.WaitFor(S(5));

                        soundOut.Stop();
                        soundOut.WaitForStopped();
                    }

                    instance.WaitFor(S(8));
                })
                .TearDown(() =>
                {
                    lorImport.Stop();
                    soundOut.Stop();
                });

            backgroundLoop
                .WhenExecuted
                .SetUp(() =>
                {
                    //                    Exec.Execute(candyCane);

                    //pulsatingEffect1.Start();
                    //flickerEffect.Start();
                    //switchButtonBlue.SetPower(true);
                    //switchButtonRed.SetPower(true);
                    //lightTreeUp.SetOnlyColor(Color.Red);

                    //faderIn.Restart();

                    //Executor.Current.Execute(twinkleSeq);
                })
                .Execute(i =>
                    {
                        while (!i.IsCancellationRequested)
                        {
                            Exec.ExecuteAndWait(fatherSeq);
                            
                            if (i.IsCancellationRequested)
                                break;

                            Exec.ExecuteAndWait(music1Seq);
                        }
                    })
                .TearDown(() =>
                {
                    //                    Exec.Cancel(candyCane);
                    //Executor.Current.Cancel(twinkleSeq);

                    //switchButtonBlue.SetPower(false);
                    //switchButtonRed.SetPower(false);
                    //EverythingOff();
                });

            starwarsCane
                .WhenExecuted
                .SetUp(() =>
                {
                    pixelsRoofEdge.TurnOff();
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
                        pixelsRoofEdge.TurnOff();
                    });

            fatherSeq
                .WhenExecuted
                .Execute(instance =>
                {
                    Exec.Cancel(candyCane);
                    Executor.Current.Execute(starwarsCane);
                    lightR2D2.Brightness = 1.0;

                    soundOut.Stop();
                    soundOut.WaitForStopped();
                    using (var waveStarwars = CodecFactory.Instance.GetCodec(@"C:\Projects\Other\ChristmasSounds\trk\01. Star Wars - Main Title.wav"))
                    {
                        soundOut.Initialize(waveStarwars);
                        soundOut.Play();

                        //lightCeiling1.SetOnlyColor(Color.Yellow);
                        //lightCeiling2.SetOnlyColor(Color.Yellow);
                        //lightCeiling3.SetOnlyColor(Color.Yellow);
                        //pulsatingEffect2.Start();

                        instance.WaitFor(S(16));

                        //pulsatingEffect2.Stop();
                        soundOut.Stop();
                        soundOut.WaitForStopped();
                        Executor.Current.Cancel(starwarsCane);
                        pixelsRoofEdge.TurnOff();
                        instance.WaitFor(S(0.5));
                        /*
                                            elJesus.SetPower(true);
                                            pulsatingStar.Start();
                                            lightJesus.SetColor(Color.White, 0.3);
                                            light3wise.SetOnlyColor(Color.LightYellow);
                                            light3wise.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));*/
                        lightVader.SetOnlyColor(Color.Red);
                        var ctrl = lightVader.TakeControl();
                        Exec.MasterEffect.Fade(lightVader, 0, 1, 1000).ContinueWith(_ => ctrl.Dispose());

                        instance.WaitFor(S(2.5));

                        //elLightsaber.SetPower(true);
                        audioDarthVader.PlayEffect("saberon");
                        for (int sab = 00; sab < 32; sab++)
                        {
                            saberPixels.Inject(Color.Red, 0.5);
                            instance.WaitFor(S(0.01));
                        }
                        instance.WaitFor(S(1));

                        lightVader.SetColor(Color.Red, 1.0);
                        audioDarthVader.PlayEffect("father");
                        instance.WaitFor(S(4));
                        saberSidePixels.SetAll(Color.Red, 0.5);
                        instance.WaitFor(S(1));

                        lightVader.Brightness = 0;
                        //light3wise.TurnOff();
                        //lightJesus.TurnOff();
                        //pulsatingStar.Stop();
                        //elJesus.TurnOff();

                        audioDarthVader.PlayEffect("force1");
                        instance.WaitFor(S(4));

                        lightVader.Brightness = 0;
                        saberSidePixels.SetAll(Color.Red, 0);

                        audioDarthVader.PlayEffect("saberoff");
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

                    }
                })
                .TearDown(() =>
                {
                    lightR2D2.Brightness = 0;
                    //                    EverythingOff();

                    soundOut.Stop();
                });

            hours.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetBackgroundState(States.Background);
                    stateMachine.SetState(States.Background);
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

            ImportAndMapLOR();
        }

        private void ImportAndMapLOR()
        {
            lorImport.LoadFromFile(@"..\..\..\Test Files\David Foster - Carol of the Bells.lms");

            lorImport.MapDeviceRGB("E - 1", "D# - 2", "D - 3", lightNote1);
            lorImport.MapDeviceRGB("C# - 4", "C - 5", "B - 6", lightNote2);
            lorImport.MapDeviceRGB("A# - 7", "A - 8", "G# - 9", lightNote3);
            lorImport.MapDeviceRGB("G - 10", "F# - 11", "F - 12", lightNote4);
            lorImport.MapDeviceRGB("E - 13", "D# - 14", "D - 15", lightNote5);
            lorImport.MapDeviceRGB("C# - 16", "C - 1", "B - 2", lightNote6);
            lorImport.MapDeviceRGB("A# - 3", "A - 4", "G# - 5", lightNote7);
            lorImport.MapDeviceRGB("G - 6", "F# - 7", "F - 8", lightNote8);
            lorImport.MapDeviceRGB("E - 9", "D# - 10", "D - 11", lightNote9);
            lorImport.MapDeviceRGB("C# - 12", "C - 13", "B - 14", lightVader);
            lorImport.MapDevice("A# - 15", lightNote11);
            lorImport.MapDevice("A - 16", lightNote12);

            lorImport.MapDevice("Sky 1", lightNet1);
            lorImport.MapDevice("Sky 2", lightNet2);
            lorImport.MapDevice("Sky 3", lightNet3);
            lorImport.MapDevice("Sky 4", lightNet4);
            lorImport.MapDevice("Sky 5", lightNet5);

            lorImport.MapDevice("Sky 1", lightNet10);
            lorImport.MapDevice("Sky 2", lightNet9);
            lorImport.MapDevice("Sky 3", lightNet8);
            lorImport.MapDevice("Sky 4", lightNet7);
            lorImport.MapDevice("Sky 5", lightNet6);

            lorImport.MapDevice("Rooftop", snowmanKaggen);

            lorImport.MapDevice("Star1", lightX1);
            lorImport.MapDevice("Star2", lightX2);
            lorImport.MapDevice("Star3", lightX3);
            lorImport.MapDevice("Star extra", lightStarExtra);

            lightREdge.OutputBrightness.Subscribe(x =>
            {
                pixelsRoofEdge.SetBrightness(x, null);
            });
            lightREdge.OutputColor.Subscribe(x =>
            {
                pixelsRoofEdge.SetAllOnlyColor(x);
            });
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
            lorImport.MapDeviceRGBW("R-Edge R", "R-Edge G", "R-Edge B", "R-Edge W", lightREdge);
            lorImport.MapDeviceRGBW("R-Bottom", "G-Bottom", "B-Bottom", "W-Bottom", lightBottom);
            lorImport.MapDeviceRGBW("Garage R", "Garage G", "Garage B", "Garage W", lightGarage);
            lorImport.MapDeviceRGBW("Rwindo R", "Rwindo G", "Rwindo B", "Rwindo W", lightRWindow);
            lorImport.MapDeviceRGBW("Cwindo R", "Cwindo G", "Cwindo B", "Cwindo W", lightCWindow);
            lorImport.MapDeviceRGBW("Lwindo R", "Lwindo G", "Lwindo B", "Lwindo W", lightLWindow);
            lorImport.MapDeviceRGBW("Ft door R", "Ft door G", "Ft door B", "FT door W", lightFrontDoor);
            lorImport.MapDeviceRGBW("Bush - red", "Bush - green", "Bush - blue", "Bush - white", lightBush);

            lorImport.MapDevice("Tree - A", lightSnowman);
            lorImport.MapDevice("Tree - B", lightSanta);

            lorImport.MapDevice("Spoke 1a", lightHat1);
            lorImport.MapDevice("Spoke 2a", lightHat2);
            lorImport.MapDevice("Spoke 3a", lightHat3);
            lorImport.MapDevice("Spoke  4a", lightHat4);
            lorImport.MapDevice("Spoke 5a", lightR2D2);
            lorImport.MapDevice("Spoke 6a", lightX1);

            lorImport.MapDevice("Spoke 7a", lightX2);
            lorImport.MapDevice("Spoke 8a", lightX3);
            lorImport.MapDevice("Spoke 9a", lightX4);
            lorImport.MapDevice("Spoike 10a", lightX5);
            lorImport.MapDevice("Spoke  11a", lightX6);
            lorImport.MapDevice("Spoke  12a", lightX7);
            lorImport.MapDevice("Spoke  13a", lightBushes);
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


            lorImport.Prepare();
        }

        public override void Start()
        {
            //            soundOut.Initialize(waveSource);

            candyCane
                .WhenExecuted
                .SetUp(() => pixelsRoofEdge.TurnOff())
                .Execute(instance =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int i = 0; i < spacing; i++)
                        {
                            pixelsRoofEdge.Inject((i % spacing) == 0 ? Color.Red : Color.White, 0.5);

                            instance.WaitFor(S(0.30), true);
                        }
                    }
                })
                .TearDown(() =>
                    {
                        pixelsRoofEdge.TurnOff();
                    });


            // Test Button
            buttonTest.Output.Subscribe(x =>
            {
                if (!x)
                    return;

                stateMachine.SetState(States.Music1);
            });

            buttonTest2.Output.Subscribe(x =>
                {
                    if (!x)
                        return;

                    stateMachine.SetState(States.DarthVader);
                    //                    Exec.Execute(fatherSeq);
                    //                    starwarsPixels.SetAll(Color.Red, 1.0);

                    /*
                    var ctrl = movingHead.TakeControl();
                    Exec.MasterEffect.Fade(movingHead.InputPan, 0, 540, 3000)
                        .ContinueWith(_ => ctrl.Dispose());*/
                });
        }

        public override void Run()
        {
            movingHead.Pan = 0;
            movingHead.Tilt = 0;
        }

        public override void Stop()
        {
            soundOut.Stop();
            System.Threading.Thread.Sleep(200);
        }
    }
}
