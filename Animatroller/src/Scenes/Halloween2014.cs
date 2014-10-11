using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Reactive;
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
        private const int midiChannel = 0;

        private Expander.MidiInput2 midiInput = new Expander.MidiInput2();
        private Expander.OscServer oscServer = new Expander.OscServer();
        private AudioPlayer audioCat = new AudioPlayer();
        private AudioPlayer audioReaper = new AudioPlayer();
        private AudioPlayer audioOla = new AudioPlayer();
        private Expander.Raspberry raspberryCat = new Expander.Raspberry("192.168.240.115:5005", 3333);
        private Expander.Raspberry raspberryReaper = new Expander.Raspberry("192.168.240.123:5005", 3334);
        private Expander.Raspberry raspberryOla = new Expander.Raspberry("192.168.240.147:5005", 3335);

        private MovingHead testLight1 = new MovingHead("Test 1");
        private StrobeColorDimmer2 reaperLight = new StrobeColorDimmer2("Reaper");
        private StrobeColorDimmer2 testLight2 = new StrobeColorDimmer2("Test Light 2");
        private Dimmer2 testLight3 = new Dimmer2("Test 3");
//        private MovingHead testLight4 = new MovingHead("Moving Head");
        private Dimmer2 lightning1 = new Dimmer2("Lightning 1");
        private Dimmer2 lightning2 = new Dimmer2("Lightning 2");
        private Dimmer2 lightStairs1 = new Dimmer2("Stairs 1");
        private Dimmer2 lightStairs2 = new Dimmer2("Stairs 2");
        private DigitalInput2 buttonCatTrigger = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest1 = new DigitalInput2("Test 1");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 forceOpen = new DigitalInput2("Force Open", true);
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest2 = new DigitalInput2("Test 2");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest3 = new DigitalInput2("Test 3");
        private DigitalInput2 catMotion = new DigitalInput2();
        private AnalogInput2 inputBrightness = new AnalogInput2("Brightness");
        private AnalogInput2 inputH = new AnalogInput2("Hue", true);
        private AnalogInput2 inputS = new AnalogInput2("Saturation", true);
        private AnalogInput2 inputStrobe = new AnalogInput2("Strobe", true);
        private AnalogInput2 inputPan = new AnalogInput2("Pan", true);
        private AnalogInput2 inputTilt = new AnalogInput2("Tilt", true);
        private Expander.AcnStream acnOutput = new Expander.AcnStream();
        private DigitalOutput2 catAir = new DigitalOutput2();
        private DigitalOutput2 catLights = new DigitalOutput2();
        private DigitalOutput2 reaperPopUp = new DigitalOutput2();
        private DigitalOutput2 reaperEyes = new DigitalOutput2();

        private OperatingHours2 hoursSmall = new OperatingHours2("Hours Small");
        private OperatingHours2 hoursFull = new OperatingHours2("Hours Full");

        // Effects
        private Effect.Flicker flickerEffect = new Effect.Flicker("Flicker", 0.4, 0.6, false);
        private Effect.PopOut2 popOut1 = new Effect.PopOut2("PopOut 1", S(0.3));
        private Effect.PopOut2 popOut2 = new Effect.PopOut2("PopOut 2", S(0.3));

        private Controller.Sequence catSeq = new Controller.Sequence();
        private Controller.Sequence thunderSeq = new Controller.Sequence();
        private Controller.Timeline<string> timelineThunder1 = new Controller.Timeline<string>(1);
        private Controller.Sequence reaperSeq = new Controller.Sequence("Reaper");

        public Halloween2014(IEnumerable<string> args)
        {
            hoursSmall.AddRange("5:00 pm", "9:00 pm");
            hoursFull.AddRange("5:00 pm", "9:00 pm");

            flickerEffect.ConnectTo(lightStairs1.InputBrightness);
            flickerEffect.ConnectTo(lightStairs2.InputBrightness);

            popOut1.AddDevice(lightning1.InputBrightness);
            popOut2.AddDevice(lightning2.InputBrightness);

            raspberryCat.DigitalInputs[4].Connect(catMotion, true);
            raspberryCat.Connect(audioCat);
            raspberryReaper.Connect(audioReaper);
            raspberryOla.Connect(audioOla);

            inputBrightness.ConnectTo(testLight1.InputBrightness);

            // Map Physical lights
            acnOutput.Connect(new Physical.SmallRGBStrobe(reaperLight, 1), 20);
            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(testLight2, 20), 20);
            acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(testLight1, 30), 20);
            acnOutput.Connect(new Physical.GenericDimmer(catAir, 11), 20);
            acnOutput.Connect(new Physical.GenericDimmer(catLights, 10), 20);
            //BROKEN            acnOutput.Connect(new Physical.GenericDimmer(testLight3, 103), 20);
            acnOutput.Connect(new Physical.GenericDimmer(lightning2, 100), 20);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 101), 20);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 102), 20);
            acnOutput.Connect(new Physical.AmericanDJStrobe(lightning1, 5), 20);

            buttonTest2.Control.Subscribe(x =>
                {
                    if (x)
                    {
                        Exec.Execute(thunderSeq);
                    }
                });

            raspberryOla.AudioTrackStart.Subscribe(x =>
                {
                    // Next track
                });

            buttonTest3.Control.Subscribe(x =>
                {
                    if (x)
                        Exec.Execute(reaperSeq);
                    //                    lightning2.Brightness = x ? 1.0 : 0.0;
                    //                    if (x)
                    //                        popOut1.Pop(1.0);
                    //                    if (x)
                    //                    lightStairs1.Brightness = x ? 1.0 : 0.0;
                    //                    testLight3.Brightness = x ? 1.0 : 0.0;
                    //                        audioSpider.PlayEffect("348 Spider Hiss");
                    //                    Exec.Execute(thunderSeq);
                });

            raspberryReaper.DigitalOutputs[7].Connect(reaperPopUp);
            raspberryReaper.DigitalOutputs[6].Connect(reaperEyes);

            forceOpen.Output.Subscribe(x =>
                {
                    if (x)
                        hoursSmall.SetForced(true);
                    else
                        hoursSmall.SetForced(null);
                });

            inputH.Output.Subscribe(x =>
            {
                testLight1.SetOnlyColor(HSV.ColorFromHSV(x.Value.GetByteScale(), inputS.Value, 1.0));
            });

            inputS.Output.Subscribe(x =>
            {
                testLight1.SetOnlyColor(HSV.ColorFromHSV(inputH.Value.GetByteScale(), x.Value, 1.0));
            });

            inputStrobe.Output.Controls(testLight1.InputStrobeSpeed);

            midiInput.Controller(midiChannel, 1).Controls(inputBrightness.Control);
            midiInput.Controller(midiChannel, 2).Controls(inputH.Control);
            midiInput.Controller(midiChannel, 3).Controls(inputS.Control);
            midiInput.Controller(midiChannel, 4).Controls(inputStrobe.Control);

            midiInput.Controller(midiChannel, 5).Controls(inputPan.Control);
            midiInput.Controller(midiChannel, 6).Controls(inputTilt.Control);

            midiInput.Note(midiChannel, 36).Controls(buttonTest1.Control);
            midiInput.Note(midiChannel, 37).Controls(buttonTest2.Control);
            midiInput.Note(midiChannel, 38).Controls(buttonTest3.Control);
            midiInput.Note(midiChannel, 39).Controls(catMotion.Control);

            midiInput.Note(midiChannel, 40).Controls(buttonCatTrigger.Control);


            inputPan.Output.Controls(testLight1.InputPan);
            inputTilt.Output.Controls(testLight1.InputTilt);

            //            buttonTest2.Output.Subscribe(reaperPopUp.InputPower);
            catMotion.Output.Subscribe(catLights.InputPower);

            buttonTest1.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        testLight1.Brightness = 1;
                        testLight1.Color = Color.Red;
//                        testLight4.StrobeSpeed = 1.0;
                    }
                    else
                    {
                        testLight1.TurnOff();
                    }
                });

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
                        audioCat.PlayNewEffect("386 Demon Creature Growls");
                        Thread.Sleep(2000);
                        catLights.Power = false;
                    }
                });

            buttonTest1.Output.Subscribe(x =>
            {
                if (x)
                {
                    testLight1.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));
                }
                else
                {
                    if (testLight1.Brightness > 0)
                        testLight1.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
                }
            });

            hoursSmall.Output.Subscribe(catAir.InputPower);
            hoursSmall.Output.Subscribe(flickerEffect.InputRun);

            hoursSmall.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        audioOla.PlayBackground();
                    }
                    else
                    {
                        audioOla.PauseBackground();
                    }
                });

            timelineThunder1.AddMs(500, "A");
            timelineThunder1.AddMs(1500, "B");
            timelineThunder1.AddMs(1600, "C");

            timelineThunder1.TimelineTrigger += (sender, e) =>
                {
                    switch (e.Code)
                    {
                        case "A":
                            popOut2.Pop(0.5);
                            break;

                        case "B":
                            popOut2.Pop(1.0);
                            break;

                        case "C":
                            popOut1.Pop(1.0);
                            break;
                    }
                };
        }

        public override void Start()
        {
            reaperSeq.WhenExecuted
                .Execute(instance =>
                {
                    //                    switchFog.SetPower(true);
                    //                    this.lastFogRun = DateTime.Now;
                    //                    Executor.Current.Execute(deadendSeq);
                    //                    audioGeorge.PlayEffect("ghostly");
                    //                    instance.WaitFor(S(0.5));
                    //                    popOutEffect.Pop(1.0);

                    //                    instance.WaitFor(S(1.0));
                    audioReaper.PlayEffect("laugh");
                    instance.WaitFor(S(0.1));
                    reaperPopUp.Power = true;
                    reaperLight.Color = Color.Red;
                    reaperLight.Brightness = 1;
                    reaperLight.StrobeSpeed = 1;
                    instance.WaitFor(S(0.5));
                    reaperEyes.Power = true;
                    instance.WaitFor(S(2));
                    reaperPopUp.Power = false;
                    reaperEyes.Power = false;
                    reaperLight.TurnOff();
                    instance.WaitFor(S(4));
                    //                    stateMachine.NextState();
                })
                .TearDown(() =>
                {
                    //                    switchFog.SetPower(false);
                    //                    audioGeorge.PauseFX();
                });

            thunderSeq.WhenExecuted
                .SetUp(() =>
                    {
                        audioOla.CueTrack("56 Lightning Strike Thunder");
                        System.Threading.Thread.Sleep(800);
                    })
                .Execute(i =>
                    {
                        var task = timelineThunder1.Start();
                        audioOla.ResumeTrack();
                        task.Wait(i.CancelToken);

                        i.WaitFor(S(4));
                    })
                .TearDown(() =>
                    {
                        timelineThunder1.Stop();
                        audioOla.PauseTrack();
                    });

            catSeq.WhenExecuted
                .Execute(instance =>
                {
                    var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                    var random = new Random();

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
            //            hoursSmall.SetForced(false);
        }

        public override void Stop()
        {
        }
    }
}
