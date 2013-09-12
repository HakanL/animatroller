using System;
using System.Drawing;
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
    internal class TestScene3 : BaseScene
    {
        private Physical.NetworkAudioPlayer audioPlayer;
        private DigitalInput pressureMat;
        private Expander.OscServer oscServer;


        public TestScene3()
        {
            pressureMat = new DigitalInput("Pressure Mat");

            audioPlayer = new Physical.NetworkAudioPlayer(
                Properties.Settings.Default.NetworkAudioPlayerIP,
                Properties.Settings.Default.NetworkAudioPlayerPort);

            this.oscServer = new Expander.OscServer(3333);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(pressureMat);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.IOExpander port)
        {
        }

        public void WireUp(Expander.DMXPro port)
        {
        }

        public void WireUp(Expander.AcnStream port)
        {
        }

        public void WireUp(Expander.Raspberry osc)
        {
            osc.DigitalInputs[0].Connect(pressureMat);
        }

        public override void Start()
        {
            this.oscServer.RegisterAction<int>("/OnOff", x =>
                {
                    if (x.Any())
                    {
                        if (x.First() != 0)
                            audioPlayer.PlayEffect("Laugh");
                    }
                });

            pressureMat.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    log.Info("Button press!");

                    audioPlayer.PlayEffect("Laugh");
                    //                    candyLight2.RunEffect(new Effect2.Fader(1.0, 0.0), S(0.5));
                    //                    Executor.Current.Execute(testSequence);
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
