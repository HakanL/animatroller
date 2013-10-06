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
    internal class HalloweenScene2013B : BaseScene, ISceneSupportsRaspExpander, ISceneSupportsSimulator, ISceneSupportsDMXPro
    {
        private AudioPlayer audioPlayer;
        private DigitalInput buttonMotion;
        private DigitalInput buttonDeadendDrive;
        private DigitalInput buttonTestSound;
        private Switch switchDeadendDrive;
        private Switch catLights;


        public HalloweenScene2013B(IEnumerable<string> args)
        {
            buttonMotion = new DigitalInput("Walkway Motion");
            buttonDeadendDrive = new DigitalInput("Deadend dr");
            buttonTestSound = new DigitalInput("Test Sound");
            
            switchDeadendDrive = new Switch("Deadend dr");
            catLights = new Switch("Cat lights");
            
            audioPlayer = new AudioPlayer("Audio Player");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonMotion);
            sim.AddDigitalInput_Momentarily(buttonDeadendDrive);
            
            sim.AddDigitalInput_Momentarily(buttonTestSound);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.Raspberry port)
        {
            port.DigitalInputs[0].Connect(buttonMotion);
            port.DigitalInputs[1].Connect(buttonDeadendDrive);
            port.DigitalOutputs[0].Connect(switchDeadendDrive);

            port.Connect(audioPlayer);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.GenericDimmer(catLights, 1));
        }

        public override void Start()
        {
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
                                    audioPlayer.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
                                    Thread.Sleep(TimeSpan.FromSeconds(2.0));
                                    break;
                                case 1:
                                    audioPlayer.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
                                    Thread.Sleep(TimeSpan.FromSeconds(3.0));
                                    break;
                                case 2:
                                    audioPlayer.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
                                    Thread.Sleep(TimeSpan.FromSeconds(2.5));
                                    break;
                                case 3:
                                    audioPlayer.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
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

            buttonTestSound.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        audioPlayer.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
//                        System.Threading.Thread.Sleep(3000);
//                        audioPlayer.PlayEffect("laugh", 0.0, 1.0);
                    }
                };

            buttonMotion.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        Executor.Current.Execute(catSeq);
                    }
                    else
                    {
                        Executor.Current.Cancel(catSeq);
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
        }

        public override void Stop()
        {
        }
    }
}
