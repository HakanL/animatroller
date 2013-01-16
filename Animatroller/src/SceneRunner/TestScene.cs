using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.Expander;

namespace Animatroller.SceneRunner
{
    internal class TestScene : BaseScene
    {
        protected StrobeDimmer georgeStrobeLight = new StrobeDimmer("George Strobe");
        protected StrobeColorDimmer spiderLight = new StrobeColorDimmer("Spider Light");
        protected Dimmer skullsLight = new Dimmer("Skulls");
        protected Dimmer cobWebLight = new Dimmer("Cob Web");
        protected Switch blinkyEyesLight = new Switch("Blinky Eyes");
        protected StrobeColorDimmer rgbLightRight = new StrobeColorDimmer("Light Right");
        protected StrobeColorDimmer rgbLight3 = new StrobeColorDimmer("Light 3");
        protected StrobeColorDimmer rgbLight4 = new StrobeColorDimmer("Light 4");
        protected MotorWithFeedback georgeMotor = new MotorWithFeedback("George Motor");
        protected Switch spiderLift = new Switch("Spider Lift");
        protected Animatroller.Framework.LogicalDevice.DigitalInput pressureMat = new Animatroller.Framework.LogicalDevice.DigitalInput("Pressure Mat");

        protected Animatroller.Framework.Effect.Pulsating pulsatingEffect;
        protected Animatroller.Framework.Effect.Flicker flickerEffect;

        public TestScene()
        {
            pulsatingEffect = new Animatroller.Framework.Effect.Pulsating("Pulse FX", TimeSpan.FromSeconds(1), 0.2, 0.7);
            flickerEffect = new Animatroller.Framework.Effect.Flicker("Flicker", 0.4, 0.6);

            Executor.Current.Register(this);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(pressureMat);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(IOExpander port)
        {
            port.Connect(new Animatroller.Framework.PhysicalDevice.AmericanDJStrobe(georgeStrobeLight, 5));

            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(spiderLight, 10));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(skullsLight, 1));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(cobWebLight, 3));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(blinkyEyesLight, 4));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(rgbLightRight, 20));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(rgbLight3, 30));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(rgbLight4, 40));

            port.Motor.Connect(georgeMotor);

            port.DigitalInputs[0].Connect(pressureMat);
            port.DigitalOutputs[0].Connect(spiderLift);
        }

        public void WireUp(DMXPro port)
        {
            port.Connect(new Animatroller.Framework.PhysicalDevice.AmericanDJStrobe(georgeStrobeLight, 16));

            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(spiderLight, 10));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(skullsLight, 1));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(cobWebLight, 3));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(blinkyEyesLight, 4));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(rgbLightRight, 20));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(rgbLight3, 30));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(rgbLight4, 40));
        }

        public override void Start()
        {
            pressureMat.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    Console.WriteLine("Button press!");

                    pulsatingEffect.Stop();

                    spiderLift.SetPower(true);
                    georgeMotor.SetVector(1, 160, TimeSpan.FromSeconds(5));
                    georgeMotor.WaitForVectorReached();
                    Console.WriteLine("Motor done");
                    georgeMotor.SetVector(0.8, 0, TimeSpan.FromSeconds(5));
                    georgeMotor.WaitForVectorReached();
                    Console.WriteLine("Motor back");

                    pulsatingEffect.Start();
                    spiderLift.SetPower(false);
                }
            };

            spiderLight.SetColor(Color.Blue, 1);

            pulsatingEffect.AddDevice(spiderLight);

            flickerEffect.AddDevice(georgeStrobeLight);
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
