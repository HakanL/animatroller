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
        private AudioPlayer audioPlayer;
        private DigitalInput buttonPlayFX;
        private DigitalInput buttonPauseFX;
        private DigitalInput buttonCueFX;
        private DigitalInput buttonResumeFX;
        private Expander.OscServer oscServer;


        public TestScene3()
        {
            buttonPlayFX = new DigitalInput("Play FX");
            buttonPauseFX = new DigitalInput("Pause FX");
            buttonCueFX = new DigitalInput("Cue FX");
            buttonResumeFX = new DigitalInput("Resume FX");
            audioPlayer = new AudioPlayer("Audio Player");

            this.oscServer = new Expander.OscServer(9999);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonPlayFX);
            sim.AddDigitalInput_Momentarily(buttonPauseFX);
            sim.AddDigitalInput_Momentarily(buttonCueFX);
            sim.AddDigitalInput_Momentarily(buttonResumeFX);

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

        public void WireUp(Expander.Raspberry port)
        {
            port.DigitalInputs[0].Connect(buttonPlayFX);

            port.Connect(audioPlayer);
        }

        public override void Start()
        {
            this.oscServer.RegisterAction<int>("/OnOff", x =>
                {
                    if (x.Any())
                    {
                        if (x.First() != 0)
                            audioPlayer.PlayEffect("Scream");
                    }
                });

            buttonPlayFX.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                    audioPlayer.PlayEffect("Scream");
            };

            buttonPauseFX.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                        audioPlayer.PauseFX();
                };

            buttonCueFX.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                    audioPlayer.CueFX("myFile");
            };

            buttonResumeFX.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                    audioPlayer.ResumeFX();
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
