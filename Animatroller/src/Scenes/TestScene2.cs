using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
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
    internal class TestScene2 : BaseScene
    {
        private StrobeColorDimmer candyLight;
        private StrobeColorDimmer candyLight2;
        private DigitalInput pressureMat;
        private Effect.Pulsating pulsatingEffect;


        public TestScene2(IEnumerable<string> args)
        {
            candyLight = new StrobeColorDimmer("Candy Light");
            candyLight2 = new StrobeColorDimmer("Candy Light 2");
            pressureMat = new DigitalInput("Pressure Mat");
            pulsatingEffect = new Effect.Pulsating("Pulse FX", S(2), 0, 1.0, false);
        }

        public void WireUp(Expander.IOExpander port)
        {
            port.Connect(new Physical.SmallRGBStrobe(candyLight, 16));
            port.DigitalInputs[0].Connect(pressureMat);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.SmallRGBStrobe(candyLight, 16));
        }

        public void WireUp(Expander.AcnStream port)
        {
        }

        public override void Start()
        {
            // Set color
            candyLight.SetColor(Color.Violet, 0);
            candyLight2.SetColor(Color.Green, 0);

            var testSequence = new Controller.Sequence("Test Sequence");
            testSequence
                .WhenExecuted
                .Execute(instance =>
                {
                    pulsatingEffect.Start();

                    instance.WaitFor(S(10));

                    pulsatingEffect.Stop();

                    candyLight.SetStrobe(1.0, Color.Yellow);

                    instance.WaitFor(S(2));
                    candyLight.TurnOff();
                    candyLight.SetColor(Color.Violet, 0);
                });

            pressureMat.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    log.Info("Button press!");

//                    candyLight2.RunEffect(new Effect2.Fader(1.0, 0.0), S(0.5));
//                    Executor.Current.Execute(testSequence);
                }
            };


            pulsatingEffect.AddDevice(candyLight)
                .AddDevice(candyLight2);

//            candyLight.RunEffect(new Effect2.Pulse(0.0, 1.0), S(2));
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
