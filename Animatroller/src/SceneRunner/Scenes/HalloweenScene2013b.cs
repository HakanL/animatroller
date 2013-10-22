using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
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
    internal class HalloweenScene2013B : BaseScene,
        ISceneRequiresRaspExpander1,
        ISceneRequiresRaspExpander2,
        ISceneRequiresRaspExpander3,
        ISceneRequiresRaspExpander4,
        ISceneRequiresDMXPro,
    //, ISceneRequiresIOExpander
        ISceneSupportsSimulator
    {
        public enum States
        {
            Background,
            Stair,
            George,
            Popup
        }

        private Controller.StateMachine<States> stateMachine;
        private OperatingHours hoursSmall;
        private OperatingHours hoursFull;
        private AudioPlayer audioCat;
        private AudioPlayer audioGeorge;
        private AudioPlayer audioBeauty;
        private AudioPlayer audioSpider;
        private DigitalInput buttonMotionCat;
        private DigitalInput buttonMotionBeauty;
        private DigitalInput buttonTriggerStairs;
        private DigitalInput buttonTriggerPopup;
        private DigitalInput buttonTestC;
        private DigitalInput buttonTestA;
        private DigitalInput buttonTestB;
        private DigitalInput buttonTestSpider;
        private Switch switchDeadendDrive;
        private Switch catLights;
        private Switch catFan;
        private MotorWithFeedback georgeMotor;
        private StrobeDimmer lightPopup;
        private StrobeColorDimmer lightGeorge;
        private StrobeColorDimmer lightBeauty;
        private StrobeColorDimmer lightFloor;
        private Dimmer skullsLight;
        private Dimmer skullsLight2;
        private Dimmer lightSpiderWeb;
        private Switch lightEyes;
        private Switch switchHand;
        private Switch switchHead;
        private Switch switchDrawer1;
        private Switch switchDrawer2;
        private Switch switchPopEyes;
        private Switch switchPopUp;
        private Switch switchSpider;
        private Switch switchFog;
        private Effect.Pulsating pulsatingEffect1;
        private Effect.Flicker flickerEffect;
        private Effect.Flicker flickerEffect2;
        private Effect.PopOut popOutEffect;


        public HalloweenScene2013B(IEnumerable<string> args)
        {
            stateMachine = new Controller.StateMachine<States>("Main");
            stateMachine.SetBackgroundState(States.Background);

            pulsatingEffect1 = new Effect.Pulsating("Pulse FX 1", S(2), 0.1, 0.5, false);
            flickerEffect = new Effect.Flicker("Flicker", 0.4, 0.6, false);
            flickerEffect2 = new Effect.Flicker("Flicker 2", 0.4, 0.6, false);
            popOutEffect = new Effect.PopOut("PopOut", S(1));

            hoursSmall = new OperatingHours("Hours Small");
            hoursFull = new OperatingHours("Hours Full");
            buttonMotionCat = new DigitalInput("Walkway Motion");
            buttonMotionBeauty = new DigitalInput("Beauty Motion");
            buttonTriggerStairs = new DigitalInput("Stairs Trigger 1");
            buttonTriggerPopup = new DigitalInput("Popup Trigger");
            buttonTestA = new DigitalInput("Test A");
            buttonTestB = new DigitalInput("Test B");
            buttonTestC = new DigitalInput("Test C");
            buttonTestSpider = new DigitalInput("Spider");

            switchDeadendDrive = new Switch("Deadend dr");
            catLights = new Switch("Cat lights");
            catFan = new Switch("Cat");

            georgeMotor = new MotorWithFeedback("George Motor");
            lightPopup = new StrobeDimmer("Popup light");
            lightGeorge = new StrobeColorDimmer("George light");
            lightBeauty = new StrobeColorDimmer("Beauty light");
            lightFloor = new StrobeColorDimmer("Floor light");
            skullsLight = new Dimmer("Skulls");
            skullsLight2 = new Dimmer("Skulls 2");
            lightSpiderWeb = new Dimmer("Spiderweb");
            lightEyes = new Switch("Eyes");

            audioCat = new AudioPlayer("Audio Cat");
            audioGeorge = new AudioPlayer("Audio George");
            audioBeauty = new AudioPlayer("Audio Beauty");
            audioSpider = new AudioPlayer("Audio Spider");

            switchHand = new Switch("Hand");
            switchHead = new Switch("Head");
            switchDrawer1 = new Switch("Drawer 1");
            switchDrawer2 = new Switch("Drawer 2");
            switchPopEyes = new Switch("Pop Eyes");
            switchPopUp = new Switch("Pop Up");
            switchSpider = new Switch("Spider");
            switchFog = new Switch("Fog");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonMotionCat);
            sim.AddDigitalInput_Momentarily(buttonMotionBeauty);
            sim.AddDigitalInput_Momentarily(buttonTriggerStairs);
            sim.AddDigitalInput_Momentarily(buttonTriggerPopup);
            sim.AddDigitalInput_FlipFlop(buttonTestC);

            sim.AddDigitalInput_Momentarily(buttonTestA);
            sim.AddDigitalInput_Momentarily(buttonTestB);
            sim.AddDigitalInput_FlipFlop(buttonTestSpider);

            sim.AutoWireUsingReflection(this);
        }

        // Cat
        public void WireUp1(Expander.Raspberry port)
        {
            port.DigitalInputs[0].Connect(buttonMotionCat);
            port.DigitalInputs[4].Connect(buttonTriggerStairs, true);
            port.DigitalOutputs[0].Connect(switchDeadendDrive);
            port.DigitalOutputs[1].Connect(switchFog);
            port.Motor.Connect(georgeMotor);

            port.Connect(audioCat);
        }

        // Beauty
        public void WireUp2(Expander.Raspberry port)
        {
            port.Connect(audioBeauty);
            port.DigitalOutputs[7].Connect(switchHand);
            port.DigitalOutputs[2].Connect(switchHead);
            port.DigitalOutputs[5].Connect(switchDrawer1);
            port.DigitalOutputs[6].Connect(switchDrawer2);
            port.DigitalOutputs[3].Connect(switchPopEyes);
            port.DigitalOutputs[4].Connect(switchPopUp);

            port.DigitalInputs[5].Connect(buttonMotionBeauty, true);
            port.DigitalInputs[6].Connect(buttonTriggerPopup, true);
        }

        // Background/George
        public void WireUp3(Expander.Raspberry port)
        {
            port.Connect(audioGeorge);
        }

        // Spider
        public void WireUp4(Expander.Raspberry port)
        {
            port.Connect(audioSpider);

            port.DigitalOutputs[0].Connect(switchSpider);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.GenericDimmer(catLights, 1));
            port.Connect(new Physical.GenericDimmer(catFan, 2));
            port.Connect(new Physical.AmericanDJStrobe(lightPopup, 5));
            port.Connect(new Physical.RGBStrobe(lightGeorge, 40));
            port.Connect(new Physical.RGBStrobe(lightBeauty, 30));
            port.Connect(new Physical.RGBStrobe(lightFloor, 20));

            port.Connect(new Physical.GenericDimmer(skullsLight, 100));
            port.Connect(new Physical.GenericDimmer(lightSpiderWeb, 104));
            port.Connect(new Physical.GenericDimmer(skullsLight2, 105));
            port.Connect(new Physical.GenericDimmer(lightEyes, 106));
        }

        public override void Start()
        {
            hoursSmall.AddRange("5:00 pm", "9:00 pm");
            hoursFull.AddRange("5:00 pm", "9:00 pm");

#if true
            hoursFull.SetForced(true);

            audioBeauty.SetSilent(true);
            audioCat.SetSilent(true);
            audioGeorge.SetSilent(true);
            audioSpider.SetSilent(true);
            georgeMotor.SetDisabled(true);
#endif
            var backgroundSeq = new Controller.Sequence("BG Sequence");
            backgroundSeq.WhenExecuted
                .SetUp(() =>
                    {
                        audioGeorge.PlayBackground();
                        lightFloor.SetOnlyColor(Color.Orange);
                        pulsatingEffect1.Start();
                        flickerEffect.Start();
                    })
                .Execute(instance =>
                    {
                        instance.WaitUntilCancel();
                    })
                .TearDown(() =>
                    {
                        audioGeorge.PauseBackground();
                        pulsatingEffect1.Stop();
                        flickerEffect.Stop();
                    });

            var deadendSeq = new Controller.Sequence("Deadend dr");
            deadendSeq.WhenExecuted
                .Execute(instance =>
                    {
                        switchDeadendDrive.SetPower(true);
                        Thread.Sleep(1000);
                        switchDeadendDrive.SetPower(false);
                    });

            var stairSeq = new Controller.Sequence("Stair Sequence");
            stairSeq.WhenExecuted
                .SetUp(() =>
                    {
                    })
                .Execute(instance =>
                    {
                        Executor.Current.Execute(deadendSeq);
                        audioSpider.PlayEffect("door-creak", 0.8);
                        audioGeorge.PlayEffect("ghostly");
                        instance.WaitFor(S(0.5));
                        popOutEffect.Pop(1.0);

                        instance.WaitFor(S(2));
                        audioGeorge.PlayNewEffect("scream");
                        instance.WaitFor(S(7));
                        stateMachine.NextState();
                    })
                .TearDown(() =>
                    {
                        audioGeorge.PauseFX();
//                        Executor.Current.Execute(backgroundSeq);
                    });

            var georgeSeq = new Controller.Sequence("George Sequence");
            georgeSeq.WhenExecuted
                .Execute(instance =>
                {
                    switchFog.SetPower(true);
                    audioGeorge.PlayEffect("laugh");
                    georgeMotor.SetVector(1.0, 350, S(10));
                    instance.WaitFor(TimeSpan.FromSeconds(0.8));
                    lightGeorge.SetColor(Color.Red);
                    georgeMotor.WaitForVectorReached();
                    instance.WaitFor(TimeSpan.FromSeconds(2));
                    georgeMotor.SetVector(0.9, 0, S(15));
                    lightGeorge.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
                    instance.WaitFor(TimeSpan.FromSeconds(1));
                    lightFloor.SetOnlyColor(Color.Green);
                    pulsatingEffect1.Start();
                    georgeMotor.WaitForVectorReached();
                    switchFog.SetPower(false);

                    instance.WaitUntilCancel();
                })
                .TearDown(() =>
                {
                    georgeMotor.SetVector(0.9, 0, S(15));
                    switchFog.SetPower(false);
                    pulsatingEffect1.Stop();
                });

            var popupSeq = new Controller.Sequence("Popup Sequence");
            popupSeq.WhenExecuted
                .Execute(instance =>
                    {
                        audioBeauty.PlayEffect("scream", 0.0, 1.0);
                        switchPopEyes.SetPower(true);
                        instance.WaitFor(TimeSpan.FromSeconds(1.0));
                        lightPopup.SetBrightness(1.0);
                        switchPopUp.SetPower(true);

                        instance.WaitFor(S(3));

                        lightPopup.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
                        switchPopEyes.TurnOff();
                        switchPopUp.TurnOff();
                    });

            var catSeq = new Controller.Sequence("Cat Sequence");
            catSeq.WhenExecuted
                .Execute(instance =>
                    {
                        var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                        var random = new Random();

//                        catLights.SetPower(true);

                        //if (random.Next(20) == 0)
                        //{
                        //    switchDeadendDrive.SetPower(true);
                        //    instance.WaitFor(TimeSpan.FromSeconds(1));
                        //    switchDeadendDrive.SetPower(false);
                        //}

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

                            if (maxRuntime.Elapsed.TotalSeconds > 15)
                                break;
                        }

//                        catLights.TurnOff();
                    });


            stateMachine.ForFromSequence(States.Background, backgroundSeq);
            stateMachine.ForFromSequence(States.Stair, stairSeq);
            stateMachine.ForFromSequence(States.George, georgeSeq);
            stateMachine.ForFromSequence(States.Popup, popupSeq);

            hoursSmall.OpenHoursChanged += (sender, e) =>
                {
                    if (e.IsOpenNow)
                    {
                        flickerEffect2.Start();
                        stateMachine.SetState(States.Background);
                        catFan.SetPower(true);
                        lightEyes.SetPower(true);
                    }
                    else
                    {
                        flickerEffect2.Stop();
                        stateMachine.Hold();
                        catFan.SetPower(false);
                        lightEyes.SetPower(false);
                        audioGeorge.PauseBackground();
                    }
                };

            buttonTestA.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        audioSpider.PlayEffect("gollum_precious1");
//                        switchHand.SetPower(true);
//                        audioGeorge.PlayBackground();
//                        lightBeauty.SetBrightness(1.0);
//                        Thread.Sleep(5000);
//                        lightGeorge.TurnOff();
//                        lightPopup.TurnOff();
//                        lightBeauty.TurnOff();
////                        lightFloor.TurnOff();
//                        switchHand.SetPower(false);


                        //                        audioPlayer.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
                        //                        System.Threading.Thread.Sleep(3000);
                        //                        audioPlayer.PlayEffect("laugh", 0.0, 1.0);
                    }
                };

            buttonTestB.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    audioBeauty.PlayEffect("gollum_precious1", 0.0, 1.0);
                    switchHead.SetPower(true);
                    lightPopup.SetBrightness(1.0);
                    Thread.Sleep(5000);
                    lightGeorge.TurnOff();
                    lightPopup.TurnOff();
                    lightBeauty.TurnOff();
                    switchHand.SetPower(false);
                    switchHead.SetPower(false);
                }
            };

            buttonTestSpider.ActiveChanged += (sender, e) =>
                {
                    switchSpider.SetPower(e.NewState);
                };

            buttonMotionCat.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        if (hoursSmall.IsOpen)
                        {
                            catLights.SetPower(true);
                            Executor.Current.Execute(catSeq);
                        }
                    }
                    else
                    {
                        catLights.SetPower(false);
                    }
                };

            buttonMotionBeauty.ActiveChanged += (sender, e) =>
                {
                    lightBeauty.SetColor(Color.Purple, e.NewState ? 0.4 : 0.0);
                    switchHand.SetPower(e.NewState);
                    switchHead.SetPower(e.NewState);
                };

            buttonTriggerStairs.ActiveChanged += (sender, e) =>
                {
#if CHECK_SENSOR_ALIGNMENT
                    lightFloor.SetColor(Color.Purple, e.NewState ? 1.0 : 0.0);
#else
                    if (e.NewState && hoursFull.IsOpen)
                    {
                        if(stateMachine.CurrentState == States.Background)
                            stateMachine.SetState(States.Stair);
//                        Executor.Current.Execute(stairSeq);
//                        Executor.Current.Execute(georgeSeq);
                    }
#endif
                };

            buttonTriggerPopup.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    if (stateMachine.CurrentState == States.George)
                        stateMachine.SetState(States.Popup);
                }
            };

            buttonTestC.ActiveChanged += (sender, e) =>
            {
                switchFog.SetPower(e.NewState);
            };

            flickerEffect.AddDevice(skullsLight);
            flickerEffect2.AddDevice(skullsLight2);
            lightFloor.SetColor(Color.Orange, 0);
            pulsatingEffect1.AddDevice(lightFloor);
            pulsatingEffect1.AddDevice(lightSpiderWeb);

            popOutEffect.AddDevice(skullsLight);
        }

        public override void Run()
        {
//            hours.SetForceOpen(true);
        }

        public override void Stop()
        {
        }
    }
}
