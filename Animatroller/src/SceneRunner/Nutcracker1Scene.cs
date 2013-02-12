using System;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Import = Animatroller.Framework.Import;
using Controller = Animatroller.Framework.Controller;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class Nutcracker1Scene : BaseScene
    {
        protected VirtualPixel1D allPixels;

        protected DigitalInput testButton;
        protected Import.LorTimeline lorTimeline;


        public Nutcracker1Scene()
        {
            testButton = new DigitalInput("Test");

            allPixels = new VirtualPixel1D("All Pixels", 80);
            allPixels.SetAll(Color.White, 0);

            var lorImport = new Animatroller.Framework.Import.LorImport(@"..\..\..\Test Files\HAUK~HALLOWEEN1.lms");

            int pixelPosition = 0;

            foreach(int unit in lorImport.AvailableUnits)
            {
                var circuits = lorImport.GetCircuits(unit).GetEnumerator();

                while (true)
                {
                    int circuitR, circuitG, circuitB;

                    if (!circuits.MoveNext())
                        break;
                    circuitR = circuits.Current;

                    if (!circuits.MoveNext())
                        break;
                    circuitG = circuits.Current;

                    if (!circuits.MoveNext())
                        break;
                    circuitB = circuits.Current;

                    var pixel = lorImport.MapDevice(unit, circuitR, circuitG, circuitB, name => new SinglePixel(name, allPixels, pixelPosition));

                    log.Debug("Mapping unit {0}  circuits R{1}/G{2}/B{3} to pixel {4} [{5}]", unit, circuitR, circuitG, circuitB, pixelPosition, pixel.Name);

                    pixelPosition++;
                }
            }

            lorTimeline = lorImport.CreateTimeline(true);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(testButton);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.IOExpander port)
        {
            port.DigitalInputs[0].Connect(testButton);
        }

        public void WireUp(Expander.DMXPro port)
        {
        }

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
            port.Connect(new Physical.PixelRope(allPixels, 0, 60), 3, 181);
        }

        public override void Start()
        {
            // Set color
            testButton.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    log.Info("Button press!");
                }
            };

            lorTimeline.Start();
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
            System.Threading.Thread.Sleep(200);
        }
    }
}
