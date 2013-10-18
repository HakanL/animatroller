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
/*        ISceneRequiresRaspExpander1,
        ISceneRequiresRaspExpander2,
        ISceneRequiresRaspExpander3,
        ISceneRequiresRaspExpander4,
        ISceneRequiresDMXPro,*/
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
        private OperatingHours hours;
        private AudioPlayer audioCat;
        private AudioPlayer audioGeorge;
        private AudioPlayer audioBeauty;
        private AudioPlayer audioSpider;
        private DigitalInput buttonMotion;
        private DigitalInput buttonTrigger1;
        private DigitalInput buttonTriggerPopup;
        private DigitalInput buttonDeadendDrive;
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
        private Switch switchHand;
        private Switch switchHead;
        private Switch switchDrawer1;
        private Switch switchDrawer2;
        private Switch switchPopEyes;
        private Switch switchPopUp;
        private Switch switchSpider;
        private Effect.Pulsating pulsatingEffect1;
        private Effect.Flicker flickerEffect;
        private Effect.PopOut popOutEffect;


        public HalloweenScene2013B(IEnumerable<string> args)
        {
            stateMachine = new Controller.StateMachine<States>("Main");

            pulsatingEffect1 = new Effect.Pulsating("Pulse FX 1", S(2), 0.1, 0.5);
            flickerEffect = new Effect.Flicker("Flicker", 0.4, 0.6);
            popOutEffect = new Effect.PopOut("PopOut", S(1));

            hours = new OperatingHours("Hours");
            buttonMotion = new DigitalInput("Walkway Motion");
            buttonTrigger1 = new DigitalInput("Stairs Trigger 1");
            buttonTriggerPopup = new DigitalInput("Popup Trigger");
            buttonDeadendDrive = new DigitalInput("Deadend dr");
            buttonTestA = new DigitalInput("Test A");
            buttonTestB = new DigitalInput("Test B");
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
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonMotion);
            sim.AddDigitalInput_Momentarily(buttonTrigger1);
            sim.AddDigitalInput_Momentarily(buttonTriggerPopup);
            sim.AddDigitalInput_Momentarily(buttonDeadendDrive);

            sim.AddDigitalInput_Momentarily(buttonTestA);
            sim.AddDigitalInput_Momentarily(buttonTestB);
            sim.AddDigitalInput_FlipFlop(buttonTestSpider);

            sim.AutoWireUsingReflection(this);
        }

        // Cat
        public void WireUp1(Expander.Raspberry port)
        {
            port.DigitalInputs[0].Connect(buttonMotion);
            port.DigitalInputs[1].Connect(buttonTrigger1);
            port.DigitalOutputs[0].Connect(switchDeadendDrive);
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

            port.DigitalInputs[6].Connect(buttonTriggerPopup);
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
            port.Connect(new Physical.SmallRGBStrobe(lightFloor, 20));

            port.Connect(new Physical.GenericDimmer(skullsLight, 100));
            port.Connect(new Physical.GenericDimmer(skullsLight2, 104));
        }

        public override void Start()
        {
            hours.AddRange("5:00 pm", "9:00 pm");

            var backgroundSeq = new Controller.Sequence("BG Sequence");
            backgroundSeq.WhenExecuted
                .SetUp(() =>
                    {
                        audioGeorge.PlayBackground();
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

            var stairSeq = new Controller.Sequence("Stair Sequence");
            stairSeq.WhenExecuted
                .SetUp(() =>
                    {
//                        Executor.Current.Cancel(backgroundSeq);
                        Thread.Sleep(500);
                        popOutEffect.Pop(1.0);
                        audioGeorge.PlayEffect("ghostly");
//                        audioBeauty.PlayEffect("door-creak", 0.2);
                    })
                .Execute(instance =>
                    {
                        instance.WaitFor(S(2));
                        audioGeorge.PlayNewEffect("sixthsense-deadpeople");
                        instance.WaitFor(S(10));
//                        stateMachine.SetMomentaryState(States.George);
                    })
                .TearDown(() =>
                    {
                        audioGeorge.PauseFX();
//                        Executor.Current.Execute(backgroundSeq);
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

                        instance.WaitFor(TimeSpan.FromSeconds(5));

                        lightPopup.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
                        switchPopEyes.TurnOff();
                        switchPopUp.TurnOff();
                        instance.WaitFor(TimeSpan.FromSeconds(6));
                    });

            var georgeSeq = new Controller.Sequence("George Sequence");
            georgeSeq.WhenExecuted
                .Execute(instance =>
                    {
                        audioGeorge.PlayEffect("laugh");
//                        georgeMotor.SetVector(1.0, 350, S(10));
                        instance.WaitFor(TimeSpan.FromSeconds(0.8));
                        lightGeorge.SetColor(Color.Red);
//                        georgeMotor.WaitForVectorReached();
                        instance.WaitFor(TimeSpan.FromSeconds(2));
//                        georgeMotor.SetVector(0.9, 0, S(15));
                        lightGeorge.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
//                        georgeMotor.WaitForVectorReached();

                        instance.WaitFor(S(0.5));
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

            hours.OpenHoursChanged += (sender, e) =>
                {
                    if (e.IsOpenNow)
                    {
                        stateMachine.SetState(States.Background);
//FIXME                        catFan.SetPower(true);
                    }
                    else
                    {
                        stateMachine.Hold();
                        catFan.SetPower(false);
                    }
                };

            buttonTestA.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        audioBeauty.PlayEffect("348 Spider Hiss", 1.0, 0.0);
                        switchHand.SetPower(true);
                        audioGeorge.PlayBackground();
                        lightBeauty.SetBrightness(1.0);
                        Thread.Sleep(5000);
                        lightGeorge.TurnOff();
                        lightPopup.TurnOff();
                        lightBeauty.TurnOff();
//                        lightFloor.TurnOff();
                        switchHand.SetPower(false);


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

            buttonMotion.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        if (hours.IsOpen)
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

            buttonTrigger1.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        stateMachine.SetMomentaryState(States.Stair);
//                        Executor.Current.Execute(stairSeq);
//                        Executor.Current.Execute(georgeSeq);
                    }
                };

            buttonTriggerPopup.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    Executor.Current.Execute(popupSeq);
                }
            };

            buttonDeadendDrive.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    switchDeadendDrive.SetPower(true);
                    Thread.Sleep(1000);
                    switchDeadendDrive.SetPower(false);
                }
            };

            flickerEffect.AddDevice(skullsLight);
            flickerEffect.AddDevice(skullsLight2);
            lightFloor.SetColor(Color.Orange, 0);
            pulsatingEffect1.AddDevice(lightFloor);

            popOutEffect.AddDevice(skullsLight);

            flickerEffect.Stop();
            pulsatingEffect1.Stop();
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
