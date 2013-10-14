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
    internal class HalloweenScene2013B : BaseScene, ISceneRequiresRaspExpander3, ISceneSupportsSimulator, ISceneRequiresDMXPro//, ISceneRequiresIOExpander
    {
        private AudioPlayer audioCat;
        private AudioPlayer audioGeorge;
        private AudioPlayer audioBeauty;
        private DigitalInput buttonMotion;
        private DigitalInput buttonTrigger1;
        private DigitalInput buttonDeadendDrive;
        private DigitalInput buttonTestA;
        private DigitalInput buttonTestB;
        private Switch switchDeadendDrive;
        private Switch catLights;
        private Switch cat;
        private MotorWithFeedback georgeMotor;
        private StrobeDimmer lightPopup;
        private StrobeColorDimmer lightGeorge;
        private StrobeColorDimmer lightBeauty;
        private StrobeColorDimmer lightFloor;
        private Switch switchHand;
        private Switch switchHead;
        private Switch switchDrawer1;
        private Switch switchDrawer2;
        private Switch switchPopEyes;
        private Switch switchPopUp;


        public HalloweenScene2013B(IEnumerable<string> args)
        {
            buttonMotion = new DigitalInput("Walkway Motion");
            buttonTrigger1 = new DigitalInput("Stairs Trigger 1");
            buttonDeadendDrive = new DigitalInput("Deadend dr");
            buttonTestA = new DigitalInput("Test A");
            buttonTestB = new DigitalInput("Test B");

            switchDeadendDrive = new Switch("Deadend dr");
            catLights = new Switch("Cat lights");
            cat = new Switch("Cat");

            georgeMotor = new MotorWithFeedback("George Motor");
            lightPopup = new StrobeDimmer("Popup light");
            lightGeorge = new StrobeColorDimmer("George light");
            lightBeauty = new StrobeColorDimmer("Beauty light");
            lightFloor = new StrobeColorDimmer("Floor light");

            audioCat = new AudioPlayer("Audio Cat");
            audioGeorge = new AudioPlayer("Audio George");
            audioBeauty = new AudioPlayer("Audio Beauty");

            switchHand = new Switch("Hand");
            switchHead = new Switch("Head");
            switchDrawer1 = new Switch("Drawer 1");
            switchDrawer2 = new Switch("Drawer 2");
            switchPopEyes = new Switch("Pop Eyes");
            switchPopUp = new Switch("Pop Up");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonMotion);
            sim.AddDigitalInput_Momentarily(buttonTrigger1);
            sim.AddDigitalInput_Momentarily(buttonDeadendDrive);

            sim.AddDigitalInput_Momentarily(buttonTestA);
            sim.AddDigitalInput_Momentarily(buttonTestB);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.IOExpander port)
        {
            port.Motor.Connect(georgeMotor);
        }

        // Cat
        public void WireUp1(Expander.Raspberry port)
        {
            port.DigitalInputs[0].Connect(buttonMotion);
            port.DigitalInputs[1].Connect(buttonTrigger1);
            port.DigitalOutputs[0].Connect(switchDeadendDrive);

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
        }

        // Background/George
        public void WireUp3(Expander.Raspberry port)
        {
            port.Connect(audioGeorge);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.GenericDimmer(catLights, 1));
            port.Connect(new Physical.GenericDimmer(cat, 2));
            port.Connect(new Physical.AmericanDJStrobe(lightPopup, 5));
            port.Connect(new Physical.RGBStrobe(lightGeorge, 40));
            port.Connect(new Physical.RGBStrobe(lightBeauty, 30));
            port.Connect(new Physical.SmallRGBStrobe(lightFloor, 20));
        }

        public override void Start()
        {
            var popupSeq = new Controller.Sequence("Popup Sequence");
            popupSeq.WhenExecuted
                .Execute(instance =>
                    {
                        audioBeauty.PlayEffect("laugh", 0.0, 1.0);
                        switchPopEyes.SetPower(true);
                        instance.WaitFor(TimeSpan.FromSeconds(1.0));
                        lightPopup.SetBrightness(1.0);
                        switchPopUp.SetPower(true);

                        instance.WaitFor(TimeSpan.FromSeconds(5));

                        lightPopup.TurnOff();
                        switchPopEyes.TurnOff();
                        switchPopUp.TurnOff();
                    });

            var catSeq = new Controller.Sequence("Cat Sequence");
            catSeq.WhenExecuted
                .Execute(instance =>
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        var random = new Random();

                        catLights.SetPower(true);

                        if (random.Next(20) == 0)
                        {
                            switchDeadendDrive.SetPower(true);
                            instance.WaitFor(TimeSpan.FromSeconds(1));
                            switchDeadendDrive.SetPower(false);
                        }

                        while (true)
                        {
                            switch (random.Next(4))
                            {
                                case 0:
                                    audioCat.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
                                    Thread.Sleep(TimeSpan.FromSeconds(2.0));
                                    break;
                                case 1:
                                    audioCat.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
                                    Thread.Sleep(TimeSpan.FromSeconds(3.0));
                                    break;
                                case 2:
                                    audioCat.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
                                    Thread.Sleep(TimeSpan.FromSeconds(2.5));
                                    break;
                                case 3:
                                    audioCat.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
                                    Thread.Sleep(TimeSpan.FromSeconds(1.5));
                                    break;
                                default:
                                    Thread.Sleep(TimeSpan.FromSeconds(3.0));
                                    break;
                            }

                            if (instance.IsCancellationRequested && !watch.IsRunning)
                            {
                                watch.Start();
                            }

                            if (watch.Elapsed > TimeSpan.FromSeconds(6))
                                break;
                        }

                        catLights.TurnOff();
                    });

            buttonTestA.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        audioBeauty.PlayEffect("348 Spider Hiss", 1.0, 0.0);
                        switchHand.SetPower(true);
                        audioGeorge.PlayBackground();
                        lightBeauty.SetBrightness(1.0);
                        lightFloor.SetColor(Color.Yellow);
                        Thread.Sleep(5000);
                        lightGeorge.TurnOff();
                        lightPopup.TurnOff();
                        lightBeauty.TurnOff();
                        lightFloor.TurnOff();
                        switchHand.SetPower(false);

                        //georgeMotor.SetVector(1.0, 350, S(10));
                        //georgeMotor.WaitForVectorReached();
                        //Thread.Sleep(5000);
                        //georgeMotor.SetVector(0.9, 0, S(10));
                        //georgeMotor.WaitForVectorReached();

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

            buttonMotion.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        //                        Executor.Current.Execute(catSeq);
                    }
                    else
                    {
                        Executor.Current.Cancel(catSeq);
                    }
                };

            buttonTrigger1.ActiveChanged += (sender, e) =>
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
        }

        public override void Run()
        {
            //            audioPlayer.PlayEffect("Laugh");
            cat.SetPower(true);
        }

        public override void Stop()
        {
        }
    }
}
