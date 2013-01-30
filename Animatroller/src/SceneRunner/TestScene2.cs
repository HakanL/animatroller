using System;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class TestScene2 : BaseScene
    {
        protected StrobeColorDimmer candyLight;
        protected StrobeColorDimmer candyLight2;
        protected DigitalInput pressureMat;
        protected Effect.Pulsating pulsatingEffect;


        public TestScene2()
        {
            candyLight = new StrobeColorDimmer("Candy Light");
            candyLight2 = new StrobeColorDimmer("Candy Light 2");
            pressureMat = new DigitalInput("Pressure Mat");
            pulsatingEffect = new Effect.Pulsating("Pulse FX", S(2), 0, 1.0, false);

            var lorImport = new Animatroller.Framework.Utility.LorImport();

            lorImport.MapDevice(1, 5, candyLight);

            lorImport.ImportLMSFile(@"C:\Projects\Animatroller\wonderful christmas time.lms");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(pressureMat);

            sim.AutoWireUsingReflection(this);
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

            var testSequence = new Sequence("Test Sequence");
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

                    Executor.Current.Execute(testSequence);
                }
            };


            pulsatingEffect.AddDevice(candyLight)
                .AddDevice(candyLight2);
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
