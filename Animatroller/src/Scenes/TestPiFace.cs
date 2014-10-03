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
    internal class TestPiFace : BaseScene
    {
        private Expander.OscServer oscServer = new Expander.OscServer();
        private AudioPlayer audioPlayer = new AudioPlayer();
        private DigitalInput buttonPlayFX = new DigitalInput("Play FX");
        private DigitalInput buttonPauseFX = new DigitalInput();
        private DigitalInput buttonCueFX = new DigitalInput();
        private DigitalInput buttonResumeFX = new DigitalInput();
        private DigitalInput buttonPlayBackground = new DigitalInput();
        private DigitalInput buttonPauseBackground = new DigitalInput();
        private DigitalInput buttonBackgroundLowVolume = new DigitalInput();
        private DigitalInput buttonBackgroundHighVolume = new DigitalInput();
        private DigitalInput buttonBackgroundNext = new DigitalInput();
        private DigitalInput buttonTrigger1 = new DigitalInput();
        private DigitalInput buttonTriggerRelay1 = new DigitalInput();
        private DigitalInput buttonTriggerRelay2 = new DigitalInput();
        private Switch switchTest1 = new Switch();
        private Switch switchRelay1 = new Switch();
        private Switch switchRelay2 = new Switch();
        private Expander.Raspberry raspberry = new Expander.Raspberry();

        public TestPiFace(IEnumerable<string> args)
        {
            raspberry.DigitalInputs[7].Connect(buttonTrigger1);
            raspberry.DigitalOutputs[7].Connect(switchTest1);

            raspberry.DigitalOutputs[0].Connect(switchRelay1);
            raspberry.DigitalOutputs[1].Connect(switchRelay2);

            raspberry.Connect(audioPlayer);

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

            this.oscServer.RegisterAction<int>("/OnOff", (msg, data) =>
            {
                if (data.Any())
                {
                    if (data.First() != 0)
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

            buttonTriggerRelay1.ActiveChanged += (sender, e) =>
            {
                switchRelay1.SetPower(e.NewState);
            };

            buttonTriggerRelay2.ActiveChanged += (sender, e) =>
            {
                switchRelay2.SetPower(e.NewState);
            };
        }
    }
}
