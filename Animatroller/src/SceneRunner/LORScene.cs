using System;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Utility = Animatroller.Framework.Utility;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class LORScene : BaseScene
    {
        protected ColorDimmer light1_1;
        protected ColorDimmer light1_2;
        protected ColorDimmer light1_3;
        protected ColorDimmer light1_4;
        protected ColorDimmer light1_5;
        protected ColorDimmer light1_6;
        protected ColorDimmer light1_7;
        protected ColorDimmer light1_8;
        protected ColorDimmer light1_9;
        protected ColorDimmer light1_10;
        protected ColorDimmer light1_11;
        protected ColorDimmer light1_12;
        protected ColorDimmer light1_13;
        protected ColorDimmer light1_14;
        protected ColorDimmer light1_15;
        protected ColorDimmer light1_16;

        protected ColorDimmer light2_1;
        protected ColorDimmer light2_2;
        protected ColorDimmer light2_3;
        protected ColorDimmer light2_4;
        protected ColorDimmer light2_5;
        protected ColorDimmer light2_6;
        protected ColorDimmer light2_7;
        protected ColorDimmer light2_8;
        protected ColorDimmer light2_9;
        protected ColorDimmer light2_10;
        protected ColorDimmer light2_11;
        protected ColorDimmer light2_12;
        protected ColorDimmer light2_13;
        protected ColorDimmer light2_14;
        protected ColorDimmer light2_15;
        protected ColorDimmer light2_16;

        protected ColorDimmer light3_1;
        protected ColorDimmer light3_2;
        protected ColorDimmer light3_3;
        protected ColorDimmer light3_4;
        protected ColorDimmer light3_5;
        protected ColorDimmer light3_6;
        protected ColorDimmer light3_7;
        protected ColorDimmer light3_8;
        protected ColorDimmer light3_9;
        protected ColorDimmer light3_10;
        protected ColorDimmer light3_11;
        protected ColorDimmer light3_12;
        protected ColorDimmer light3_13;
        protected ColorDimmer light3_14;
        protected ColorDimmer light3_15;
        protected ColorDimmer light3_16;

        protected ColorDimmer light4_1;
        protected ColorDimmer light4_2;
        protected ColorDimmer light4_3;
        protected ColorDimmer light4_4;
        protected ColorDimmer light4_5;
        protected ColorDimmer light4_6;
        protected ColorDimmer light4_7;
        protected ColorDimmer light4_8;
        protected ColorDimmer light4_9;
        protected ColorDimmer light4_10;
        protected ColorDimmer light4_11;
        protected ColorDimmer light4_12;
        protected ColorDimmer light4_13;
        protected ColorDimmer light4_14;
        protected ColorDimmer light4_15;
        protected ColorDimmer light4_16;

        protected ColorDimmer light5_1;
        protected ColorDimmer light5_2;
        protected ColorDimmer light5_3;
        protected ColorDimmer light5_4;
        protected ColorDimmer light5_5;
        protected ColorDimmer light5_6;
        protected ColorDimmer light5_7;
        protected ColorDimmer light5_8;
        protected ColorDimmer light5_9;
        protected ColorDimmer light5_10;
        protected ColorDimmer light5_11;
        protected ColorDimmer light5_12;
        protected ColorDimmer light5_13;
        protected ColorDimmer light5_14;
        protected ColorDimmer light5_15;
        protected ColorDimmer light5_16;

        protected VirtualPixel1D allPixels;

        protected DigitalInput testButton;
        protected Utility.LorTimeline lorTimeline;


        public LORScene()
        {
            testButton = new DigitalInput("Test");

            allPixels = new VirtualPixel1D("All Pixels", 80);
            allPixels.SetAll(Color.White, 0);

            var lorImport = new Animatroller.Framework.Utility.LorImport(@"..\..\..\Test Files\wonderful christmas time.lms");
/*
            light1_1 = lorImport.MapDevice(1, 1, name => new StrobeColorDimmer(name));
            light1_2 = lorImport.MapDevice(1, 2, name => new StrobeColorDimmer(name));
            light1_3 = lorImport.MapDevice(1, 3, name => new StrobeColorDimmer(name));
            light1_4 = lorImport.MapDevice(1, 4, name => new StrobeColorDimmer(name));
            light1_5 = lorImport.MapDevice(1, 5, name => new StrobeColorDimmer(name));
            light1_6 = lorImport.MapDevice(1, 6, name => new StrobeColorDimmer(name));
            light1_7 = lorImport.MapDevice(1, 7, name => new StrobeColorDimmer(name));
            light1_8 = lorImport.MapDevice(1, 8, name => new StrobeColorDimmer(name));
            light1_9 = lorImport.MapDevice(1, 9, name => new StrobeColorDimmer(name));
            light1_10 = lorImport.MapDevice(1, 10, name => new StrobeColorDimmer(name));
            light1_11 = lorImport.MapDevice(1, 11, name => new StrobeColorDimmer(name));
            light1_12 = lorImport.MapDevice(1, 12, name => new StrobeColorDimmer(name));
            light1_13 = lorImport.MapDevice(1, 13, name => new StrobeColorDimmer(name));
            light1_14 = lorImport.MapDevice(1, 14, name => new StrobeColorDimmer(name));
            light1_15 = lorImport.MapDevice(1, 15, name => new StrobeColorDimmer(name));
            light1_16 = lorImport.MapDevice(1, 16, name => new StrobeColorDimmer(name));

            light2_1 = lorImport.MapDevice(2, 1, name => new StrobeColorDimmer(name));
            light2_2 = lorImport.MapDevice(2, 2, name => new StrobeColorDimmer(name));
            light2_3 = lorImport.MapDevice(2, 3, name => new StrobeColorDimmer(name));
            light2_4 = lorImport.MapDevice(2, 4, name => new StrobeColorDimmer(name));
            light2_5 = lorImport.MapDevice(2, 5, name => new StrobeColorDimmer(name));
            light2_6 = lorImport.MapDevice(2, 6, name => new StrobeColorDimmer(name));
            light2_7 = lorImport.MapDevice(2, 7, name => new StrobeColorDimmer(name));
            light2_8 = lorImport.MapDevice(2, 8, name => new StrobeColorDimmer(name));
            light2_9 = lorImport.MapDevice(2, 9, name => new StrobeColorDimmer(name));
            light2_10 = lorImport.MapDevice(2, 10, name => new StrobeColorDimmer(name));
            light2_11 = lorImport.MapDevice(2, 11, name => new StrobeColorDimmer(name));
            light2_12 = lorImport.MapDevice(2, 12, name => new StrobeColorDimmer(name));
            light2_13 = lorImport.MapDevice(2, 13, name => new StrobeColorDimmer(name));
            light2_14 = lorImport.MapDevice(2, 14, name => new StrobeColorDimmer(name));
            light2_15 = lorImport.MapDevice(2, 15, name => new StrobeColorDimmer(name));
            light2_16 = lorImport.MapDevice(2, 16, name => new StrobeColorDimmer(name));

            light3_1 = lorImport.MapDevice(3, 1, name => new StrobeColorDimmer(name));
            light3_2 = lorImport.MapDevice(3, 2, name => new StrobeColorDimmer(name));
            light3_3 = lorImport.MapDevice(3, 3, name => new StrobeColorDimmer(name));
            light3_4 = lorImport.MapDevice(3, 4, name => new StrobeColorDimmer(name));
            light3_5 = lorImport.MapDevice(3, 5, name => new StrobeColorDimmer(name));
            light3_6 = lorImport.MapDevice(3, 6, name => new StrobeColorDimmer(name));
            light3_7 = lorImport.MapDevice(3, 7, name => new StrobeColorDimmer(name));
            light3_8 = lorImport.MapDevice(3, 8, name => new StrobeColorDimmer(name));
            light3_9 = lorImport.MapDevice(3, 9, name => new StrobeColorDimmer(name));
            light3_10 = lorImport.MapDevice(3, 10, name => new StrobeColorDimmer(name));
            light3_11 = lorImport.MapDevice(3, 11, name => new StrobeColorDimmer(name));
            light3_12 = lorImport.MapDevice(3, 12, name => new StrobeColorDimmer(name));
            light3_13 = lorImport.MapDevice(3, 13, name => new StrobeColorDimmer(name));
            light3_14 = lorImport.MapDevice(3, 14, name => new StrobeColorDimmer(name));
            light3_15 = lorImport.MapDevice(3, 15, name => new StrobeColorDimmer(name));
            light3_16 = lorImport.MapDevice(3, 16, name => new StrobeColorDimmer(name));

            light4_1 = lorImport.MapDevice(4, 1, name => new StrobeColorDimmer(name));
            light4_2 = lorImport.MapDevice(4, 2, name => new StrobeColorDimmer(name));
            light4_3 = lorImport.MapDevice(4, 3, name => new StrobeColorDimmer(name));
            light4_4 = lorImport.MapDevice(4, 4, name => new StrobeColorDimmer(name));
            light4_5 = lorImport.MapDevice(4, 5, name => new StrobeColorDimmer(name));
            light4_6 = lorImport.MapDevice(4, 6, name => new StrobeColorDimmer(name));
            light4_7 = lorImport.MapDevice(4, 7, name => new StrobeColorDimmer(name));
            light4_8 = lorImport.MapDevice(4, 8, name => new StrobeColorDimmer(name));
            light4_9 = lorImport.MapDevice(4, 9, name => new StrobeColorDimmer(name));
            light4_10 = lorImport.MapDevice(4, 10, name => new StrobeColorDimmer(name));
            light4_11 = lorImport.MapDevice(4, 11, name => new StrobeColorDimmer(name));
            light4_12 = lorImport.MapDevice(4, 12, name => new StrobeColorDimmer(name));
            light4_13 = lorImport.MapDevice(4, 13, name => new StrobeColorDimmer(name));
            light4_14 = lorImport.MapDevice(4, 14, name => new StrobeColorDimmer(name));
            light4_15 = lorImport.MapDevice(4, 15, name => new StrobeColorDimmer(name));
            light4_16 = lorImport.MapDevice(4, 16, name => new StrobeColorDimmer(name));

            light5_1 = lorImport.MapDevice(5, 1, name => new StrobeColorDimmer(name));
            light5_2 = lorImport.MapDevice(5, 2, name => new StrobeColorDimmer(name));
            light5_3 = lorImport.MapDevice(5, 3, name => new StrobeColorDimmer(name));
            light5_4 = lorImport.MapDevice(5, 4, name => new StrobeColorDimmer(name));
            light5_5 = lorImport.MapDevice(5, 5, name => new StrobeColorDimmer(name));
            light5_6 = lorImport.MapDevice(5, 6, name => new StrobeColorDimmer(name));
            light5_7 = lorImport.MapDevice(5, 7, name => new StrobeColorDimmer(name));
            light5_8 = lorImport.MapDevice(5, 8, name => new StrobeColorDimmer(name));
            light5_9 = lorImport.MapDevice(5, 9, name => new StrobeColorDimmer(name));
            light5_10 = lorImport.MapDevice(5, 10, name => new StrobeColorDimmer(name));
            light5_11 = lorImport.MapDevice(5, 11, name => new StrobeColorDimmer(name));
            light5_12 = lorImport.MapDevice(5, 12, name => new StrobeColorDimmer(name));
            light5_13 = lorImport.MapDevice(5, 13, name => new StrobeColorDimmer(name));
            light5_14 = lorImport.MapDevice(5, 14, name => new StrobeColorDimmer(name));
            light5_15 = lorImport.MapDevice(5, 15, name => new StrobeColorDimmer(name));
            light5_16 = lorImport.MapDevice(5, 16, name => new StrobeColorDimmer(name));
*/

            for (int unit = 1; unit <= 5; unit++)
            {
                for (int circuit = 1; circuit <= 16; circuit++)
                {
                    int pixelPos = (unit - 1) * 16 + circuit - 1;

                    var pixel = lorImport.MapDevice(unit, circuit, name => new SinglePixel(name, allPixels, pixelPos));

                    var color = lorImport.GetChannelColor(unit, circuit);
                    allPixels.SetColor(pixelPos, color, 0);

                    log.Debug("Mapping unit {0}  circuit {1} to pixel {2} [{3}]", unit, circuit, pixelPos, pixel.Name);
                }
            }

            lorTimeline = lorImport.CreateTimeline();
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(testButton);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.IOExpander port)
        {
            port.Connect(new Physical.SmallRGBStrobe(light5_1, 16));
            port.DigitalInputs[0].Connect(testButton);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.SmallRGBStrobe(light5_1, 16));
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
        }
    }
}
