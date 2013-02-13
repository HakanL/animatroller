using System;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Expander = Animatroller.Framework.Expander;
using Import = Animatroller.Framework.Import;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class Nutcracker2Scene : BaseScene
    {
        private VirtualPixel1D allPixels;
        private DigitalInput testButton;
        private Import.BaseImporter.Timeline vixTimeline;

        public Nutcracker2Scene()
        {
            testButton = new DigitalInput("Test");

            allPixels = new VirtualPixel1D("All Pixels", 80);
            allPixels.SetAll(Color.White, 0);

            var vixImport = new Import.VixenImport(@"..\..\..\Test Files\HAUK~HALLOWEEN1.vix");

            int pixelPosition = 0;

            var circuits = vixImport.GetChannels.GetEnumerator();

            while (true)
            {
                Controller.IChannelIdentity channelR, channelG, channelB;

                if (!circuits.MoveNext())
                    break;
                channelR = circuits.Current;

                if (!circuits.MoveNext())
                    break;
                channelG = circuits.Current;

                if (!circuits.MoveNext())
                    break;
                channelB = circuits.Current;

                var pixel = vixImport.MapDevice(
                    channelR,
                    channelG,
                    channelB,
                    name => new SinglePixel(name, allPixels, pixelPosition));

                log.Debug("Mapping channel R[{0}]/G[{1}]/B[{2}] to pixel {3} [{4}]",
                    channelR,
                    channelG,
                    channelB,
                    pixelPosition,
                    pixel.Name);

                pixelPosition++;
            }

            vixTimeline = vixImport.CreateTimeline(true);
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

            vixTimeline.Start();
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
            // FIXME: Get rid of this sleep
            System.Threading.Thread.Sleep(200);
        }
    }
}
