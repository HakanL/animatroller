﻿using System;
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

namespace Animatroller.SceneRunner
{
    internal class Halloween2015 : BaseScene
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
        private AnalogInput3 faderR = new AnalogInput3(persistState: true);
        private AnalogInput3 faderG = new AnalogInput3(persistState: true);
        private AnalogInput3 faderB = new AnalogInput3(persistState: true);
        private AnalogInput3 faderBright = new AnalogInput3(persistState: true);
        private DigitalInput2 manualFader = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        private IControlToken testToken = null;
        private Effect.Flicker flickerEffect = new Effect.Flicker(0.4, 0.6, false);
        private Effect.PopOut2 popOut1 = new Effect.PopOut2(S(0.3));
        private Effect.PopOut2 popOut2 = new Effect.PopOut2(S(0.3));
        private Effect.PopOut2 popOut3 = new Effect.PopOut2(S(0.5));
        private Effect.PopOut2 popOut4 = new Effect.PopOut2(S(1.2));

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

        private StrobeColorDimmer3 spiderLight = new StrobeColorDimmer3("Spider");
        private StrobeColorDimmer3 wall1Light = new StrobeColorDimmer3("Wall 1");
        private StrobeColorDimmer3 wall2Light = new StrobeColorDimmer3("Wall 2");
        private StrobeColorDimmer3 wall3Light = new StrobeColorDimmer3("Wall 3");
        private StrobeColorDimmer3 wall4Light = new StrobeColorDimmer3("Wall 4");
        private Dimmer3 stairs1Light = new Dimmer3("Stairs 1");
        private Dimmer3 stairs2Light = new Dimmer3("Stairs 2");
        private StrobeDimmer3 underGeorge = new StrobeDimmer3("ADJ Flash");
        private StrobeColorDimmer3 pinSpot = new StrobeColorDimmer3("Pin Spot");

        private Controller.Sequence catSeq = new Controller.Sequence();
        private Controller.Sequence welcomeSeq = new Controller.Sequence();
        private Controller.Sequence motionSeq = new Controller.Sequence();

        private Controller.Timeline<string> timelineThunder1 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder2 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder3 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder4 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder5 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder6 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder7 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder8 = new Controller.Timeline<string>(1);

        public Halloween2015(IEnumerable<string> args)
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

            popOut1.ConnectTo(wall1Light);
            popOut2.ConnectTo(wall2Light);
            popOut3.ConnectTo(wall3Light);
            popOut4.ConnectTo(wall4Light);
            popOut4.ConnectTo(underGeorge);

            flickerEffect.ConnectTo(stairs1Light);
            flickerEffect.ConnectTo(stairs2Light);


            raspberryLocal.AudioTrackStart.Subscribe(x =>
            {
                // Next track
                switch (x)
                {
                    case "Thunder1.wav":
                        timelineThunder1.Start();
                        break;

                    case "Thunder2.wav":
                        timelineThunder2.Start();
                        break;

                    case "Thunder3.wav":
                        timelineThunder3.Start();
                        break;

                    case "Thunder4.wav":
                        timelineThunder4.Start();
                        break;

                    case "Thunder5.wav":
                        timelineThunder5.Start();
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

            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 0, 50), 6, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 50, 100), 5, 1);

            acnOutput.Connect(new Physical.SmallRGBStrobe(spiderLight, 1), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall1Light, 60), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall2Light, 70), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall3Light, 40), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall4Light, 80), 1);
            acnOutput.Connect(new Physical.GenericDimmer(stairs1Light, 50), 1);
            acnOutput.Connect(new Physical.GenericDimmer(stairs2Light, 51), 1);
            acnOutput.Connect(new Physical.AmericanDJStrobe(underGeorge, 100), 1);
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

            oscServer.RegisterAction<int>("/1/push13", d => d.First() != 0, (msg, data) =>
            {
                //                Exec.MasterEffect.Fade(stairs1Light, 1.0, 0.0, 2000, token: testToken);
                //popOut1.Pop();
                //popOut2.Pop();
                //popOut3.Pop();
                popOut4.Pop();
            });

            oscServer.RegisterAction<int>("/1/toggle1", (msg, data) =>
            {
                //                candyEyes.Value = data.First() != 0;
                if (data.First() != 0)
                    audioMain.PlayBackground();
                else
                    audioMain.PauseBackground();
            });

            oscServer.RegisterAction<int>("/1/toggle2", (msg, data) =>
            {
                underGeorge.Brightness = data.First();
            });

            oscServer.RegisterAction<int>("/1/toggle3", (msg, data) =>
            {
                if (data.First() != 0)
                    flickerEffect.Start();
                else
                    flickerEffect.Stop();
            });

            oscServer.RegisterAction<int>("/1/toggle4", (msg, data) =>
            {
                if (data.First() != 0)
                {
                    testToken = stairs1Light.TakeControl(5);
                }
                else
                {
                    if(testToken != null)
                    {
                        testToken.Dispose();
                        testToken = null;
                    }
                }
            });

            oscServer.RegisterAction<int>("/1/push14", (msg, data) =>
            {
                //                flickerEffect.Start();
                spiderLight.Color = Color.Red;
                spiderLight.Brightness = data.First();
                pinSpot.Color = Color.Purple;
                pinSpot.Brightness = data.First();
                underGeorge.Brightness = data.First();
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
                log.Info("Page 1");
                manualFader.Value = false;

                SetPixelColor();
            });

            oscServer.RegisterAction("/2", msg =>
            {
                log.Info("Page 2");
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

        private void TriggerThunderTimeline(object sender, Animatroller.Framework.Controller.Timeline<string>.TimelineEventArgs e)
        {
            switch (e.Code)
            {
                case "A":
                    popOut3.Pop(1.0);
                    popOut4.Pop(1.0);
                    break;

                case "B":
                    popOut2.Pop(0.5);
                    break;

                case "C":
                    popOut1.Pop(1.0);
                    break;
            }
        }

        public override void Run()
        {
            SetPixelColor();
        }

        public override void Stop()
        {
            audioMain.PauseBackground();
        }
    }
}
