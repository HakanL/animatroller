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

namespace Animatroller.Scenes
{
    internal class Halloween2015Manual : BaseScene
    {
        private const int midiChannel = 0;

        private Expander.MidiInput2 midiInput = new Expander.MidiInput2("LPD8");
        private Expander.OscServer oscServer = new Expander.OscServer();
        private AudioPlayer audioCat = new AudioPlayer();
        private AudioPlayer audioMain = new AudioPlayer();
        private AudioPlayer audioPop = new AudioPlayer();
        private AudioPlayer audioDIN = new AudioPlayer();
        private VideoPlayer video3dfx = new VideoPlayer();
        private VideoPlayer video2 = new VideoPlayer();
        private Expander.Raspberry raspberryCat = new Expander.Raspberry("192.168.240.115:5005", 3333);
        private Expander.Raspberry raspberry3dfx = new Expander.Raspberry("192.168.240.226:5005", 3334);
        private Expander.Raspberry raspberryLocal = new Expander.Raspberry("127.0.0.1:5005", 3339);
        private Expander.Raspberry raspberryPop = new Expander.Raspberry("192.168.240.123:5005", 3335);
        private Expander.Raspberry raspberryDIN = new Expander.Raspberry("192.168.240.127:5005", 3337);
        private Expander.Raspberry raspberryVideo2 = new Expander.Raspberry("192.168.240.124:5005", 3336);
        private Expander.OscClient touchOSC = new Expander.OscClient("192.168.240.163", 9000);

        private VirtualPixel1D pixelsRoofEdge = new VirtualPixel1D(150);
        private AnalogInput2 faderR = new AnalogInput2(persistState: true);
        private AnalogInput2 faderG = new AnalogInput2(persistState: true);
        private AnalogInput2 faderB = new AnalogInput2(persistState: true);
        private AnalogInput2 faderBright = new AnalogInput2(persistState: true);
        private DigitalInput2 manualFader = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        private Effect.Flicker flickerEffect = new Effect.Flicker(0.4, 0.6, false);

        private DigitalOutput2 spiderCeiling = new DigitalOutput2("Spider Ceiling");
        private DigitalOutput2 spiderCeilingDrop = new DigitalOutput2("Spider Ceiling Drop");
        private DigitalInput2 catMotion = new DigitalInput2();
        private DigitalInput2 firstBeam = new DigitalInput2();
        private DigitalInput2 finalBeam = new DigitalInput2();
        private DigitalInput2 motion2 = new DigitalInput2();
        private Expander.AcnStream acnOutput = new Expander.AcnStream();
        private DigitalOutput2 catAir = new DigitalOutput2(initial: true);
        private DigitalOutput2 fog = new DigitalOutput2();
        private DigitalOutput2 candyEyes = new DigitalOutput2();
        private DigitalOutput2 catLights = new DigitalOutput2();
        private DigitalOutput2 george1 = new DigitalOutput2();
        private DigitalOutput2 george2 = new DigitalOutput2();
        private DigitalOutput2 popper = new DigitalOutput2();
        private DigitalOutput2 dropSpiderEyes = new DigitalOutput2();

        private OperatingHours2 hoursSmall = new OperatingHours2("Hours Small");

        private StrobeColorDimmer2 spiderLight = new StrobeColorDimmer2("Spider");
        private StrobeColorDimmer2 wall1Light = new StrobeColorDimmer2("Wall 1");
        private StrobeColorDimmer2 wall2Light = new StrobeColorDimmer2("Wall 2");
        private StrobeColorDimmer2 wall3Light = new StrobeColorDimmer2("Wall 3");
        private StrobeColorDimmer2 wall4Light = new StrobeColorDimmer2("Wall 4");
        private Dimmer2 stairs1Light = new Dimmer2("Stairs 1");
        private Dimmer2 stairs2Light = new Dimmer2("Stairs 2");
        private StrobeDimmer strobeDimmer = new StrobeDimmer("ADJ Flash");
        private StrobeColorDimmer2 pinSpot = new StrobeColorDimmer2("Pin Spot");

        private Controller.Sequence catSeq = new Controller.Sequence();
        private Controller.Sequence welcomeSeq = new Controller.Sequence();
        private Controller.Sequence motionSeq = new Controller.Sequence();

        public Halloween2015Manual(IEnumerable<string> args)
        {
            hoursSmall.AddRange("5:00 pm", "9:00 pm");

            // Logging
            hoursSmall.Output.Log("Hours small");

            hoursSmall
                .ControlsMasterPower(catAir);
            //                .ControlsMasterPower(eyes);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    hoursSmall.SetForced(true);
                else
                    hoursSmall.SetForced(null);
            });

            hoursSmall.Output.Subscribe(x =>
            {
                if (x)
                    flickerEffect.Start();
                else
                    flickerEffect.Stop();
                SetPixelColor();
            });

            flickerEffect.ConnectTo(stairs1Light.InputBrightness);
            flickerEffect.ConnectTo(stairs2Light.InputBrightness);


            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 0, 50), 6, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 50, 100), 5, 1);

            acnOutput.Connect(new Physical.SmallRGBStrobe(spiderLight, 1), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall1Light, 60), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall2Light, 70), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall3Light, 40), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall4Light, 80), 1);
            acnOutput.Connect(new Physical.GenericDimmer(stairs1Light, 50), 1);
            acnOutput.Connect(new Physical.GenericDimmer(stairs2Light, 51), 1);
            acnOutput.Connect(new Physical.AmericanDJStrobe(strobeDimmer, 100), 1);
            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(pinSpot, 20), 1);


            raspberryCat.DigitalInputs[4].Connect(catMotion, false);
            raspberryCat.DigitalInputs[5].Connect(firstBeam, false);
            raspberryCat.DigitalInputs[6].Connect(finalBeam, false);
            raspberryCat.DigitalOutputs[7].Connect(spiderCeilingDrop);
            raspberryCat.Connect(audioCat);
            raspberryLocal.Connect(audioMain);
            raspberry3dfx.Connect(video3dfx);
            raspberryVideo2.Connect(video2);
            raspberryPop.Connect(audioPop);
            raspberryDIN.Connect(audioDIN);
            raspberryDIN.DigitalInputs[4].Connect(motion2);
            raspberryCat.DigitalOutputs[6].Connect(fog);
            raspberryDIN.DigitalOutputs[1].Connect(candyEyes);
            raspberryPop.DigitalOutputs[7].Connect(george1);
            raspberryPop.DigitalOutputs[6].Connect(george2);
            raspberryPop.DigitalOutputs[5].Connect(popper);
            raspberryPop.DigitalOutputs[2].Connect(dropSpiderEyes);

            acnOutput.Connect(new Physical.GenericDimmer(catAir, 10), 1);
            acnOutput.Connect(new Physical.GenericDimmer(catLights, 11), 1);

            oscServer.RegisterAction<int>("/1/push1", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push2", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push3", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push4", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
            });


            oscServer.RegisterAction<int>("/1/push5", (msg, data) =>
            {
                george1.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push6", (msg, data) =>
            {
                george2.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push7", (msg, data) =>
            {
                popper.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push8", d => d.First() != 0, (msg, data) =>
            {
                audioPop.PlayEffect("laugh.wav", 1.0, 0.0);
            });

            oscServer.RegisterAction<int>("/1/spiderEyes", (msg, data) =>
            {
                dropSpiderEyes.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push10", d => d.First() != 0, (msg, data) =>
            {
                audioPop.PlayEffect("348 Spider Hiss.wav", 0.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push11", (msg, data) =>
            {
                spiderCeilingDrop.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push12", (msg, data) =>
            {
                fog.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push13", (msg, data) =>
            {
                candyEyes.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push14", (msg, data) =>
            {
                //                flickerEffect.Start();
                spiderLight.Color = Color.Red;
                spiderLight.Brightness = data.First();
                pinSpot.Color = Color.Purple;
                pinSpot.Brightness = data.First();
                strobeDimmer.Brightness = data.First();
                wall1Light.Color = Color.Purple;
                wall1Light.Brightness = data.First();
                wall2Light.Color = Color.Purple;
                wall2Light.Brightness = data.First();
                wall3Light.Color = Color.Purple;
                wall3Light.Brightness = data.First();
                wall4Light.Color = Color.Purple;
                wall4Light.Brightness = data.First();
                //                audioDIN.PlayEffect("gollum_precious1.wav");
            });

            oscServer.RegisterAction<int>("/1/push20", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Siren_ComeHither_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/1/push21", d => d.First() != 0, (msg, data) =>
            {
                //               video2.PlayVideo("SkeletonSurprise_Door_Horz_HD.mp4");
                video2.PlayVideo("Beauty_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/1/push22", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("FearTheReaper_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/1/push23", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("SkeletonSurprise_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/1/push24", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("DancingDead_Wall_HD.mp4");
            });


            oscServer.RegisterAction("/1", msg =>
            {
                this.log.Information("Page 1");
                manualFader.Value = false;

                SetPixelColor();
            });

            oscServer.RegisterAction("/2", msg =>
            {
                this.log.Information("Page 2");
                manualFader.Value = true;

                SetPixelColor();
            });

            oscServer.RegisterAction<float>("/2/faderBright", (msg, data) =>
            {
                faderBright.Value = data.First();

                SetPixelColor();
            });

            oscServer.RegisterAction<float>("/2/faderR", (msg, data) =>
            {
                faderR.Value = data.First();

                SetPixelColor();
            });

            oscServer.RegisterAction<float>("/2/faderG", (msg, data) =>
            {
                faderG.Value = data.First();

                SetPixelColor();
            });

            oscServer.RegisterAction<float>("/2/faderB", (msg, data) =>
            {
                faderB.Value = data.First();

                SetPixelColor();
            });



            midiInput.Note(midiChannel, 36).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
            });
            midiInput.Note(midiChannel, 37).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
            });
            midiInput.Note(midiChannel, 38).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
            });
            midiInput.Note(midiChannel, 39).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
            });
            midiInput.Note(midiChannel, 40).Subscribe(x =>
            {
                if (x)
                    audioMain.PlayBackground();
            });
            midiInput.Note(midiChannel, 41).Subscribe(x =>
            {
                if (x)
                    audioMain.PauseBackground();
            });
            midiInput.Note(midiChannel, 42).Subscribe(x =>
            {
                spiderCeilingDrop.Value = x;
            });

            //            catMotion.Output.Subscribe(catLights.Control);

            catMotion.Output.Subscribe(x =>
            {
                if (x && hoursSmall.IsOpen)
                    Executor.Current.Execute(catSeq);

                touchOSC.Send("/1/led1", x ? 1 : 0);
            });

            firstBeam.Output.Subscribe(x =>
            {
                touchOSC.Send("/1/led2", x ? 1 : 0);

                //if (x)
                //    Executor.Current.Execute(welcomeSeq);
            });

            finalBeam.Output.Subscribe(x =>
            {
                touchOSC.Send("/1/led3", x ? 1 : 0);
            });

            motion2.Output.Subscribe(x =>
            {
                if (x && hoursSmall.IsOpen)
                    Executor.Current.Execute(motionSeq);

                touchOSC.Send("/1/led4", x ? 1 : 0);
            });

            welcomeSeq.WhenExecuted
                .Execute(i =>
                {
                    audioPop.PlayEffect("100471__robinhood76__01886-welcome-spell.wav");

                    i.WaitFor(S(3));
                });

            catSeq.WhenExecuted
                .Execute(instance =>
                {
                    var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                    catLights.Value = true;

                    while (true)
                    {
                        switch (random.Next(4))
                        {
                            case 0:
                                audioCat.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.0));
                                break;
                            case 1:
                                audioCat.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                            case 2:
                                audioCat.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.5));
                                break;
                            case 3:
                                audioCat.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
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
                .TearDown(() =>
                {
                    catLights.Value = false;
                });

            motionSeq.WhenExecuted
                .Execute(instance =>
                {
                    //video2.PlayVideo("DancingDead_Wall_HD.mp4");

                    instance.WaitFor(S(10));
                })
                .TearDown(() =>
                {
                });

            SetPixelColor();
        }

        private Color GetFaderColor()
        {
            return HSV.ColorFromRGB(faderR.Value, faderG.Value, faderB.Value);
        }

        private void SetPixelColor()
        {
            if (manualFader.Value)
                pixelsRoofEdge.SetAll(GetFaderColor(), faderBright.Value);
            else
            {
                if (hoursSmall.IsOpen)
                {
                    pixelsRoofEdge.SetAll(
                        HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                        0.16470588235294117);
                }
                else
                {
                    pixelsRoofEdge.SetAll(Color.Black, 0.0);
                }
            }
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
