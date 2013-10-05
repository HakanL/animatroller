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
    internal class HalloweenScene2013 : BaseScene, ISceneSupportsRaspExpander, ISceneSupportsSimulator
    {
        private AudioPlayer audioPlayer;
        private DigitalInput buttonTestHand;
        private DigitalInput buttonTestHead;
        private DigitalInput buttonTestDrawer1;
        private DigitalInput buttonTestDrawer2;
        private DigitalInput buttonRunSequence;
        private DigitalInput buttonTestSound;
        private Switch switchHand;
        private Switch switchHead;
        private Switch switchDrawer1;
        private Switch switchDrawer2;


        public HalloweenScene2013(IEnumerable<string> args)
        {
            buttonTestHand = new DigitalInput("Hand");
            buttonTestHead = new DigitalInput("Head");
            buttonTestDrawer1 = new DigitalInput("Drawer 1");
            buttonTestDrawer2 = new DigitalInput("Drawer 2");
            buttonRunSequence = new DigitalInput("Run Seq!");
            buttonTestSound = new DigitalInput("Test Sound");
            
            switchHand = new Switch("Hand");
            switchHead = new Switch("Head");
            switchDrawer1 = new Switch("Drawer 1");
            switchDrawer2 = new Switch("Drawer 2");
            
            audioPlayer = new AudioPlayer("Audio Player");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_FlipFlop(buttonTestHand);
            sim.AddDigitalInput_Momentarily(buttonTestHead);
            sim.AddDigitalInput_FlipFlop(buttonTestDrawer1);
            sim.AddDigitalInput_FlipFlop(buttonTestDrawer2);
            
            sim.AddDigitalInput_Momentarily(buttonRunSequence);
            sim.AddDigitalInput_Momentarily(buttonTestSound);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.Raspberry port)
        {
            port.DigitalInputs[0].Connect(buttonTestHand);
            port.DigitalInputs[1].Connect(buttonTestHead);
            port.DigitalInputs[2].Connect(buttonTestDrawer1);
            port.DigitalInputs[3].Connect(buttonTestDrawer2);
            port.DigitalInputs[7].Connect(buttonRunSequence);
            port.DigitalOutputs[7].Connect(switchHand);
            port.DigitalOutputs[4].Connect(switchHead);
            port.DigitalOutputs[5].Connect(switchDrawer1);
            port.DigitalOutputs[6].Connect(switchDrawer2);

            port.Connect(audioPlayer);
        }

        public override void Start()
        {
            var popSeq = new Controller.Sequence("Pop Sequence");
            popSeq.WhenExecuted
                .Execute(instance =>
                    {
                        instance.WaitFor(TimeSpan.FromSeconds(5));

                        audioPlayer.PlayEffect("laugh");
                        instance.WaitFor(TimeSpan.FromSeconds(0.4));
                        switchHead.SetPower(true);
                        instance.WaitFor(TimeSpan.FromSeconds(0.6));
                        switchHand.SetPower(true);
                        instance.WaitFor(TimeSpan.FromSeconds(2));
                        switchHead.SetPower(false);

                        for (int i = 0; i < 5; i++)
                        {
                            switchDrawer1.SetPower(true);
                            instance.WaitFor(TimeSpan.FromSeconds(0.5));
                            switchDrawer1.SetPower(false);
                            switchDrawer2.SetPower(true);
                            instance.WaitFor(TimeSpan.FromSeconds(0.5));
                            switchDrawer2.SetPower(false);
                        }

                        switchHand.SetPower(false);
                        instance.WaitFor(TimeSpan.FromSeconds(1));
                    });

            buttonRunSequence.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    Executor.Current.Execute(popSeq);
                }
            };

            buttonTestSound.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                    {
                        audioPlayer.PlayEffect("laugh", 1.0, 0.0);
                        System.Threading.Thread.Sleep(3000);
                        audioPlayer.PlayEffect("laugh", 0.0, 1.0);
                    }
                };

            buttonTestHand.ActiveChanged += (sender, e) =>
                {
                    switchHand.SetPower(e.NewState);
                };

            buttonTestHead.ActiveChanged += (sender, e) =>
            {
                switchHead.SetPower(e.NewState);
            };

            buttonTestDrawer1.ActiveChanged += (sender, e) =>
            {
                switchDrawer1.SetPower(e.NewState);
            };

            buttonTestDrawer2.ActiveChanged += (sender, e) =>
            {
                switchDrawer2.SetPower(e.NewState);
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
