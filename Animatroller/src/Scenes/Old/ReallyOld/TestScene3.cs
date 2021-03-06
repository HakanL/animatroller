﻿using System;
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

namespace Animatroller.Scenes
{
    internal class TestScene3 : BaseScene
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
        private Expander.Raspberry raspberry = new Expander.Raspberry();

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

            oscServer = new Expander.OscServer(9999);

            raspberry.DigitalInputs[7].Connect(buttonTrigger1);
            raspberry.DigitalOutputs[7].Connect(switchTest1);

            raspberry.Connect(audioPlayer);
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
