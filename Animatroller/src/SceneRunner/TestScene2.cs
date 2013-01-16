﻿using System;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.SceneRunner
{
    internal class TestScene2 : BaseScene
    {
        protected StrobeColorDimmer candyLight = new StrobeColorDimmer("Candy Light");
        protected StrobeColorDimmer candyLight2 = new StrobeColorDimmer("Candy Light 2");
        protected DigitalInput pressureMat = new DigitalInput("Pressure Mat");

        protected Animatroller.Framework.Effect.Pulsating pulsatingEffect;

        public TestScene2()
        {
            pulsatingEffect = new Animatroller.Framework.Effect.Pulsating("Pulse FX", TimeSpan.FromSeconds(2), 0, 1.0, false);

            Executor.Current.Register(this);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(pressureMat);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(IOExpander port)
        {
            port.Connect(new Animatroller.Framework.PhysicalDevice.SmallRGBStrobe(candyLight, 16));
            port.DigitalInputs[0].Connect(pressureMat);
        }

        public void WireUp(DMXPro port)
        {
            port.Connect(new Animatroller.Framework.PhysicalDevice.SmallRGBStrobe(candyLight, 16));
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

                    instance.WaitFor(TimeSpan.FromSeconds(3));

                    pulsatingEffect.Stop();

                    candyLight.SetStrobe(1.0, Color.Yellow);
                    
                    instance.WaitFor(TimeSpan.FromSeconds(2));
                    candyLight.TurnOff();
                    candyLight.SetColor(Color.Violet, 0);
                });

            pressureMat.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    Console.WriteLine("Button press!");

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
