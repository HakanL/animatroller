using System;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Expander = Animatroller.Framework.Expander;
using Import = Animatroller.Framework.Import;
using Physical = Animatroller.Framework.PhysicalDevice;
using Effect2 = Animatroller.Framework.Effect2;

namespace Animatroller.SceneRunner
{
    internal class Nutcracker3Scene : BaseScene
    {
        private VirtualPixel1D allPixels;
        private DigitalInput testButton;
        private Import.BaseImporter.Timeline lorTimeline;
        private StrobeColorDimmer candyLight;

        public Nutcracker3Scene()
        {
            candyLight = new StrobeColorDimmer("Candy Light");
            testButton = new DigitalInput("Test");

            allPixels = new VirtualPixel1D("All Pixels", 60);
            allPixels.SetAll(Color.White, 0);

            var lorImport = new Import.LorImport(@"..\..\..\Test Files\HAUK~HALLOWEEN1.lms");

            int pixelPosition = 0;

            var circuits = lorImport.GetChannels.GetEnumerator();

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

                var pixel = lorImport.MapDevice(
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

            lorTimeline = lorImport.CreateTimeline(null);
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
            port.Connect(new Physical.SmallRGBStrobe(candyLight, 20));
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
                    candyLight.RunEffect(new Effect2.Pulse(0.0, 1.0), S(0.5));
                    System.Threading.Thread.Sleep(S(1));
                    candyLight.StopEffect();
                    candyLight.TurnOff();
                }
            };

            lorTimeline.Start();
        }

        public override void Run()
        {
//            allPixels.SetColor(0, Color.Blue);
//            allPixels.SetColor(3, Color.Purple);


//            candyLight.SetColor(Color.Purple, 0);
        }

        public override void Stop()
        {
        }
    }
}
