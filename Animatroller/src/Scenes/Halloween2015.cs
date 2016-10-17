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
    internal class Halloween2015 : BaseScene
    {
        public enum States
        {
            Background,
            EmergencyStop,
            Special1
        }

        private const int midiChannel = 0;

        private Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();
        private Expander.MidiInput2 midiInput = new Expander.MidiInput2("LPD8", ignoreMissingDevice: true);
        private Expander.OscServer oscServer = new Expander.OscServer();
        private AudioPlayer audioCat = new AudioPlayer();
        private AudioPlayer audioMain = new AudioPlayer();
        private AudioPlayer audioEeebox = new AudioPlayer();
        private AudioPlayer audioPop = new AudioPlayer();
        private AudioPlayer audioDIN = new AudioPlayer();
        private VideoPlayer video3dfx = new VideoPlayer();
        private VideoPlayer video2 = new VideoPlayer();
        private Expander.Raspberry raspberryCat = new Expander.Raspberry("192.168.240.115:5005", 3333);
        private Expander.Raspberry raspberry3dfx = new Expander.Raspberry("192.168.240.226:5005", 3334);
        private Expander.Raspberry monoExpanderEeebox = new Expander.Raspberry("192.168.240.237:5005", 3338);
        private Expander.Raspberry raspberryLocal = new Expander.Raspberry("127.0.0.1:5005", 3339);
        private Expander.Raspberry raspberryPop = new Expander.Raspberry("192.168.240.123:5005", 3335);
        private Expander.Raspberry raspberryDIN = new Expander.Raspberry("192.168.240.127:5005", 3337);
        private Expander.Raspberry raspberryVideo2 = new Expander.Raspberry("192.168.240.124:5005", 3336);
        private Expander.OscClient touchOSC = new Expander.OscClient("192.168.240.163", 9000);
        private Expander.AcnStream acnOutput = new Expander.AcnStream();

        private VirtualPixel1D3 pixelsRoofEdge = new VirtualPixel1D3(150);
        private AnalogInput3 faderR = new AnalogInput3(persistState: true);
        private AnalogInput3 faderG = new AnalogInput3(persistState: true);
        private AnalogInput3 faderB = new AnalogInput3(persistState: true);
        private AnalogInput3 faderBright = new AnalogInput3(persistState: true);
        private DigitalInput2 manualFader = new DigitalInput2(persistState: true);

        private AnalogInput3 inputBrightness = new AnalogInput3(true, name: "Brightness");
        private AnalogInput3 inputH = new AnalogInput3(true, name: "Hue");
        private AnalogInput3 inputS = new AnalogInput3(true, name: "Saturation");

        Controller.Subroutine subFinal = new Controller.Subroutine();
        Controller.Subroutine subFirst = new Controller.Subroutine();
        Controller.Subroutine subVideo = new Controller.Subroutine();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 emergencyStop = new DigitalInput2(persistState: true);
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 block = new DigitalInput2(persistState: true);

        private Effect.Flicker flickerEffect = new Effect.Flicker(0.4, 0.6, false);
        private Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        private Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.4, 1.0, false);
        private Effect.PopOut2 popOut1 = new Effect.PopOut2(S(0.3));
        private Effect.PopOut2 popOut2 = new Effect.PopOut2(S(0.3));
        private Effect.PopOut2 popOutAll = new Effect.PopOut2(S(1.2));

        private DigitalOutput2 spiderCeiling = new DigitalOutput2("Spider Ceiling");
        private DigitalOutput2 spiderCeilingDrop = new DigitalOutput2("Spider Ceiling Drop");
        private DigitalInput2 catMotion = new DigitalInput2();
        private DigitalInput2 firstBeam = new DigitalInput2();
        private DigitalInput2 finalBeam = new DigitalInput2();
        private DigitalInput2 motion2 = new DigitalInput2();
        private DigitalOutput2 catAir = new DigitalOutput2(initial: true);
        private DigitalOutput2 fog = new DigitalOutput2();
        private DateTime? lastFogRun = DateTime.Now;
        private DigitalOutput2 candyEyes = new DigitalOutput2();
        private DigitalOutput2 catLights = new DigitalOutput2();
        private DigitalOutput2 george1 = new DigitalOutput2();
        private DigitalOutput2 george2 = new DigitalOutput2();
        private DigitalOutput2 popper = new DigitalOutput2();
        private DigitalOutput2 dropSpiderEyes = new DigitalOutput2();

        private OperatingHours2 hoursSmall = new OperatingHours2("Hours Small");
        private OperatingHours2 hoursFull = new OperatingHours2("Hours Full");

        private GroupDimmer allLights = new GroupDimmer();
        private GroupDimmer purpleLights = new GroupDimmer();

        private StrobeColorDimmer3 spiderLight = new StrobeColorDimmer3("Spider");
        private StrobeColorDimmer3 wall1Light = new StrobeColorDimmer3("Wall 1");
        private StrobeColorDimmer3 wall2Light = new StrobeColorDimmer3("Wall 2");
        private StrobeColorDimmer3 wall3Light = new StrobeColorDimmer3("Wall 3");
        private StrobeColorDimmer3 wall4Light = new StrobeColorDimmer3("Wall 4");
        private Dimmer3 stairs1Light = new Dimmer3("Stairs 1");
        private Dimmer3 stairs2Light = new Dimmer3("Stairs 2");
        private Dimmer3 treeGhosts = new Dimmer3();
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
        private string currentVideoFile;

        private string[] videoFiles = new string[]
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

        public Halloween2015(IEnumerable<string> args)
        {
            hoursSmall.AddRange("5:00 pm", "7:00 pm");
            //            hoursFull.AddRange("5:00 pm", "7:00 pm");

            // Logging
            hoursSmall.Output.Log("Hours small");
            hoursFull.Output.Log("Hours full");

            hoursSmall
                .ControlsMasterPower(catAir);
            hoursFull
                .ControlsMasterPower(catAir);
            //                .ControlsMasterPower(eyes);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    hoursFull.SetForced(true);
                else
                    hoursFull.SetForced(false);
            });


            emergencyStop.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.GoToState(States.EmergencyStop);
                }
                else
                {
                    if (hoursFull.IsOpen)
                        stateMachine.GoToState(States.Background);
                    else
                        stateMachine.GoToIdle();
                }
            });

            hoursFull.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetDefaultState(States.Background);

                    if (emergencyStop.Value)
                        stateMachine.GoToState(States.EmergencyStop);
                    else
                        stateMachine.GoToState(States.Background);
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
            popOutAll.ConnectTo(underGeorge);
            popOutAll.ConnectTo(pixelsRoofEdge);
            popOutAll.ConnectTo(pinSpot);

            allLights.Add(wall1Light, wall2Light, wall3Light, wall4Light, underGeorge, pixelsRoofEdge, pinSpot, spiderLight);
            purpleLights.Add(wall1Light, wall2Light, wall3Light, wall4Light, pixelsRoofEdge);

            flickerEffect.ConnectTo(stairs1Light);
            flickerEffect.ConnectTo(stairs2Light);
            pulsatingEffect1.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(0, 255, 0)));
            pulsatingEffect2.ConnectTo(pinSpot, Tuple.Create<DataElements, object>(DataElements.Color, Color.FromArgb(255, 0, 0)));

            stateMachine.For(States.Background)
                .Execute(i =>
                    {
                        subVideo.Run();
                        flickerEffect.Start();
                        treeGhosts.SetBrightness(1.0);
                        audioMain.PlayBackground();
                        audioEeebox.SetBackgroundVolume(0.6);
                        audioEeebox.PlayBackground();

                        ColorBrightness purpleColor = new ColorBrightness(HSV.ColorFromRGB(0.73333333333333328, 0, 1),
                            0.16470588235294117);

                        purpleLights.SetBrightness(purpleColor.Brightness, new Data(DataElements.Color, purpleColor.Color));

                        while (!i.IsCancellationRequested && stateMachine.CurrentState == States.Background)
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
                        audioMain.PauseBackground();
                        audioEeebox.PauseBackground();

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
                    audioEeebox.PlayNewEffect("640 The Demon Exorcised.wav");

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

            midiInput.Controller(midiChannel, 1).Controls(inputBrightness.Control);
            midiInput.Controller(midiChannel, 2).Controls(inputH.Control);
            midiInput.Controller(midiChannel, 3).Controls(inputS.Control);

            raspberryLocal.AudioTrackStart.Subscribe(x =>
            {
                // Next track
                switch (x)
                {
                    case "Thunder1.wav":
                        timelineThunder1.Start();
                        audioEeebox.PlayEffect("scream.wav");
                        break;

                    case "Thunder2.wav":
                        timelineThunder2.Start();
                        break;

                    case "Thunder3.wav":
                        timelineThunder3.Start();
                        break;

                    case "Thunder4.wav":
                        timelineThunder4.Start();
                        audioEeebox.PlayEffect("424 Coyote Howling.wav");
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

            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 0, 50), 6, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 50, 100), 5, 1);

            acnOutput.Connect(new Physical.SmallRGBStrobe(spiderLight, 1), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall1Light, 60), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall2Light, 70), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall3Light, 40), 1);
            acnOutput.Connect(new Physical.RGBStrobe(wall4Light, 80), 1);
            acnOutput.Connect(new Physical.GenericDimmer(stairs1Light, 50), 1);
            acnOutput.Connect(new Physical.GenericDimmer(stairs2Light, 51), 1);
            acnOutput.Connect(new Physical.GenericDimmer(treeGhosts, 52), 1);
            acnOutput.Connect(new Physical.AmericanDJStrobe(underGeorge, 100), 1);
            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(pinSpot, 20), 1);

            //            acnOutput.Connect(new Physical.RGBIS(testLight1, 260), 1);


            raspberryCat.DigitalInputs[4].Connect(catMotion, false);
            raspberryCat.DigitalInputs[5].Connect(firstBeam, false);
            raspberryCat.DigitalInputs[6].Connect(finalBeam, false);
            raspberryCat.DigitalOutputs[7].Connect(spiderCeilingDrop);
            raspberryCat.Connect(audioCat);
            raspberryLocal.Connect(audioMain);
            monoExpanderEeebox.Connect(audioEeebox);
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

            oscServer.RegisterAction<int>("/3/multipush1/6/1", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("sixthsense-deadpeople.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/2", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("162 Blood Curdling Scream of Terror.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/3", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("424 Coyote Howling.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/4", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/5", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("242004__junkfood2121__fart-01.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/1", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("death-scream.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/2", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("scream.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/3", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("door-creak.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/4", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("violin screech.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/5", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("gollum_precious1.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/1", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayNewEffect("640 The Demon Exorcised.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/2", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("266 Monster Growl 7.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/3", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("285 Monster Snarl 2.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/4", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("286 Monster Snarl 3.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/5", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("287 Monster Snarl 4.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/3/multipush1/3/1", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("180 Babbling Lunatic.wav");
            });

            oscServer.RegisterAction<int>("/1/eStop", (msg, data) =>
            {
                emergencyStop.Control.OnNext(data.First() != 0);
            });

            oscServer.RegisterAction<int>("/1/push2", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("285 Monster Snarl 2.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push3", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("286 Monster Snarl 3.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push4", (msg, data) =>
            {
                // Flash
                if (data.First() != 0)
                {
                    allLights.TakeAndHoldControl();
                    allLights.SetBrightness(1.0, new Data(DataElements.Color, Color.White));
                }
                else
                    allLights.ReleaseControl();
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

            oscServer.RegisterAction<int>("/1/special1", (msg, data) =>
            {
                if (data.First() != 0)
                    stateMachine.GoToMomentaryState(States.Special1);
                else
                    stateMachine.StopCurrentJob();
            });

            oscServer.RegisterAction<int>("/1/push13", d => d.First() != 0, (msg, data) =>
            {
                //                Exec.MasterEffect.Fade(stairs1Light, 1.0, 0.0, 2000, token: testToken);
                //popOut1.Pop();
                //popOut2.Pop();
                //popOut3.Pop();
                popOutAll.Pop(color: Color.White);
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
                //                pinSpot.SetBrightness(data.First());
            });

            oscServer.RegisterAction<int>("/1/toggle3", (msg, data) =>
            {
                if (data.First() != 0)
                    audioEeebox.PlayBackground();
                else
                    audioEeebox.PauseBackground();
            });

            oscServer.RegisterAction<int>("/1/toggle4", (msg, data) =>
            {
                block.Value = data.First() != 0;
                //                treeGhosts.SetBrightness(data.First() != 0 ? 1.0 : 0.0);
            });

            oscServer.RegisterAction<int>("/1/push14", (msg, data) =>
            {
                //                flickerEffect.Start();
                //double brightness = data.First();

                //spiderLight.SetColor(Color.Red, brightness);
                //pinSpot.SetColor(Color.Purple, brightness);
                //underGeorge.SetBrightness(brightness);
                //wall1Light.SetColor(Color.Purple, brightness);
                //wall2Light.SetColor(Color.Purple, brightness);
                //wall3Light.SetColor(Color.Purple, brightness);
                //wall4Light.SetColor(Color.Purple, brightness);
                //                audioDIN.PlayEffect("gollum_precious1.wav");
            });

            oscServer.RegisterAction<int>("/1/push20", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Wraith_StartleScare_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/1/push21", d => d.First() != 0, (msg, data) =>
            {
                //               video2.PlayVideo("SkeletonSurprise_Door_Horz_HD.mp4");
                video2.PlayVideo("Beauty_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/5/1", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("Beauty_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/5/2", d => d.First() != 0, (msg, data) =>
            {
            });

            oscServer.RegisterAction<int>("/4/multipush2/5/3", d => d.First() != 0, (msg, data) =>
            {
            });

            oscServer.RegisterAction<int>("/4/multipush2/5/4", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("FearTheReaper_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/4/1", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("GatheringGhouls_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/4/2", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("Girl_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/4/3", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("HeadOfHouse_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/4/4", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("JitteryBones_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/3/1", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Poltergeist_StartleScare_Holl_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/3/2", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Siren_StartleScare_Holl_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/3/3", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Spinster_StartleScare_Holl_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/3/4", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Wraith_StartleScare_Holl_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/2/1", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PopUpPanic_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/2/2", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("SkeletonSurprise_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/2/3", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("Wraith_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/2/4", d => d.First() != 0, (msg, data) =>
            {
            });

            oscServer.RegisterAction<int>("/4/multipush2/1/1", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Wraith_StartleScare_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/1/2", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Spinster_StartleScare_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/1/3", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Siren_StartleScare_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/1/4", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Poltergeist_StartleScare_3DFX_H.mp4");
            });


            oscServer.RegisterAction("/1", msg =>
            {
                log.Info("Page 1");
                //                manualFader.Value = false;

                SetPixelColor();
            });

            oscServer.RegisterAction("/2", msg =>
            {
                log.Info("Page 2");
                //                manualFader.Value = true;

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
                {
                    allLights.TakeAndHoldControl();
                    allLights.SetBrightness(1.0, new Data(DataElements.Color, Color.White));
                }
                else
                    allLights.ReleaseControl();
            });

            midiInput.Note(midiChannel, 41).Subscribe(x =>
            {
                if (x)
                    audioEeebox.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav");
            });

            midiInput.Note(midiChannel, 42).Subscribe(x =>
            {
                if (x)
                    audioEeebox.PlayEffect("180 Babbling Lunatic.wav");
            });

            midiInput.Note(midiChannel, 43).Subscribe(x =>
            {
                if (x)
                    audioEeebox.PlayEffect("162 Blood Curdling Scream of Terror.wav");
            });

            catMotion.Output.Subscribe(x =>
            {
                if (x && (hoursFull.IsOpen || hoursSmall.IsOpen))
                    Executor.Current.Execute(catSeq);

                touchOSC.Send("/1/led1", x ? 1 : 0);
            });

            firstBeam.Output.Subscribe(x =>
            {
                touchOSC.Send("/1/led2", x ? 1 : 0);

                if (x && hoursFull.IsOpen && !emergencyStop.Value && !block.Value)
                    subFirst.Run();
            });

            finalBeam.Output.Subscribe(x =>
            {
                touchOSC.Send("/1/led3", x ? 1 : 0);

                if (x && hoursFull.IsOpen && !emergencyStop.Value && !block.Value)
                    subFinal.Run();
            });

            motion2.Output.Subscribe(x =>
            {
                //if (x && hoursFull.IsOpen)
                //    Executor.Current.Execute(motionSeq);

                touchOSC.Send("/1/led4", x ? 1 : 0);
            });

            welcomeSeq.WhenExecuted
                .Execute(i =>
                {
                    audioPop.PlayEffect("100471__robinhood76__01886-welcome-spell.wav");

                    i.WaitFor(S(3));
                });

            subFirst
                .LockWhenRunning(10, spiderLight)
                .RunAction(i =>
                {
                    fog.Value = true;
                    this.lastFogRun = DateTime.Now;
                    pulsatingEffect2.Start();
                    i.WaitFor(S(5.0));

                    spiderLight.SetColor(Color.Red, token: i.Token);
                    dropSpiderEyes.Value = true;
                    spiderCeilingDrop.Value = true;
                    audioPop.PlayEffect("348 Spider Hiss.wav", 1.0, 0.0);

                    i.WaitFor(S(2.0));
                    dropSpiderEyes.Value = false;
                    spiderLight.SetBrightness(0.0, i.Token);
                    switch (random.Next(4))
                    {
                        case 0:
                            video3dfx.PlayVideo("PHA_Wraith_StartleScare_3DFX_H.mp4");
                            break;

                        case 1:
                            video3dfx.PlayVideo("PHA_Spinster_StartleScare_3DFX_H.mp4");
                            break;

                        case 2:
                            video3dfx.PlayVideo("PHA_Siren_StartleScare_3DFX_H.mp4");
                            break;

                        case 3:
                            video3dfx.PlayVideo("PHA_Poltergeist_StartleScare_3DFX_H.mp4");
                            break;
                    }
                    fog.Value = false;
                    i.WaitFor(S(2.0));
                    spiderCeilingDrop.Value = false;
                    i.WaitFor(S(3.0));
                })
                .TearDown(() =>
                {
                    pulsatingEffect2.Stop();
                    Thread.Sleep(S(5));
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
        .TearDown(instance =>
        {
            catLights.Value = false;
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
        }

        private Color GetFaderColor()
        {
            return HSV.ColorFromRGB(faderR.Value, faderG.Value, faderB.Value);
        }

        private void SetPixelColor()
        {
            if (manualFader.Value)
                pixelsRoofEdge.SetColor(GetFaderColor(), faderBright.Value);
        }

        private void TriggerThunderTimeline(object sender, Animatroller.Framework.Controller.Timeline<string>.TimelineEventArgs e)
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
            audioMain.PauseBackground();
            audioEeebox.PauseBackground();
        }
    }
}
