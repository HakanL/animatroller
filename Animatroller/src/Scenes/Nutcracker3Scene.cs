using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Expander = Animatroller.Framework.Expander;
using Import = Animatroller.Framework.Import;
using Physical = Animatroller.Framework.PhysicalDevice;
using Effect2 = Animatroller.Framework.Effect2;

namespace Animatroller.SceneRunner
{
    internal class Nutcracker3Scene : BaseScene,  ISceneRequiresAcnStream
    {
        private VirtualPixel1D allPixels1;
        private VirtualPixel1D allPixels2;
        private DigitalInput testButton;
        private Import.BaseImporter.Timeline lorTimeline;
        private StrobeColorDimmer candyLight;

        public Nutcracker3Scene(IEnumerable<string> args)
        {
            candyLight = new StrobeColorDimmer("Candy Light");
            testButton = new DigitalInput("Test");

            allPixels1 = new VirtualPixel1D(256);
            allPixels2 = new VirtualPixel1D(256);
            allPixels1.SetAll(Color.White, 0);
            allPixels2.SetAll(Color.White, 0);

            var lorImport = new Import.LorImport(@"C:\Users\HLindestaf\Downloads\coke_song\Coke-Cola Christmas.lms");

            var channelNames = lorImport.GetChannels.Select(x => lorImport.GetChannelName(x)).ToList();
            channelNames.ForEach(x => Console.WriteLine(x));

            int pixelPosition = 0;

            var circuits = lorImport.GetChannels.GetEnumerator();

            while (true)
            {
                //                Controller.IChannelIdentity channelR, channelG, channelB;
                Controller.IChannelIdentity channel;

                if (!circuits.MoveNext())
                    break;
                channel = circuits.Current;

                VirtualPixel1D pixel1d;
                int pixelNum;
                if (pixelPosition < 256)
                {
                    pixel1d = allPixels1;
                    pixelNum = pixelPosition;
                }
                else
                {
                    pixel1d = allPixels2;
                    pixelNum = pixelPosition - 256;
                }

                var pixel = lorImport.MapDevice(
                    channel,
                    name => new SinglePixel(name, pixel1d, pixelNum));

                log.Debug("Mapping channel [{0}] to pixel {1} [{2}]",
                    channel,
                    pixelPosition,
                    pixel.Name);

                pixelPosition++;
            }

            lorTimeline = lorImport.CreateTimeline(1);
        }

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
            //            port.Connect(new Physical.PixelRope(allPixels, 0, 60), 3, 181);
        }

        public override void Start()
        {
            // Set color
            testButton.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    log.Info("Button press!");
                    //candyLight.RunEffect(new Effect2.Pulse(0.0, 1.0), S(0.5));
                    //System.Threading.Thread.Sleep(S(1));
                    //candyLight.StopEffect();
                    //candyLight.TurnOff();
                    lorTimeline.Start();
                }
            };

        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
