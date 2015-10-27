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
    internal class DemoScene1 : BaseScene, ISceneRequiresDMXPro
    {
        private AudioPlayer audioPlayer;

        [SimulatorButtonType(SimulatorButtonTypes.Momentarily)]
        private DigitalInput buttonTestSound;
        
        [SimulatorButtonType(SimulatorButtonTypes.Momentarily)]
        private DigitalInput buttonPlayBackground;
        
        [SimulatorButtonType(SimulatorButtonTypes.Momentarily)]
        private DigitalInput buttonPauseBackground;
        
        [SimulatorButtonType(SimulatorButtonTypes.Momentarily)]
        private DigitalInput buttonTrigger1;
        
        [SimulatorButtonType(SimulatorButtonTypes.Momentarily)]
        private DigitalInput buttonTestLight1;
        
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput buttonTestLight2;
        
        private Switch switchTest1;
        private StrobeColorDimmer light1;
        private Effect.Pulsating pulsatingEffect1;
        private Expander.Raspberry raspberry = new Expander.Raspberry();

        public DemoScene1(IEnumerable<string> args)
        {
            pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false, "Pulse FX 1");
            light1 = new StrobeColorDimmer("Small RGB");
            buttonTestSound = new DigitalInput("Test sound");
            buttonPlayBackground = new DigitalInput("Play Background");
            buttonPauseBackground = new DigitalInput("Pause Background");
            buttonTrigger1 = new DigitalInput("Test seq");
            buttonTestLight1 = new DigitalInput("Test light");
            buttonTestLight2 = new DigitalInput("Test pulse");
            switchTest1 = new Switch("Switch test 1");

            audioPlayer = new AudioPlayer("Audio Player");

            raspberry.DigitalInputs[4].Connect(buttonTrigger1, true);
            raspberry.DigitalOutputs[7].Connect(switchTest1);

            raspberry.Connect(audioPlayer);


            var demoSeq = new Controller.Sequence("Demo Sequence");
            demoSeq.WhenExecuted
                .Execute(instance =>
                {
                    audioPlayer.PlayEffect("laugh");
                    switchTest1.SetPower(true);
                    light1.SetColor(Color.Orange, 1.0);
                    instance.WaitFor(TimeSpan.FromSeconds(1));
                    switchTest1.SetPower(false);
                    light1.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
                });

            buttonTestSound.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    audioPlayer.PlayEffect("sixthsense-deadpeople");
                }
            };

            buttonPlayBackground.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    audioPlayer.PlayBackground();
                }
            };

            buttonPauseBackground.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    audioPlayer.PauseBackground();
                }
            };

            buttonTestLight1.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    light1.SetOnlyColor(Color.White);
                    light1.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));
                    Thread.Sleep(S(1));
                    light1.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
                }
            };

            buttonTestLight2.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    pulsatingEffect1.Start();
                }
                else
                {
                    pulsatingEffect1.Stop();
                }
            };

            buttonTrigger1.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    Executor.Current.Execute(demoSeq);
                }
            };

            pulsatingEffect1.AddDevice(light1);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.SmallRGBStrobe(light1, 50));
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
