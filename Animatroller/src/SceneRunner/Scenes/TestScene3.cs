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
    internal class TestScene3 : BaseScene, ISceneSupportsRaspExpander, ISceneSupportsSimulator
    {
        private Expander.OscServer oscServer;
        private AudioPlayer audioPlayer;
        private DigitalInput buttonPlayFX;
        private DigitalInput buttonPauseFX;
        private DigitalInput buttonCueFX;
        private DigitalInput buttonResumeFX;
        private DigitalInput buttonPlayBackground;
        private DigitalInput buttonPauseBackground;
        private DigitalInput buttonBackgroundLowVolume;
        private DigitalInput buttonBackgroundHighVolume;
        private DigitalInput buttonBackgroundNext;
        private DigitalInput buttonTrigger1;
        private Switch switchTest1;


        public TestScene3(IEnumerable<string> args)
        {
            buttonPlayFX = new DigitalInput("Play FX");
            buttonPauseFX = new DigitalInput("Pause FX");
            buttonCueFX = new DigitalInput("Cue FX");
            buttonResumeFX = new DigitalInput("Resume FX");
            buttonPlayBackground = new DigitalInput("Play Background");
            buttonPauseBackground = new DigitalInput("Pause Background");
            buttonBackgroundLowVolume = new DigitalInput("Background Low");
            buttonBackgroundHighVolume = new DigitalInput("Background High");
            buttonBackgroundNext = new DigitalInput("BG next");
            buttonTrigger1 = new DigitalInput("Pop!");
            switchTest1 = new Switch("Switch test 1");
            
            audioPlayer = new AudioPlayer("Audio Player");

            this.oscServer = new Expander.OscServer(9999);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonPlayFX);
            sim.AddDigitalInput_Momentarily(buttonPauseFX);
            sim.AddDigitalInput_Momentarily(buttonCueFX);
            sim.AddDigitalInput_Momentarily(buttonResumeFX);
            sim.AddDigitalInput_Momentarily(buttonPlayBackground);
            sim.AddDigitalInput_Momentarily(buttonPauseBackground);
            sim.AddDigitalInput_Momentarily(buttonBackgroundLowVolume);
            sim.AddDigitalInput_Momentarily(buttonBackgroundHighVolume);
            sim.AddDigitalInput_Momentarily(buttonBackgroundNext);
            sim.AddDigitalInput_Momentarily(buttonTrigger1);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.Raspberry port)
        {
            port.DigitalInputs[7].Connect(buttonTrigger1);
            port.DigitalOutputs[7].Connect(switchTest1);

            port.Connect(audioPlayer);
        }

        public override void Start()
        {
            var popSeq = new Controller.Sequence("Pop Sequence");
            popSeq.WhenExecuted
                .Execute(instance =>
                    {
//                        audioPlayer.PlayEffect("laugh");
                        instance.WaitFor(TimeSpan.FromSeconds(1));
                        switchTest1.SetPower(true);
                        instance.WaitFor(TimeSpan.FromSeconds(5));
                        switchTest1.SetPower(false);
                        instance.WaitFor(TimeSpan.FromSeconds(1));
                    });

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
                {
                    Executor.Current.Execute(popSeq);
                }
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

            buttonPlayBackground.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    audioPlayer.PlayBackground();

//                    switchTest1.SetPower(true);
                }
            };

            buttonPauseBackground.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    audioPlayer.PauseBackground();

//                    switchTest1.SetPower(false);
                }
            };

            buttonBackgroundLowVolume.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                    audioPlayer.SetBackgroundVolume(0.5);
            };

            buttonBackgroundHighVolume.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                    audioPlayer.SetBackgroundVolume(1.0);
            };

            buttonBackgroundNext.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                    audioPlayer.NextBackgroundTrack();
            };

            buttonTrigger1.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    Executor.Current.Execute(popSeq);
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
