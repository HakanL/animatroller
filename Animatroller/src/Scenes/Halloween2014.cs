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

namespace Animatroller.SceneRunner
{
    internal class Halloween2014 : BaseScene
    {
        public enum States
        {
            Background,
            Stair,
            George,
            StepForward
        }

        private const int midiChannel = 0;

        private Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();
        private DateTime? lastFogRun = DateTime.Now;
        //private Expander.MidiInput2 midiInput = new Expander.MidiInput2();
        private Expander.OscServer oscServer = new Expander.OscServer();
        private AudioPlayer audioCat = new AudioPlayer();
        private AudioPlayer audioReaper = new AudioPlayer();
        private AudioPlayer audioOla = new AudioPlayer();
        private AudioPlayer audioGeorge = new AudioPlayer();
        private Expander.Raspberry raspberryCat = new Expander.Raspberry("192.168.240.115:5005", 3333);
        private Expander.Raspberry raspberryReaper = new Expander.Raspberry("192.168.240.123:5005", 3334);
        private Expander.Raspberry raspberryOla = new Expander.Raspberry("192.168.240.147:5005", 3335);
        private Expander.Raspberry raspberryGeorge = new Expander.Raspberry("192.168.240.131:5005", 3336);

        private MotorWithFeedback georgeMotor = new MotorWithFeedback("George");
        private MovingHead movingHead = new MovingHead("Test 1");
        private StrobeColorDimmer2 reaperLight = new StrobeColorDimmer2("Reaper");
        private StrobeColorDimmer2 candySpot = new StrobeColorDimmer2("Test Light 2");
        private Dimmer2 testLight3 = new Dimmer2("Test 3");
        //        private MovingHead testLight4 = new MovingHead("Moving Head");
        private Dimmer2 lightning1 = new Dimmer2("Lightning 1");
        //        private Dimmer2 lightning2 = new Dimmer2("Lightning 2");
        private Dimmer2 lightStairs1 = new Dimmer2("Stairs 1");
        private Dimmer2 lightStairs2 = new Dimmer2("Stairs 2");
        private Dimmer2 lightSpiderWeb = new Dimmer2("Spider Web");
        private Dimmer2 lightMirrorSkeleton = new Dimmer2("Mirror Skel");
        private Dimmer2 lightWindow = new Dimmer2("Window");
        private Dimmer2 lightInside = new Dimmer2("Inside");
        private DigitalOutput2 lightFlash1 = new DigitalOutput2("Flash 1");
        private DigitalOutput2 lightFlash2 = new DigitalOutput2("Flash 2");
        private DigitalOutput2 lightTree = new DigitalOutput2("Tree");
        private DigitalOutput2 deadEnd = new DigitalOutput2("Dead End");
        private DigitalOutput2 fog = new DigitalOutput2("Fog");
        private DigitalOutput2 eyes = new DigitalOutput2("Eyes", initial: true);
        private DigitalOutput2 spiderEyes1 = new DigitalOutput2("Spider Entrance");
        private DigitalOutput2 spiderEyes2 = new DigitalOutput2("Spider Ceiling");
        private StrobeColorDimmer2 lightBehindHeads = new StrobeColorDimmer2("Behind heads");
        private StrobeColorDimmer2 lightBehindSheet = new StrobeColorDimmer2("Behind sheet");
        private StrobeColorDimmer2 lightEntranceR = new StrobeColorDimmer2("Entrance R");
        private StrobeColorDimmer2 lightEntranceL = new StrobeColorDimmer2("Entrance L");
        private DigitalInput2 buttonCatTrigger = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest1 = new DigitalInput2("Test 1");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 forceOpen = new DigitalInput2("Force Open", true);
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest2 = new DigitalInput2("Test 2");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest3 = new DigitalInput2("Test 3");
        private DigitalInput2 buttonTest4 = new DigitalInput2("Next BG");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 manualHeadMovement = new DigitalInput2("Manual Head");
        private DigitalInput2 catMotion = new DigitalInput2();
        private DigitalInput2 finalBeam = new DigitalInput2();
        private DigitalInput2 firstBeam = new DigitalInput2();
        private AnalogInput2 inputBrightness = new AnalogInput2("Brightness");
        private AnalogInput2 inputH = new AnalogInput2("Hue", true);
        private AnalogInput2 inputS = new AnalogInput2("Saturation", true);
        private AnalogInput2 inputStrobe = new AnalogInput2("Strobe", true);
        private AnalogInput2 inputPan = new AnalogInput2("Pan", true);
        private AnalogInput2 inputTilt = new AnalogInput2("Tilt", true);
        private Expander.AcnStream acnOutput = new Expander.AcnStream();
        private DigitalOutput2 catAir = new DigitalOutput2(initial: true);
        private DigitalOutput2 catLights = new DigitalOutput2();
        private DigitalOutput2 skullEyes = new DigitalOutput2();
        private DigitalOutput2 reaperPopUp = new DigitalOutput2();
        private DigitalOutput2 reaperEyes = new DigitalOutput2();

        private OperatingHours2 hoursSmall = new OperatingHours2("Hours Small");
        private OperatingHours2 hoursInside = new OperatingHours2("Inside");

        // Effects
        private Effect.Flicker flickerEffect = new Effect.Flicker(0.4, 0.6, false);
        private Effect.Flicker flickerEffect2 = new Effect.Flicker(0.4, 0.6, false);
        private Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        private Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.4, 1.0, false);

        private Effect.PopOut popOut1 = new Effect.PopOut(S(0.3));
        private Effect.PopOut popOutBehindHeads = new Effect.PopOut(S(1.0));
        private Effect.PopOut popOut2 = new Effect.PopOut(S(0.3));
        private Effect.PopOut popOut3 = new Effect.PopOut(S(0.5));
        private Effect.PopOut popOut4 = new Effect.PopOut(S(1.2));

        private Controller.Sequence catSeq = new Controller.Sequence();
        private Controller.Sequence thunderSeq = new Controller.Sequence();
        private Controller.Sequence stepForwardSeq = new Controller.Sequence();
        private Controller.Sequence backgroundSeq = new Controller.Sequence();
        private Controller.Sequence finalSeq = new Controller.Sequence();
        private Controller.Sequence reaperSeq = new Controller.Sequence("Reaper");
        private Controller.Sequence reaperPopSeq = new Controller.Sequence("Reaper Pop");
        private Controller.Sequence georgeSeq = new Controller.Sequence();
        private Controller.Timeline<string> timelineThunder1 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder2 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder3 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder4 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder5 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder6 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder7 = new Controller.Timeline<string>(1);
        private Controller.Timeline<string> timelineThunder8 = new Controller.Timeline<string>(1);

        public Halloween2014(IEnumerable<string> args)
        {
            hoursSmall.AddRange("5:00 pm", "9:00 pm");
            hoursInside.AddRange("6:00 pm", "10:00 pm");
            hoursInside.SetForced(false);

            // Logging
            hoursSmall.Output.Log("Hours small");
            hoursInside.Output.Log("Hours inside");
            movingHead.InputPan.Log("Pan");
            movingHead.InputTilt.Log("Tilt");


            hoursSmall
                .ControlsMasterPower(catAir)
                .ControlsMasterPower(catLights)
                .ControlsMasterPower(eyes);

            hoursInside.Output.Subscribe(x =>
                {
                    lightInside.Brightness = x ? 1.0 : 0.0;
                    lightMirrorSkeleton.Brightness = x ? 1.0 : 0.0;
                });

            flickerEffect.ConnectTo(lightStairs1.InputBrightness);
            flickerEffect.ConnectTo(lightStairs2.InputBrightness);
            //            pulsatingEffect1.ConnectTo(lightBehindHeads.InputBrightness);
            //            pulsatingEffect1.ConnectTo(lightBehindSheet.InputBrightness);
            pulsatingEffect1.ConnectTo(candySpot.InputBrightness);
            pulsatingEffect2.ConnectTo(lightSpiderWeb.InputBrightness);
            //            pulsatingEffect2.ConnectTo(lightMirrorSkeleton.InputBrightness);
            //            pulsatingEffect2.ConnectTo(lightWindow.InputBrightness);
            //            pulsatingEffect2.ConnectTo(lightInside.InputBrightness);
            //            flickerEffect2.ConnectTo(lightInside.InputBrightness);
            //            pulsatingEffect1.ConnectTo(movingHead.InputBrightness);

            //            movingHead.InputBrightness.Log("Moving Head Brightness");

            //            popOut1.AddDevice(lightning1.InputBrightness);
            //            popOut2.ConnectTo(light.InputBrightness);
            popOut1.ConnectTo(lightBehindHeads.InputBrightness);
            popOut1.ConnectTo(lightBehindSheet.InputBrightness);
            //            popOut1 ConnectTo(lightFlash1.InputBrightness);
            //            popOut2.ConnectTo(lightFlash1.InputBrightness);
            popOut2.ConnectTo(lightBehindSheet.InputBrightness);
            popOut2.ConnectTo(lightning1.InputBrightness);
            popOut3.ConnectTo(lightBehindHeads.InputBrightness);
            popOut3.ConnectTo(lightBehindSheet.InputBrightness);
            popOut3.ConnectTo(lightEntranceR.InputBrightness);
            popOut3.ConnectTo(lightEntranceL.InputBrightness);
            popOut3.ConnectTo(new BrightnessPowerAdapter(lightFlash1).InputBrightness);
            //            popOut3.ConnectTo(lightFlash1.InputBrightness);
            //            popOut4.ConnectTo(lightEntranceL.InputBrightness);
            popOut4.ConnectTo(new BrightnessPowerAdapter(lightFlash1).InputBrightness);
            popOut4.ConnectTo(new BrightnessPowerAdapter(lightFlash2).InputBrightness);

            popOutBehindHeads.ConnectTo(lightBehindHeads.InputBrightness);
            //popOut3.ConnectTo(movingHead.InputBrightness);

            raspberryReaper.DigitalInputs[7].Connect(finalBeam, false);
            raspberryReaper.DigitalOutputs[2].Connect(spiderEyes1);
            raspberryReaper.DigitalOutputs[3].Connect(spiderEyes2);

            raspberryCat.DigitalInputs[4].Connect(catMotion, true);
            raspberryCat.DigitalInputs[5].Connect(firstBeam, false);
            raspberryCat.DigitalOutputs[7].Connect(deadEnd);
            raspberryCat.Connect(audioCat);
            raspberryCat.Motor.Connect(georgeMotor);

            raspberryReaper.Connect(audioReaper);
            raspberryOla.Connect(audioOla);

            raspberryGeorge.DigitalOutputs[7].Connect(fog);
            raspberryGeorge.Connect(audioGeorge);

            inputBrightness.ConnectTo(movingHead.InputBrightness);

            // Map Physical lights
            acnOutput.Connect(new Physical.SmallRGBStrobe(reaperLight, 1), 20);
            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(candySpot, 20), 20);
            acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(movingHead, 200), 20);
            acnOutput.Connect(new Physical.GenericDimmer(catAir, 11), 20);
            acnOutput.Connect(new Physical.GenericDimmer(eyes, 12), 20);
            acnOutput.Connect(new Physical.GenericDimmer(catLights, 10), 20);
            //BROKEN            acnOutput.Connect(new Physical.GenericDimmer(testLight3, 103), 20);
            //            acnOutput.Connect(new Physical.GenericDimmer(lightning2, 100), 20);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 101), 20);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 102), 20);
            acnOutput.Connect(new Physical.AmericanDJStrobe(lightning1, 5), 20);
            acnOutput.Connect(new Physical.RGBStrobe(lightBehindHeads, 40), 20);
            acnOutput.Connect(new Physical.RGBStrobe(lightBehindSheet, 60), 20);
            acnOutput.Connect(new Physical.RGBStrobe(lightEntranceR, 70), 20);
            acnOutput.Connect(new Physical.RGBStrobe(lightEntranceL, 80), 20);
            acnOutput.Connect(new Physical.GenericDimmer(lightTree, 50), 20);
            acnOutput.Connect(new Physical.GenericDimmer(lightSpiderWeb, 2), 21);
            acnOutput.Connect(new Physical.GenericDimmer(lightMirrorSkeleton, 1), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightInside, 2), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightWindow, 3), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightFlash1, 1), 21);
            acnOutput.Connect(new Physical.GenericDimmer(lightFlash2, 4), 22);

            candySpot.SetOnlyColor(Color.Green);

            oscServer.RegisterAction<int>("/mrmr/pushbutton/0/Hakan-iPhone-6", (msg, data) =>
            {
                if (data.First() != 0)
                {
                    audioOla.PlayTrack("12 Fear of the Dark");
                    //                    popOut4.Pop(1.0);
                    //                    stateMachine.SetState(States.Stair);
                    //                    Exec.Execute(testSeq);
                    //                    Exec.Execute(reaperSeq);
                    //                    popOut4.Pop(1.0);
                    //                    pulsatingEffect2.Start();
                }
            });

            oscServer.RegisterAction<int>("/mrmr/pushbutton/1/Hakan-iPhone-6", (msg, data) =>
            {
                if (data.Any() && data.First() != 0)
                {
                    audioOla.NextBackgroundTrack();
                    //                    stateMachine.SetState(States.George);
                    //                    Exec.Execute(georgeSeq);
                }
            });

            oscServer.RegisterAction<int>("/mrmr/pushbutton/2/Hakan-iPhone-6", (msg, data) =>
            {
                if (data.First() != 0)
                {
                    Exec.Execute(reaperPopSeq);
                }
            });

            oscServer.RegisterAction<int>("/mrmr/pushbutton/3/Hakan-iPhone-6", (msg, data) =>
            {
                if (data.First() != 0)
                {
                    catLights.Power = true;
                    switch (random.Next(3))
                    {
                        case 0:
                            audioCat.PlayEffect("386 Demon Creature Growls");
                            break;
                        case 1:
                            audioCat.PlayEffect("348 Spider Hiss");
                            break;
                        case 2:
                            audioCat.PlayEffect("death-scream");
                            break;
                    }
                    Thread.Sleep(2000);
                    catLights.Power = false;
                }
            });


            finalBeam.WhenOutputChanges(x =>
                {
                    if (x && hoursSmall.IsOpen)
                    {
                        Exec.Execute(finalSeq);
                    }
                    //                    lightning1.Brightness = x ? 1.0 : 0.0;
                    //if (x)
                    //{
                    //    Exec.Execute(thunderSeq);
                    //}
                });

            firstBeam.WhenOutputChanges(x =>
            {
                if (x && hoursSmall.IsOpen)
                {
                    //                    Exec.Execute(reaperSeq);
                    if (stateMachine.CurrentState == States.Background ||
                        stateMachine.CurrentState == States.StepForward)
                        stateMachine.SetState(States.Stair);
                }
            });

            buttonTest2.WhenOutputChanges(x =>
                {
                    if (x)
                    {
                        audioOla.NextBackgroundTrack();
                        //                        Exec.Execute(georgeSeq);
                    }
                });

            raspberryOla.AudioTrackStart.Subscribe(x =>
                {
                    // Next track
                    switch (x)
                    {
                        case "12 Fear of the Dark":
                            // Do nothing
                            break;

                        case "Thunder1":
                            timelineThunder1.Start();
                            break;

                        case "Thunder2":
                            timelineThunder2.Start();
                            break;

                        case "Thunder3":
                            timelineThunder3.Start();
                            break;

                        case "Thunder4":
                            timelineThunder4.Start();
                            break;

                        case "Thunder5":
                            timelineThunder5.Start();
                            break;

                        case "Thunder6":
                            timelineThunder6.Start();
                            break;

                        case "Thunder7":
                            timelineThunder7.Start();
                            break;

                        case "Thunder8":
                            timelineThunder8.Start();
                            break;
                    }
                });

            buttonTest4.WhenOutputChanges(x =>
                {
                    if (x)
                        audioOla.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral");
                    //                        Exec.Execute(reaperSeq);
                    //                    lightning2.Brightness = x ? 1.0 : 0.0;
                    //                    if (x)
                    //                        popOut1.Pop(1.0);
                    //                    if (x)
                    //                    lightStairs1.Brightness = x ? 1.0 : 0.0;
                    //                    testLight3.Brightness = x ? 1.0 : 0.0;
                    //                        audioSpider.PlayEffect("348 Spider Hiss");
                    //                    Exec.Execute(thunderSeq);
                });

            buttonTest3.WhenOutputChanges(x =>
            {
                //                fog.Power = x;
                if (x)
                {
                    Exec.Execute(reaperPopSeq);

                    //for (int i = 0; i < 100; i++)
                    //{
                    //    raspberryReaper.Test(i);
                    //}
                }
                //                    audioGeorge.PlayEffect("242004__junkfood2121__fart-01");
            });

            raspberryReaper.DigitalOutputs[7].Connect(reaperPopUp);
            raspberryReaper.DigitalOutputs[6].Connect(reaperEyes);
            raspberryReaper.DigitalOutputs[5].Connect(skullEyes);

            forceOpen.WhenOutputChanges(x =>
                {
                    if (x)
                        hoursSmall.SetForced(true);
                    else
                        hoursSmall.SetForced(null);
                });

            inputH.WhenOutputChanges(x =>
            {
                movingHead.SetOnlyColor(HSV.ColorFromHSV(x.Value.GetByteScale(), inputS.Value, 1.0));
            });

            inputS.Output.Subscribe(x =>
            {
                movingHead.SetOnlyColor(HSV.ColorFromHSV(inputH.Value.GetByteScale(), x.Value, 1.0));
            });

            inputStrobe.Output.Controls(movingHead.InputStrobeSpeed);

            //midiInput.Controller(midiChannel, 1).Controls(inputBrightness.Control);
            //midiInput.Controller(midiChannel, 2).Controls(inputH.Control);
            //midiInput.Controller(midiChannel, 3).Controls(inputS.Control);
            //midiInput.Controller(midiChannel, 4).Controls(inputStrobe.Control);

            //midiInput.Controller(midiChannel, 5).Controls(Observer.Create<double>(x =>
            //    {
            //        inputPan.Control.OnNext(new DoubleZeroToOne(x * 540));
            //    }));
            //midiInput.Controller(midiChannel, 6).Controls(Observer.Create<double>(x =>
            //{
            //    inputTilt.Control.OnNext(new DoubleZeroToOne(x * 270));
            //}));

            //midiInput.Note(midiChannel, 36).Controls(buttonTest1.Control);
            //midiInput.Note(midiChannel, 37).Controls(buttonTest2.Control);
            //midiInput.Note(midiChannel, 38).Controls(buttonTest3.Control);
            //midiInput.Note(midiChannel, 39).Controls(buttonTest4.Control);

            //midiInput.Note(midiChannel, 40).Controls(buttonCatTrigger.Control);


            //buttonTest4.Output.Subscribe(x =>
            //    {
            //        if (x)
            //        {
            //            audioOla.NextBackgroundTrack();
            //        }
            //    });

            manualHeadMovement.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        inputPan.Output.Controls(movingHead.InputPan);
                        inputTilt.Output.Controls(movingHead.InputTilt);
                    }
                    else
                    {
                        //                        inputPan.Output.Dis
                    }
                });

            //            buttonTest2.Output.Subscribe(reaperPopUp.PowerControl);
            catMotion.Output.Subscribe(catLights.ControlValue);

            /*
                        finalBeam.Output.Subscribe(x =>
                            {
                                lightning1.Brightness = x ? 1.0 : 0.0;
                            });
            */

            //            buttonTest1.Output.Subscribe(pulsatingEffect2.InputRun);

            //buttonTest1.Output.Subscribe(x =>
            //    {
            //        pulsatingEffect2.Start
            //        if(x)

            //        if (x)
            //        {
            //            movingHead.Brightness = 1;
            //            movingHead.Color = Color.Red;
            //            //                        testLight4.StrobeSpeed = 1.0;
            //        }
            //        else
            //        {
            //            movingHead.TurnOff();
            //        }
            //    });

            catMotion.Output.Subscribe(x =>
                {
                    if (x && hoursSmall.IsOpen)
                        Executor.Current.Execute(catSeq);
                });

            buttonCatTrigger.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        catLights.Power = true;
                        switch (random.Next(3))
                        {
                            case 0:
                                audioCat.PlayEffect("386 Demon Creature Growls");
                                break;
                            case 1:
                                audioCat.PlayEffect("348 Spider Hiss");
                                break;
                            case 2:
                                audioCat.PlayEffect("death-scream");
                                break;
                        }
                        Thread.Sleep(2000);
                        catLights.Power = false;
                    }
                });

            buttonTest1.Output.Subscribe(x =>
            {
                if (x)
                    audioOla.PlayTrack("12 Fear of the Dark");

                //if(x)
                //    georgeMotor.SetVector(0.9, 0, S(15));

                //                deadEnd.Power = x;
            });

            //            hoursSmall.Output.Subscribe(catAir.InputPower);
            //            hoursSmall.Output.Subscribe(flickerEffect.InputRun);
            //hoursSmall.Output.Subscribe(pulsatingEffect1.InputRun);
            //hoursSmall.Output.Subscribe(pulsatingEffect2.InputRun);
            hoursSmall.Output.Subscribe(lightTree.ControlValue);

            hoursSmall.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        stateMachine.SetBackgroundState(States.Background);
                        stateMachine.SetState(States.Background);
                    }
                    else
                    {
                        stateMachine.Hold();
                        stateMachine.SetBackgroundState(null);
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

            stateMachine.ForFromSequence(States.Background, backgroundSeq);
            stateMachine.ForFromSequence(States.Stair, reaperSeq);
            stateMachine.ForFromSequence(States.George, georgeSeq);
            stateMachine.ForFromSequence(States.StepForward, stepForwardSeq);
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

        public override void Start()
        {
            georgeSeq.WhenExecuted
                .Execute(instance =>
                {
                    var controlPan = new Effect.Fader(S(3.8), 106, 150, false);
                    var controlTilt = new Effect.Fader(S(3.8), 231.8823531, 168.3529407, false);

                    controlPan.ConnectTo(movingHead.InputPan);
                    controlTilt.ConnectTo(movingHead.InputTilt);

                    controlPan.Prime();
                    controlTilt.Prime();
                    movingHead.Brightness = 0;
                    // Make sure George isn't moving
                    georgeMotor.WaitForVectorReached();

                    instance.WaitFor(S(1.5));

                    controlPan.Start();
                    controlTilt.Start();

                    georgeMotor.SetVector(1.0, 450, S(10));
                    instance.WaitFor(S(1.2));
                    movingHead.SetColor(Color.Red, 0.1);
                    instance.WaitFor(S(1.0));
                    audioGeorge.PlayEffect("162 Blood Curdling Scream of Terror");
                    georgeMotor.WaitForVectorReached();

                    instance.WaitFor(S(4));
                    movingHead.Brightness = 0;

                    georgeMotor.SetVector(0.9, 0, S(15));
                    movingHead.Pan = 106;
                    movingHead.Tilt = 31;
                    //georgeMotor.WaitForVectorReached();

                    //                        deadEnd.Power = true;
                    instance.WaitFor(S(0.5));
                    //                        deadEnd.Power = false;
                    //                        Exec.Execute(thunderSeq);
                    stateMachine.NextState();
                });

            stepForwardSeq.WhenExecuted
                .SetUp(() =>
                {
                    audioOla.PlayTrack("152 Haunted Castle");
                    candySpot.SetOnlyColor(Color.Green);
                    pulsatingEffect1.Start();
                    pulsatingEffect2.Start();
                })
                .Execute(i =>
                {
                    i.WaitFor(S(30.0));
                })
                .TearDown(() =>
                {
                    audioOla.PauseTrack();
                    pulsatingEffect1.Stop();
                    pulsatingEffect2.Stop();
                });

            reaperSeq.WhenExecuted
            .SetUp(() =>
                {
                    flickerEffect.Stop();
                    pulsatingEffect2.Stop();
                })
                .Execute(instance =>
                {
                    //                    switchFog.SetPower(true);
                    //                    this.lastFogRun = DateTime.Now;
                    //                    Executor.Current.Execute(deadendSeq);
                    //                    audioGeorge.PlayEffect("ghostly");
                    //                    instance.WaitFor(S(0.5));
                    //                    popOutEffect.Pop(1.0);

                    //                    instance.WaitFor(S(1.0));



                    movingHead.Pan = 106;
                    movingHead.Tilt = 31;
                    fog.Power = true;
                    this.lastFogRun = DateTime.Now;
                    instance.WaitFor(S(0.05));
                    audioReaper.PlayEffect("348 Spider Hiss", 1.0, 0.0);
                    instance.WaitFor(S(0.05));
                    spiderEyes1.Power = true;
                    instance.WaitFor(S(0.5));

                    movingHead.SetColor(Color.Turquoise, 0.2);
                    movingHead.StrobeSpeed = 0.8;

                    deadEnd.Power = true;
                    instance.WaitFor(S(0.3));
                    deadEnd.Power = false;

                    instance.WaitFor(S(2.5));
                    movingHead.StrobeSpeed = 0;
                    movingHead.Brightness = 0;

                    movingHead.Pan = 80;
                    movingHead.Tilt = 26;
                    instance.WaitFor(S(1.0));
                    audioReaper.PlayNewEffect("laugh", 0.0, 1.0);
                    instance.WaitFor(S(0.1));
                    spiderEyes1.Power = false;
                    reaperPopUp.Power = true;
                    reaperLight.Color = Color.Red;
                    reaperLight.Brightness = 1;
                    reaperLight.StrobeSpeed = 1;
                    instance.WaitFor(S(0.5));
                    reaperEyes.Power = true;
                    instance.WaitFor(S(2.0));

                    reaperPopUp.Power = false;
                    reaperEyes.Power = false;
                    reaperLight.TurnOff();
                    instance.WaitFor(S(2.0));
                    audioOla.PlayEffect("424 Coyote Howling", 0.0, 1.0);
                    audioGeorge.PlayEffect("424 Coyote Howling");
                    movingHead.SetColor(Color.Orange, 0.2);
                    instance.WaitFor(S(2.0));
                    spiderEyes2.Power = true;
                    popOut2.Pop(0.4);
                    audioGeorge.PlayEffect("348 Spider Hiss");
                    audioReaper.PlayEffect("348 Spider Hiss");

                    movingHead.Brightness = 0.7;
                    movingHead.Color = Color.Red;
                    movingHead.StrobeSpeed = 0.8;
                    movingHead.Pan = 51;
                    movingHead.Tilt = 61;
                    instance.WaitFor(S(2.0));
                    movingHead.StrobeSpeed = 0.0;
                    movingHead.Brightness = 0;
                    spiderEyes2.Power = false;
                    instance.WaitFor(S(2.0));

                    movingHead.Pan = 106;
                    movingHead.Tilt = 231;

                    stateMachine.NextState();
                })
                .TearDown(() =>
                {
                    flickerEffect.Start();
                    pulsatingEffect2.Start();
                    fog.Power = false;
                });

            reaperPopSeq.WhenExecuted
                .Execute(instance =>
                {
                    audioReaper.PlayNewEffect("laugh", 0.0, 1.0);
                    instance.WaitFor(S(0.1));
                    reaperPopUp.Power = true;
                    reaperLight.Color = Color.Red;
                    reaperLight.Brightness = 1;
                    reaperLight.StrobeSpeed = 1;
                    instance.WaitFor(S(0.5));
                    reaperEyes.Power = true;
                    instance.WaitFor(S(2.0));

                    reaperPopUp.Power = false;
                    reaperEyes.Power = false;
                    reaperLight.TurnOff();
                    instance.WaitFor(S(0.5));
                });

            finalSeq.WhenExecuted
                .SetUp(() =>
                {
                    audioOla.PauseTrack();
                    pulsatingEffect1.Stop();
                    pulsatingEffect2.Stop();
                })
                .Execute(i =>
                {
                    skullEyes.Power = true;
                    candySpot.SetColor(Color.Red);

                    i.WaitFor(S(1.0));
                    lightBehindHeads.SetOnlyColor(Color.White);
                    popOutBehindHeads.Pop(1.0);
                    audioOla.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral", 0.0, 0.7);
                    i.WaitFor(S(10));
                })
                .TearDown(() =>
                {
                    audioOla.PlayBackground();
                    skullEyes.Power = false;
                    candySpot.SetOnlyColor(Color.Green);
                    pulsatingEffect1.Start();
                    pulsatingEffect2.Start();
                    //                        candySpot.SetColor(Color.Green);

                    //                        audioOla.PlayBackground();

                    if (stateMachine.CurrentState == States.StepForward)
                        stateMachine.SetState(States.Background);
                });

            thunderSeq.WhenExecuted
                .SetUp(() =>
                    {
                        audioOla.PauseBackground();
                        //                        movingHead.Pan = 0.25;
                        //                        movingHead.Tilt = 0.5;
                        Thread.Sleep(200);
                    })
                .Execute(i =>
                    {
                        audioOla.PlayTrack("08 Weather-lightning-strike2");
                        //                        movingHead.SetOnlyColor(Color.White);
                        popOut1.Pop(1.0);
                        popOut2.Pop(1.0);
                        popOut3.Pop(1.0);

                        i.WaitFor(S(4));
                    })
                .TearDown(() =>
                    {
                        audioOla.PauseTrack();
                        audioOla.PlayBackground();
                    });

            backgroundSeq.WhenExecuted
                .SetUp(() =>
                {
                    audioOla.NextBackgroundTrack();
                    flickerEffect.Start();
                    pulsatingEffect1.Start();
                    pulsatingEffect2.Start();
                })
                .Execute(i =>
                {
                    while (!i.IsCancellationRequested)
                    {
                        i.WaitFor(S(1));
                        if (!this.lastFogRun.HasValue || (DateTime.Now - this.lastFogRun.Value).TotalMinutes > 10)
                        {
                            // Run the fog for a little while
                            fog.Power = true;
                            i.WaitFor(S(4));
                            fog.Power = false;
                            this.lastFogRun = DateTime.Now;
                        }
                    }
                })
                .TearDown(() =>
                {
                    flickerEffect.Stop();
                    pulsatingEffect1.Stop();
                    pulsatingEffect2.Stop();
                    audioOla.PauseTrack();
                    audioOla.PauseBackground();
                });

            catSeq.WhenExecuted
                .Execute(instance =>
                {
                    var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                    catLights.Power = true;

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
                    catLights.Power = false;
                });
        }

        public override void Run()
        {
            movingHead.Pan = 106;
            movingHead.Tilt = 31;
            //            hoursSmall.SetForced(false);
        }

        public override void Stop()
        {
        }
    }
}
