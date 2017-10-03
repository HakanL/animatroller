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

namespace Animatroller.Scenes
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
            Music2,
            DarthVader
        }

        ISoundOut soundOut = new WasapiOut();

        Expander.AcnStream acnOutput = new Expander.AcnStream();
        Expander.OscServer oscServer = new Expander.OscServer(8000);
        OperatingHours2 hours = new OperatingHours2();
        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();
        AudioPlayer audioDarthVader = new AudioPlayer();
        Expander.Raspberry raspberryDarth = new Expander.Raspberry("192.168.240.115:5005", 3333);
        Expander.Raspberry raspberrySnow = new Expander.Raspberry("192.168.240.131:5005", 3336);

        VirtualPixel1D pixelsRoofEdge = new VirtualPixel1D(150);
        VirtualPixel1D saberPixels = new VirtualPixel1D(32);
        VirtualPixel1D saberSidePixels = new VirtualPixel1D(18);
        DigitalInput2 buttonTest = new DigitalInput2();
        DigitalInput2 buttonTest2 = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonSnowMachine = new DigitalInput2();
        DigitalInput buttonStartInflatables = new DigitalInput();

        //        Dimmer3 lightSpiral1 = new Dimmer3();
        //        Dimmer3 lightSpiral2 = new Dimmer3();

        Effect.Pulsating pulsatingStar = new Effect.Pulsating(S(2), 0.2, 0.4, false);

        ColorDimmer3 lightStraigthAhead = new ColorDimmer3();
        ColorDimmer3 lightRightColumn = new ColorDimmer3();
        //ColorDimmer3 lightNote3 = new ColorDimmer3();
        //ColorDimmer3 lightNote4 = new ColorDimmer3();
        //ColorDimmer3 lightNote5 = new ColorDimmer3();
        ColorDimmer3 lightUpTree = new ColorDimmer3();
        //ColorDimmer3 lightNote7 = new ColorDimmer3();
        //ColorDimmer3 lightNote8 = new ColorDimmer3();
        //ColorDimmer3 lightNote9 = new ColorDimmer3();
        ColorDimmer3 lightVader = new ColorDimmer3();
        ColorDimmer3 lightSnow = new ColorDimmer3();
        //ColorDimmer3 lightNote11 = new ColorDimmer3();
        //ColorDimmer3 lightNote12 = new ColorDimmer3();

        DigitalOutput2 airSnowman = new DigitalOutput2();
        DigitalOutput2 airR2D2 = new DigitalOutput2();
        DigitalOutput2 airSanta = new DigitalOutput2();
        DigitalOutput2 airReindeer = new DigitalOutput2();
        DigitalOutput2 packages = new DigitalOutput2();
        DigitalOutput2 snowMachine = new DigitalOutput2();
        Dimmer3 lightHat1 = new Dimmer3();
        Dimmer3 lightHat2 = new Dimmer3();
        Dimmer3 lightHat3 = new Dimmer3();
        Dimmer3 lightHat4 = new Dimmer3();
        Dimmer3 snowmanKaggen = new Dimmer3();
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
        Dimmer3 lightNet11 = new Dimmer3();

        Dimmer3 lightStar = new Dimmer3();

        Dimmer3 lightStairsLeft = new Dimmer3();
        Dimmer3 lightFenceLeft = new Dimmer3();
        Dimmer3 lightFenceMid = new Dimmer3();
        Dimmer3 lightFenceRight = new Dimmer3();
        Dimmer3 lightStairsRight = new Dimmer3();
        Dimmer3 lightBushes = new Dimmer3();

        ColorDimmer3 lightRoofEdge = new ColorDimmer3();
        //        ColorDimmer3 lightNotUsed1 = new ColorDimmer3();
        ColorDimmer3 lightWhiteStrobe = new ColorDimmer3();
        ColorDimmer3 lightMetalReindeer = new ColorDimmer3();
        //ColorDimmer3 lightCWindow = new ColorDimmer3();
        //ColorDimmer3 lightLWindow = new ColorDimmer3();
        //ColorDimmer3 lightFrontDoor = new ColorDimmer3();
        //ColorDimmer3 lightBush = new ColorDimmer3();

        MovingHead movingHead = new MovingHead();
        GroupDimmer allLights = new GroupDimmer();

        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();

        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);

        Expander.MidiInput2 midiAkai = new Expander.MidiInput2("LPD8", true);
        Expander.MidiInput2 midiBCF = new Expander.MidiInput2("BCF2000", true);
        Expander.MidiOutput midiBcfOutput = new Expander.MidiOutput("BCF2000", true);
        Controller.Sequence candyCane = new Controller.Sequence();
        Controller.Sequence fatherSeq = new Controller.Sequence();
        Controller.Sequence starwarsCane = new Controller.Sequence();
        Controller.Sequence music1Seq = new Controller.Sequence();
        Controller.Sequence music2Seq = new Controller.Sequence();
        Controller.Sequence backgroundLoop = new Controller.Sequence();
        Controller.Sequence backgroundLights = new Controller.Sequence();
        Subject<bool> inflatablesRunning = new Subject<bool>();

        Import.LorImport2 lorCarolBells = new Import.LorImport2();
        Import.LorImport2 lorCoke = new Import.LorImport2();

        public Xmas2014(IEnumerable<string> args)
        {
            hours.AddRange("5:00 pm", "10:00 pm");
            //            hours.SetForced(true);

            //acnOutput.Muted = true;

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            midiAkai.Controller(midiChannel, 1).Controls(blackOut.Control);
            midiAkai.Controller(midiChannel, 2).Controls(whiteOut.Control);

            midiBCF.Controller(0, 87).Controls(blackOut.Control);
            midiBCF.Controller(0, 88).Controls(whiteOut.Control);

            midiBCF.Controller(0, 91).Controls(x =>
                {
                    if (x.Value == 1)
                    {
                        if (hours.IsOpen)
                        {
                            inflatablesRunning.OnNext(true);
                        }
                    }
                });

            midiBCF.Controller(0, 92).Controls(x =>
                {
                    buttonSnowMachine.Control.OnNext(x.Value == 1);
                });

            blackOut.ConnectTo(x =>
                {
                    midiBcfOutput.Send(0, 87, x.GetByteScale(127));
                });
            whiteOut.ConnectTo(x =>
            {
                midiBcfOutput.Send(0, 88, x.GetByteScale(127));
            });

            inflatablesRunning.Subscribe(x =>
                {
                    airReindeer.Value = x;

                    Exec.SetKey("InflatablesRunning", x.ToString());
                });

            // Read from storage
            inflatablesRunning.OnNext(Exec.GetSetKey("InflatablesRunning", false));

            hours.Output.Log("Hours inside");
            //            movingHead.Pan.Log("Pan");
            //            movingHead.Tilt.Log("Tilt");

            raspberryDarth.Connect(audioDarthVader);
            raspberrySnow.DigitalOutputs[0].Connect(snowMachine);

            packages.Value = true;
            airSnowman.Value = true;
            airR2D2.Value = true;
            airSanta.Value = true;

            hours
                .ControlsMasterPower(packages)
                .ControlsMasterPower(airSnowman)
                .ControlsMasterPower(airR2D2)
                .ControlsMasterPower(airSanta);

            //TODO            airSanta.Follow(hours);

            //midiInput.Controller(midiChannel, 1).Controls(
            //    Observer.Create<double>(x => { allLights.Brightness = x; }));

            //            pulsatingEffect1.ConnectTo(lightStar.GetBrightnessObserver());

            //            pulsatingStar.ConnectTo()

            buttonStartInflatables.ActiveChanged += (o, e) =>
            {
                if (e.NewState && hours.IsOpen)
                {
                    inflatablesRunning.OnNext(true);
                }
            };

            lightRoofEdge.OutputBrightness.Subscribe(x =>
            {
                pixelsRoofEdge.SetAll(lightRoofEdge.Color, x);
            });

            lightRoofEdge.OutputColor.Subscribe(x =>
            {
                pixelsRoofEdge.SetAll(x, lightRoofEdge.Brightness);
            });

            hours.Output.Subscribe(pulsatingEffect1.InputRun);

            hours.Output.Subscribe(x =>
                {
                    packages.Value = x;
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

            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 0, 50), 4, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 50, 100), 5, 1);
            acnOutput.Connect(new Physical.PixelRope(saberPixels, 0, 32), 14, 85);
            acnOutput.Connect(new Physical.PixelRope(saberSidePixels, 0, 18), 14, 1);

            acnOutput.Connect(new Physical.GenericDimmer(airReindeer, 10), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airSnowman, 11), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airSanta, 12), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airR2D2, 13), SacnUniverseDMX);

            acnOutput.Connect(new Physical.GenericDimmer(lightStar, 50), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightBushes, 51), SacnUniverseDMX);

            acnOutput.Connect(new Physical.AmericanDJStrobe(lightWhiteStrobe, 5), SacnUniverseDMX);
            //            acnOutput.Connect(new Physical.SmallRGBStrobe(lightBottom, 1), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightStraigthAhead, 60), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightRightColumn, 80), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightUpTree, 40), SacnUniverseDMX);
            acnOutput.Connect(new Physical.RGBStrobe(lightVader, 70), SacnUniverseDMX);
            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(lightSnow, 20), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 1), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 2), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 3), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 4), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(movingHead, 200), SacnUniverseDMX);

            acnOutput.Connect(new Physical.GenericDimmer(lightNet4, 5), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet3, 6), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet1, 7), SacnUniverseRenard2);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 8), SacnUniverseRenard2);

            acnOutput.Connect(new Physical.GenericDimmer(packages, 1), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(snowmanKaggen, 2), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightR2D2, 3), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 4), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 5), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 6), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightMetalReindeer, 7), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet9, 8), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet10, 9), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet8, 10), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairsLeft, 11), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightFenceLeft, 12), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightFenceMid, 13), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightFenceRight, 14), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairsRight, 15), SacnUniverseRenard1);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet11, 16), SacnUniverseRenard1);

            acnOutput.Connect(new Physical.GenericDimmer(lightSanta, 1), SacnUniverseArduino);
            acnOutput.Connect(new Physical.GenericDimmer(lightSnowman, 2), SacnUniverseArduino);

            stateMachine.ForFromSequence(States.Background, backgroundLights);
            stateMachine.ForFromSequence(States.Music1, music1Seq);
            stateMachine.ForFromSequence(States.Music2, music2Seq);
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
                        stateMachine.SetState(States.DarthVader);
                    }
                });

            oscServer.RegisterActionSimple<int>("/2/push2", (msg, data) =>
            {
                if (data == 1)
                {
                    stateMachine.SetState(States.Music1);
                }
            });

            oscServer.RegisterActionSimple<int>("/2/push3", (msg, data) =>
            {
                if (data == 1)
                {
                    //                    stateMachine.SetState(States.Music2);
                }
            });

            oscServer.RegisterActionSimple<int>("/2/toggle4", (msg, data) =>
            {
                buttonSnowMachine.Control.OnNext(data == 1);
            });

            music1Seq
                .WhenExecuted
                .SetUp(() =>
                {
                    soundOut.Stop();
                    soundOut.WaitForStopped();
                })
                .Execute(instance =>
                {
                    using (var waveCarol = CodecFactory.Instance.GetCodec(@"C:\Projects\Other\ChristmasSounds\trk\09 Carol of the Bells (Instrumental).wav"))
                    {
                        soundOut.Initialize(waveCarol);
                        soundOut.Play();
                        var task = lorCarolBells.Start();
                        task.Wait(instance.CancelToken);

                        instance.WaitFor(S(5));

                        soundOut.Stop();
                        soundOut.WaitForStopped();
                    }

                    instance.WaitFor(S(8));
                })
                .TearDown(() =>
                {
                    lorCarolBells.Stop();
                    soundOut.Stop();
                });

            music2Seq
                .WhenExecuted
                .SetUp(() =>
                {
                    soundOut.Stop();
                    soundOut.WaitForStopped();

                    lightRoofEdge.SetOnlyColor(Color.Red);
                    lightStraigthAhead.SetOnlyColor(Color.Red);
                    lightRightColumn.SetOnlyColor(Color.Red);
                    lightUpTree.SetOnlyColor(Color.Red);
                })
                .Execute(instance =>
                {
                    using (var wave = CodecFactory.Instance.GetCodec(@"C:\Projects\Other\ChristmasSounds\trk\Coca Cola - Holidays Are Coming.wav"))
                    {
                        soundOut.Initialize(wave);
                        soundOut.Play();
                        var task = lorCoke.Start();
                        task.Wait(instance.CancelToken);

                        instance.WaitFor(S(5));

                        soundOut.Stop();
                        soundOut.WaitForStopped();
                    }

                    instance.WaitFor(S(8));
                })
                .TearDown(() =>
                {
                    lorCoke.Stop();
                    soundOut.Stop();
                });

            backgroundLights
                .WhenExecuted
                .SetUp(() =>
                {
                    saberPixels.SetAll(Color.Red, 0.1);
                    pulsatingStar.Start();
                    lightUpTree.SetColor(Color.Red, 0.5);
                    lightHat1.Brightness = 1;
                    lightHat2.Brightness = 1;
                    lightHat3.Brightness = 1;
                    lightHat4.Brightness = 1;
                    snowmanKaggen.Brightness = 1;
                    lightR2D2.Brightness = 1;
                    lightSnowman.Brightness = 1;
                    lightSanta.Brightness = 1;
                    lightNet1.Brightness = 1;
                    lightNet2.Brightness = 1;
                    lightNet3.Brightness = 1;
                    lightNet4.Brightness = 1;
                    lightNet5.Brightness = 1;
                    lightNet6.Brightness = 1;
                    lightNet7.Brightness = 1;
                    lightNet8.Brightness = 1;
                    lightNet9.Brightness = 1;
                    lightNet10.Brightness = 1;
                    lightNet11.Brightness = 1;
                    lightStairsLeft.Brightness = 1;
                    lightFenceLeft.Brightness = 1;
                    lightFenceMid.Brightness = 1;
                    lightFenceRight.Brightness = 1;
                    lightStairsRight.Brightness = 1;
                    lightBushes.Brightness = 1;
                    lightMetalReindeer.Brightness = 1;
                })
                .TearDown(() =>
                {
                    saberPixels.TurnOff();
                    pulsatingStar.Stop();
                    lightUpTree.Brightness = 0;
                    lightHat1.Brightness = 0;
                    lightHat2.Brightness = 0;
                    lightHat3.Brightness = 0;
                    lightHat4.Brightness = 0;
                    snowmanKaggen.Brightness = 0;
                    lightR2D2.Brightness = 0;
                    lightSnowman.Brightness = 0;
                    lightSanta.Brightness = 0;
                    lightNet1.Brightness = 0;
                    lightNet2.Brightness = 0;
                    lightNet3.Brightness = 0;
                    lightNet4.Brightness = 0;
                    lightNet5.Brightness = 0;
                    lightNet6.Brightness = 0;
                    lightNet7.Brightness = 0;
                    lightNet8.Brightness = 0;
                    lightNet9.Brightness = 0;
                    lightNet10.Brightness = 0;
                    lightNet11.Brightness = 0;
                    lightStairsLeft.Brightness = 0;
                    lightFenceLeft.Brightness = 0;
                    lightFenceMid.Brightness = 0;
                    lightFenceRight.Brightness = 0;
                    lightStairsRight.Brightness = 0;
                    lightBushes.Brightness = 0;
                    lightMetalReindeer.Brightness = 0;
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

                            if (i.IsCancellationRequested)
                                break;

                            Exec.ExecuteAndWait(music2Seq);
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

            ImportAndMapCarolBells();
            ImportAndMapCoke();
        }

        private void ImportAndMapCarolBells()
        {
            lorCarolBells.LoadFromFile(@"..\..\..\Test Files\David Foster - Carol of the Bells.lms");

            lorCarolBells.MapDeviceRGB("E - 1", "D# - 2", "D - 3", lightStraigthAhead);
            lorCarolBells.MapDeviceRGB("C# - 4", "C - 5", "B - 6", lightRightColumn);
            //lorCarolBells.MapDeviceRGB("A# - 7", "A - 8", "G# - 9", lightNote3);
            //lorCarolBells.MapDeviceRGB("G - 10", "F# - 11", "F - 12", lightNote4);
            //lorCarolBells.MapDeviceRGB("E - 13", "D# - 14", "D - 15", lightNote5);
            lorCarolBells.MapDeviceRGB("C# - 16", "C - 1", "B - 2", lightUpTree);
            //lorCarolBells.MapDeviceRGB("A# - 3", "A - 4", "G# - 5", lightNote7);
            //lorCarolBells.MapDeviceRGB("G - 6", "F# - 7", "F - 8", lightNote8);
            //lorCarolBells.MapDeviceRGB("E - 9", "D# - 10", "D - 11", lightNote9);
            lorCarolBells.MapDeviceRGB("C# - 12", "C - 13", "B - 14", lightVader);
            //lorCarolBells.MapDevice("A# - 15", lightNote11);
            //lorCarolBells.MapDevice("A - 16", lightNote12);

            lorCarolBells.MapDevice("Sky 1", lightNet1);
            lorCarolBells.MapDevice("Sky 2", lightNet2);
            lorCarolBells.MapDevice("Sky 3", lightNet3);
            lorCarolBells.MapDevice("Sky 4", lightNet4);
            lorCarolBells.MapDevice("Sky 5", lightNet5);

            lorCarolBells.MapDevice("Sky 1", lightNet10);
            lorCarolBells.MapDevice("Sky 2", lightNet9);
            lorCarolBells.MapDevice("Sky 3", lightNet8);
            lorCarolBells.MapDevice("Sky 4", lightNet7);
            lorCarolBells.MapDevice("Sky 5", lightNet6);

            lorCarolBells.MapDevice("Rooftop", snowmanKaggen);

            lorCarolBells.MapDevice("Star1", lightNet11);
            lorCarolBells.MapDevice("Star2", lightStairsLeft);
            lorCarolBells.MapDevice("Star3", lightFenceLeft);
            lorCarolBells.MapDevice("Star extra", lightStar);

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
            lorCarolBells.MapDeviceRGBW("R-Edge R", "R-Edge G", "R-Edge B", "R-Edge W", lightRoofEdge);
            //            lorCarolBells.MapDeviceRGBW("R-Bottom", "G-Bottom", "B-Bottom", "W-Bottom", lightNotUsed1);
            lorCarolBells.MapDeviceRGBW("Garage R", "Garage G", "Garage B", "Garage W", lightWhiteStrobe);
            lorCarolBells.MapDeviceRGBW("Rwindo R", "Rwindo G", "Rwindo B", "Rwindo W", lightMetalReindeer);
            //lorCarolBells.MapDeviceRGBW("Cwindo R", "Cwindo G", "Cwindo B", "Cwindo W", lightCWindow);
            //lorCarolBells.MapDeviceRGBW("Lwindo R", "Lwindo G", "Lwindo B", "Lwindo W", lightLWindow);
            //lorCarolBells.MapDeviceRGBW("Ft door R", "Ft door G", "Ft door B", "FT door W", lightFrontDoor);
            //lorCarolBells.MapDeviceRGBW("Bush - red", "Bush - green", "Bush - blue", "Bush - white", lightBush);

            lorCarolBells.MapDevice("Tree - A", lightSnowman);
            lorCarolBells.MapDevice("Tree - B", lightSanta);

            lorCarolBells.MapDevice("Spoke 1a", lightHat1);
            lorCarolBells.MapDevice("Spoke 2a", lightHat2);
            lorCarolBells.MapDevice("Spoke 3a", lightHat3);
            lorCarolBells.MapDevice("Spoke  4a", lightHat4);
            lorCarolBells.MapDevice("Spoke 5a", lightR2D2);
            lorCarolBells.MapDevice("Spoke 6a", lightNet11);

            lorCarolBells.MapDevice("Spoke 7a", lightStairsLeft);
            lorCarolBells.MapDevice("Spoke 8a", lightFenceLeft);
            lorCarolBells.MapDevice("Spoke 9a", lightFenceMid);
            lorCarolBells.MapDevice("Spoike 10a", lightFenceRight);
            lorCarolBells.MapDevice("Spoke  11a", lightStairsRight);
            lorCarolBells.MapDevice("Spoke  12a", lightNet11);
            lorCarolBells.MapDevice("Spoke  13a", lightBushes);
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


            lorCarolBells.Prepare();
        }

        private void ImportAndMapCoke()
        {
            lorCoke.LoadFromFile(@"..\..\..\Test Files\Coca Cola - Holidays Are Coming.lms");

            lorCoke.MapDevice("Mini Tree 1", lightBushes);
            // lorCoke.MapDevice("House  Red 1", light);
            // lorCoke.MapDevice("House Red 2", light);
            // lorCoke.MapDevice("Porch Railing Red", light);
            // lorCoke.MapDevice("Garage Red ", light);
            // lorCoke.MapDevice("Windows Red ", light);
            // lorCoke.MapDevice("House Clear 1", light);
            // lorCoke.MapDevice("House Clear 2", light);
            // lorCoke.MapDevice("Porch Railing Clear", light);
            // lorCoke.MapDevice("Garage Clear ", light);
            // lorCoke.MapDevice("Windows Clear ", light);
            // lorCoke.MapDevice("Window Candles", light);
            lorCoke.MapDevice("Red Fence 1", snowmanKaggen);
            // lorCoke.MapDevice("Red Fence 2", light);
            // lorCoke.MapDevice("Red Fence 3", light);
            // lorCoke.MapDevice("Red Fence 4", light);
            // lorCoke.MapDevice("Red Fence 5", light);
            // lorCoke.MapDevice("Red Fence 6", light);
            // lorCoke.MapDevice("Red Fence 7", light);
            // lorCoke.MapDevice("Red Fence 8", light);
            // lorCoke.MapDevice("Clear Fence 1", light);
            // lorCoke.MapDevice("Clear Fence 2", light);
            // lorCoke.MapDevice("Clear Fence 3", light);
            // lorCoke.MapDevice("Clear Fence 4", light);
            // lorCoke.MapDevice("Clear Fence 5", light);
            // lorCoke.MapDevice("Clear Fence 6", light);
            // lorCoke.MapDevice("Clear Fence 7", light);
            // lorCoke.MapDevice("Clear Fence 8", light);
            // lorCoke.MapDevice("C", light);
            // lorCoke.MapDevice("H", light);
            // lorCoke.MapDevice("R (2)", light);
            // lorCoke.MapDevice("I", light);
            // lorCoke.MapDevice("S", light);
            // lorCoke.MapDevice("T", light);
            // lorCoke.MapDevice("M (1)", light);
            // lorCoke.MapDevice("A", light);
            // lorCoke.MapDevice("S (1)", light);
            // lorCoke.MapDevice("Tree 1", light);
            // lorCoke.MapDevice("Tree 3", light);
            // lorCoke.MapDevice("Tree 5", light);
            lorCoke.MapDevice("Mega Star 1", lightStar);
            lorCoke.MapDevice("Mega Star 2", lightR2D2);
            // lorCoke.MapDevice("Mega Star 3", light);
            // lorCoke.MapDevice("Mega Star 4", light);
            lorCoke.MapDevice("Snow Flake 1", lightUpTree);
            lorCoke.MapDevice("Snow Flake 2", lightRightColumn);
            lorCoke.MapDevice("Snow Flake 3", lightVader);
            // lorCoke.MapDevice("Christmas Sign", light);
            // lorCoke.MapDevice("Nativity Scene", light);
            lorCoke.MapDevice("Cross", lightSanta);
            lorCoke.MapDevice("Santa Wireframe", lightMetalReindeer);
            // lorCoke.MapDevice("Santa Claus Blowmold", light);
            // lorCoke.MapDevice("Snowman Ropelight", light);
            // lorCoke.MapDevice("Snowman Wireframe", light);
            // lorCoke.MapDevice("Believe Sign", light);
            lorCoke.MapDevice("Archway", lightSnowman);
            lorCoke.MapDevice("Horse and Sleigh", lightWhiteStrobe);
            // lorCoke.MapDevice("Back Porch Trees", light);
            // lorCoke.MapDevice("Red Spheres", light);
            // lorCoke.MapDevice("Blue Spheres", light);
            // lorCoke.MapDevice("Train Engine", light);
            // lorCoke.MapDevice("Train Cart 1", light);
            // lorCoke.MapDevice("Train Caboose", light);
            // lorCoke.MapDevice("Train Wheels", light);
            // lorCoke.MapDevice("Pole 1-1", light);
            // lorCoke.MapDevice("Pole 1-2", light);
            // lorCoke.MapDevice("Pole 1-3", light);
            // lorCoke.MapDevice("Pole 1-4", light);
            // lorCoke.MapDevice("Pole 1-5", light);
            // lorCoke.MapDevice("Pole 1-6", light);
            // lorCoke.MapDevice("Pole 1-7", light);
            // lorCoke.MapDevice("Pole 2-1", light);
            // lorCoke.MapDevice("Pole 2-2", light);
            // lorCoke.MapDevice("Pole 2-3", light);
            // lorCoke.MapDevice("Pole 2-4", light);
            // lorCoke.MapDevice("Pole 2-5", light);
            // lorCoke.MapDevice("Pole 2-6", light);
            // lorCoke.MapDevice("Pole 2-7", light);
            // lorCoke.MapDevice("Pole 3-1", light);
            // lorCoke.MapDevice("Pole 3-2", light);
            // lorCoke.MapDevice("Pole 3-3", light);
            // lorCoke.MapDevice("Pole 3-4", light);
            // lorCoke.MapDevice("Pole 3-5", light);
            // lorCoke.MapDevice("Pole 3-6", light);
            // lorCoke.MapDevice("Pole 3-7", light);
            // lorCoke.MapDevice("Pole 4-1", light);
            // lorCoke.MapDevice("Pole 4-2", light);
            // lorCoke.MapDevice("Pole 4-3", light);
            // lorCoke.MapDevice("Pole 4-4", light);
            // lorCoke.MapDevice("Pole 4-5", light);
            // lorCoke.MapDevice("Pole 4-6", light);
            // lorCoke.MapDevice("Pole 4-7", light);
            // lorCoke.MapDevice("Pole 5-1", light);
            // lorCoke.MapDevice("Pole 5-2", light);
            // lorCoke.MapDevice("Pole 5-3", light);
            // lorCoke.MapDevice("Pole 5-4", light);
            // lorCoke.MapDevice("Pole 5-5", light);
            // lorCoke.MapDevice("Pole 5-6", light);
            // lorCoke.MapDevice("Pole 5-7", light);
            lorCoke.MapDevice("Red Candy 1", lightSnowman);
            // lorCoke.MapDevice("Red Candy 2", light);
            // lorCoke.MapDevice("Red Candy 3", light);
            // lorCoke.MapDevice("Red Candy 4", light);
            // lorCoke.MapDevice("Red Candy 5", light);
            // lorCoke.MapDevice("Red Candy 6", light);
            // lorCoke.MapDevice("Red Candy 7", light);
            // lorCoke.MapDevice("Red Candy 8", light);
            lorCoke.MapDevice("Red Candy 9", lightSanta);
            // lorCoke.MapDevice("Red Candy 10", light);
            // lorCoke.MapDevice("Red Candy 11", light);
            // lorCoke.MapDevice("Red Candy 12", light);
            // lorCoke.MapDevice("Red Candy 13", light);
            // lorCoke.MapDevice("Red Candy 14", light);
            // lorCoke.MapDevice("Red Candy 15", light);
            // lorCoke.MapDevice("Red Candy 16", light);
            // lorCoke.MapDevice("Spinner 1-1", light);
            // lorCoke.MapDevice("Spinner 1-2", light);
            // lorCoke.MapDevice("Spinner 1-3", light);
            // lorCoke.MapDevice("Spinner 1-4", light);
            // lorCoke.MapDevice("Spinner 1-5", light);
            // lorCoke.MapDevice("Spinner 1-6", light);
            // lorCoke.MapDevice("Spinner 1-7", light);
            // lorCoke.MapDevice("Spinner 1-8", light);
            // lorCoke.MapDevice("Spinner 2-1", light);
            // lorCoke.MapDevice("Spinner 2-2", light);
            // lorCoke.MapDevice("Spinner 2-3", light);
            // lorCoke.MapDevice("Spinner 2-4", light);
            // lorCoke.MapDevice("Spinner 2-5", light);
            // lorCoke.MapDevice("Spinner 2-6", light);
            // lorCoke.MapDevice("Spinner 2-7", light);
            // lorCoke.MapDevice("Spinner 2-8", light);
            // lorCoke.MapDevice("Spinner 3-1", light);
            // lorCoke.MapDevice("Spinner 3-2", light);
            // lorCoke.MapDevice("Spinner 3-3", light);
            // lorCoke.MapDevice("Spinner 3-4", light);
            // lorCoke.MapDevice("Spinner 3-5", light);
            // lorCoke.MapDevice("Spinner 3-6", light);
            // lorCoke.MapDevice("Spinner 3-7", light);
            // lorCoke.MapDevice("Spinner 3-8", light);
            // lorCoke.MapDevice("Spinner 4-1", light);
            // lorCoke.MapDevice("Spinner 4-2", light);
            // lorCoke.MapDevice("Spinner 4-3", light);
            // lorCoke.MapDevice("Spinner 4-4", light);
            // lorCoke.MapDevice("Spinner 4-5", light);
            // lorCoke.MapDevice("Spinner 4-6", light);
            // lorCoke.MapDevice("Spinner 4-7", light);
            // lorCoke.MapDevice("Spinner 4-8", light);
            // lorCoke.MapDevice("Spinner 1-1 (1)", light);
            // lorCoke.MapDevice("Spinner 1-2 (1)", light);
            // lorCoke.MapDevice("Spinner 1-3 (1)", light);
            // lorCoke.MapDevice("Spinner 1-4 (1)", light);
            // lorCoke.MapDevice("Spinner 2-1 (1)", light);
            // lorCoke.MapDevice("Spinner 2-2 (1)", light);
            // lorCoke.MapDevice("Spinner 2-3 (1)", light);
            // lorCoke.MapDevice("Spinner 2-4 (1)", light);
            // lorCoke.MapDevice("Spinner 3-1 (1)", light);
            // lorCoke.MapDevice("Spinner 3-2 (1)", light);
            // lorCoke.MapDevice("Spinner 3-3 (1)", light);
            // lorCoke.MapDevice("Spinner 3-4 (1)", light);
            // lorCoke.MapDevice("Spinner 4-1 (1)", light);
            // lorCoke.MapDevice("Spinner 4-2 (1)", light);
            // lorCoke.MapDevice("Spinner 4-3 (1)", light);
            // lorCoke.MapDevice("Spinner 4-4 (1)", light);
            // lorCoke.MapDevice("Spinner 5-1", light);
            // lorCoke.MapDevice("Spinner 5-2", light);
            // lorCoke.MapDevice("Spinner 5-3", light);
            // lorCoke.MapDevice("Spinner 5-4", light);
            // lorCoke.MapDevice("Spinner 6-1", light);
            // lorCoke.MapDevice("Spinner 6-2", light);
            // lorCoke.MapDevice("Spinner 6-3", light);
            // lorCoke.MapDevice("Spinner 6-4", light);
            // lorCoke.MapDevice("Spinner 7-1", light);
            // lorCoke.MapDevice("Spinner 7-2", light);
            // lorCoke.MapDevice("Spinner 7-3", light);
            // lorCoke.MapDevice("Spinner 7-4", light);
            // lorCoke.MapDevice("Spinner 8-1", light);
            // lorCoke.MapDevice("Spinner 8-2", light);
            // lorCoke.MapDevice("Spinner 8-3", light);
            // lorCoke.MapDevice("Spinner 8-4", light);
            // lorCoke.MapDevice("Deer 1", light);
            // lorCoke.MapDevice("Deer 3", light);
            // lorCoke.MapDevice("Deer 4", light);
            // lorCoke.MapDevice("Deer 5", light);
            // lorCoke.MapDevice("Deer 6", light);
            // lorCoke.MapDevice("Deer 7", light);
            // lorCoke.MapDevice("Deer 8", light);
            lorCoke.MapDevice("Creek 1", lightNet1);
            lorCoke.MapDevice("Creek 2", lightNet2);
            lorCoke.MapDevice("Creek 3", lightNet3);
            lorCoke.MapDevice("Creek 4", lightNet4);
            lorCoke.MapDevice("Creek 5", lightNet5);
            lorCoke.MapDevice("Creek 6", lightNet6);
            lorCoke.MapDevice("Creek 7", lightNet7);
            lorCoke.MapDevice("Creek 8", lightNet8);
            lorCoke.MapDevice("Creek 9", lightNet9);
            lorCoke.MapDevice("Creek 10", lightNet10);
            lorCoke.MapDevice("Creek 11", lightNet11);
            // lorCoke.MapDevice("Creek 12", light);
            // lorCoke.MapDevice("Creek 13", light);
            // lorCoke.MapDevice("Creek 14", light);
            // lorCoke.MapDevice("Creek 15", light);
            // lorCoke.MapDevice("Creek 16", light);
            // lorCoke.MapDevice("Mini Tree 1", light);
            // lorCoke.MapDevice("Mini Tree 2", light);
            // lorCoke.MapDevice("Mini Tree 3", light);
            // lorCoke.MapDevice("Mini Tree 4", light);
            // lorCoke.MapDevice("Mini Tree 5", light);
            // lorCoke.MapDevice("Mini Tree 6", light);
            // lorCoke.MapDevice("Mini Tree 7", light);
            // lorCoke.MapDevice("Mini Tree 8 ", light);
            // lorCoke.MapDevice("Mini Tree 9", light);
            // lorCoke.MapDevice("Mini Tree 10", light);
            // lorCoke.MapDevice("Mini Tree 11", light);
            // lorCoke.MapDevice("Mini Tree 12", light);
            // lorCoke.MapDevice("Mini Tree 13", light);
            // lorCoke.MapDevice("Mini Tree 14", light);
            // lorCoke.MapDevice("Mini Tree 15", light);
            // lorCoke.MapDevice("Mini Tree 16", light);
            // lorCoke.MapDevice("Mini Tree 1 (1)", light);
            // lorCoke.MapDevice("Mini Tree 2 ", light);
            // lorCoke.MapDevice("Mini Tree 3 (1)", light);
            // lorCoke.MapDevice("Mini Tree 4 (1)", light);
            // lorCoke.MapDevice("Mini Tree 5 (1)", light);
            // lorCoke.MapDevice("Mini Tree 6 (1)", light);
            // lorCoke.MapDevice("Mini Tree 7 (1)", light);
            // lorCoke.MapDevice("Mini Tree 8", light);
            // lorCoke.MapDevice("Mini Tree 9 (1)", light);
            // lorCoke.MapDevice("Mini Tree 10 (1)", light);
            // lorCoke.MapDevice("Mini Tree 11 (1)", light);
            // lorCoke.MapDevice("Mini Tree 12 ", light);
            // lorCoke.MapDevice("Mini Tree 13  ", light);
            // lorCoke.MapDevice("Mini Tree 14 ", light);
            // lorCoke.MapDevice("Mini Tree 15 ", light);
            // lorCoke.MapDevice("Mini Tree 16 (1)", light);
            // lorCoke.MapDevice("Mini Tree 1 (2)", light);
            // lorCoke.MapDevice("Mini Tree 2 (1)", light);
            // lorCoke.MapDevice("Mini Tree 3 (2)", light);
            // lorCoke.MapDevice("Mini Tree 4 (2)", light);
            // lorCoke.MapDevice("Mini Tree 5 (2)", light);
            // lorCoke.MapDevice("Mini Tree 6 (2)", light);
            // lorCoke.MapDevice("Mini Tree 7 (2)", light);
            // lorCoke.MapDevice("Mini Tree 8 (1)", light);
            // lorCoke.MapDevice("Mini Tree 9 (2)", light);
            // lorCoke.MapDevice("Mini Tree 10 (2)", light);
            // lorCoke.MapDevice("Mini Tree 11 (2)", light);
            // lorCoke.MapDevice("Mini Tree 12 (1)", light);
            // lorCoke.MapDevice("Mini Tree 13 (1)", light);
            // lorCoke.MapDevice("Mini Tree 14 (1)", light);
            // lorCoke.MapDevice("Mini Tree 15 (1)", light);
            // lorCoke.MapDevice("Mini Tree 16 (2)", light);
            // lorCoke.MapDevice("Red 1", light);
            // lorCoke.MapDevice("Red 2", light);
            // lorCoke.MapDevice("Red 3", light);
            // lorCoke.MapDevice("Red 4", light);
            // lorCoke.MapDevice("Red 5", light);
            // lorCoke.MapDevice("Red 6", light);
            // lorCoke.MapDevice("Red 7", light);
            // lorCoke.MapDevice("Red 8", light);
            // lorCoke.MapDevice("Red 9", light);
            // lorCoke.MapDevice("Red 10", light);
            // lorCoke.MapDevice("Red 11", light);
            // lorCoke.MapDevice("Red 12", light);
            // lorCoke.MapDevice("White 1", light);
            // lorCoke.MapDevice("White 2", light);
            // lorCoke.MapDevice("White 3", light);
            // lorCoke.MapDevice("White 4", light);
            // lorCoke.MapDevice("White 5", light);
            // lorCoke.MapDevice("White 6", light);
            // lorCoke.MapDevice("White 7", light);
            // lorCoke.MapDevice("White 8", light);
            // lorCoke.MapDevice("White 9", light);
            // lorCoke.MapDevice("White 10", light);
            // lorCoke.MapDevice("White 11", light);
            // lorCoke.MapDevice("White 12", light);
            lorCoke.MapDevice("White 1 (1)", lightStraigthAhead);
            lorCoke.MapDevice("White 2 (1)", lightStairsLeft);
            lorCoke.MapDevice("White 3 (1)", lightFenceLeft);
            lorCoke.MapDevice("White 4 (1)", lightFenceMid);
            lorCoke.MapDevice("White 5 (1)", lightFenceRight);
            lorCoke.MapDevice("White 6 (1)", lightStairsRight);
            // lorCoke.MapDevice("White 7 (1)", light);
            // lorCoke.MapDevice("White 8 (1)", light);
            // lorCoke.MapDevice("White 9 (1)", light);
            // lorCoke.MapDevice("White 10 (1)", light);
            // lorCoke.MapDevice("White 11 (1)", light);
            // lorCoke.MapDevice("White 12 (1)", light);
            // lorCoke.MapDevice("Red 1 (2)", light);
            // lorCoke.MapDevice("Red 2 (2)", light);
            // lorCoke.MapDevice("Red 3 (2)", light);
            // lorCoke.MapDevice("Red 4 (2)", light);
            // lorCoke.MapDevice("Red 5 (2)", light);
            // lorCoke.MapDevice("Red 6 (2)", light);
            // lorCoke.MapDevice("Red 7 (2)", light);
            // lorCoke.MapDevice("Red 8 (2)", light);
            // lorCoke.MapDevice("Red 9 (2)", light);
            // lorCoke.MapDevice("Red 10 (2)", light);
            // lorCoke.MapDevice("Red 11 (2)", light);
            // lorCoke.MapDevice("Red 12 (2)", light);
            lorCoke.MapDevice("Clear 1", lightHat1);
            lorCoke.MapDevice("Clear 2", lightHat2);
            lorCoke.MapDevice("Clear 3", lightHat3);
            lorCoke.MapDevice("Clear 4", lightHat4);
            // lorCoke.MapDevice("Clear 5", light);
            // lorCoke.MapDevice("Clear 6", light);
            // lorCoke.MapDevice("Clear 7", light);
            // lorCoke.MapDevice("Clear 8", light);
            // lorCoke.MapDevice("Clear 9", light);
            // lorCoke.MapDevice("Clear 10", light);
            // lorCoke.MapDevice("Clear 11", light);
            // lorCoke.MapDevice("Clear 12", light);
            lorCoke.MapDevice("Red 1 (3)", lightRoofEdge);
            // lorCoke.MapDevice("Red 2 (3)", light);
            // lorCoke.MapDevice("Red 3 (3)", light);
            // lorCoke.MapDevice("Red 4 (3)", light);
            // lorCoke.MapDevice("Red 5 (3)", light);
            // lorCoke.MapDevice("Red 6 (3)", light);
            // lorCoke.MapDevice("Red 7 (3)", light);
            // lorCoke.MapDevice("Red 8 (3)", light);
            // lorCoke.MapDevice("Red 9 (3)", light);
            // lorCoke.MapDevice("Red 10 (3)", light);
            // lorCoke.MapDevice("Red 11 (3)", light);
            // lorCoke.MapDevice("Red 12 (3)", light);
            // lorCoke.MapDevice("Clear 1 (1)", light);
            // lorCoke.MapDevice("Clear 2 (1)", light);
            // lorCoke.MapDevice("Clear 3 (1)", light);
            // lorCoke.MapDevice("Clear 4 (1)", light);
            // lorCoke.MapDevice("Clear 5 (1)", light);
            // lorCoke.MapDevice("Clear 6 (1)", light);
            // lorCoke.MapDevice("Clear 7 (1)", light);
            // lorCoke.MapDevice("Clear 8 (1)", light);
            // lorCoke.MapDevice("Clear 9 (1)", light);
            // lorCoke.MapDevice("Clear 10 (1)", light);
            // lorCoke.MapDevice("Clear 11 (1)", light);
            // lorCoke.MapDevice("Clear 12 (1)", light);


            lorCoke.Prepare();
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

                for (int sab = 00; sab < 32; sab++)
                {
                    saberPixels.Inject(Color.Red, 0.5);
                    Exec.Sleep(S(0.01));
                }
                Exec.Sleep(S(1));

                saberPixels.SetAll(Color.Black, 0);
                //                stateMachine.SetState(States.Music2);
            });

            buttonSnowMachine.Output.Subscribe(x =>
                {
                    lightSnow.SetColor(Color.White, x ? 1.0 : 0.0);

                    snowMachine.Value = x;

                    midiBcfOutput.Send(0, 92, (byte)(x ? 127 : 0));
                });

            snowMachine.Output.Subscribe(x =>
            {
                // TODO: Turn off after X period of time
            });

            buttonTest2.Output.Subscribe(x =>
                {
                    if (!x)
                        return;

                    stateMachine.SetState(States.Music1);
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
