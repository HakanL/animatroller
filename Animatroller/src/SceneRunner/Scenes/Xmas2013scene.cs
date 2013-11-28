using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class Xmas2013scene : BaseScene, ISceneRequiresAcnStream, ISceneSupportsSimulator
    {
        private VirtualPixel1D allPixels;
        private DigitalInput buttonTest;
        private StrobeColorDimmer lightTest;
        private Dimmer lightStar;
        private Dimmer lightStairs1;

        private Effect.Pulsating pulsatingEffect1;
        private Controller.Sequence testSeq;
        private Controller.Sequence candyCane;
        private Controller.Sequence laserSeq;

        public Xmas2013scene(IEnumerable<string> args)
        {
            lightStar = new Dimmer("Star");
            lightStairs1 = new Dimmer("Stair 1");
            lightTest = new StrobeColorDimmer("Small");

            pulsatingEffect1 = new Effect.Pulsating("Pulse FX 1", S(2), 0.0, 1.0, false);

            testSeq = new Controller.Sequence("Pulse");
            candyCane = new Controller.Sequence("Candy Cane");
            laserSeq = new Controller.Sequence("Laser");

            allPixels = new VirtualPixel1D("All Pixels", 150);

            buttonTest = new DigitalInput("Test");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonTest);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
//            port.Connect(new Physical.PixelRope(allPixels, 0, 100), 4, 1);
            // GECE
//            port.Connect(new Physical.PixelRope(allPixels, 100, 50), 2, 91);

            port.Connect(new Physical.SmallRGBStrobe(lightTest, 48), 20);
            port.Connect(new Physical.GenericDimmer(lightStar, 1), 21);
            port.Connect(new Physical.GenericDimmer(lightStar, 2), 21);
            port.Connect(new Physical.GenericDimmer(lightStar, 3), 21);
            port.Connect(new Physical.GenericDimmer(lightStar, 4), 21);
            port.Connect(new Physical.GenericDimmer(lightStar, 5), 21);
            port.Connect(new Physical.GenericDimmer(lightStar, 6), 21);
            port.Connect(new Physical.GenericDimmer(lightStar, 7), 21);
            port.Connect(new Physical.GenericDimmer(lightStar, 24), 21);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.GenericDimmer(lightStar, 104));
            port.Connect(new Physical.GenericDimmer(lightStairs1, 107));
        }

        private void TestAllPixels(Color color, double brightness, TimeSpan delay)
        {
            allPixels.SetAll(color, brightness);
            System.Threading.Thread.Sleep(delay);
        }

        public override void Start()
        {
            testSeq
                .WhenExecuted
                .SetUp(() => allPixels.TurnOff())
                .Execute(instance =>
                {
                    allPixels.SetAllOnlyColor(Color.Orange);
                    allPixels.RunEffect(new Effect2.Pulse(0.0, 1.0), S(2.0))
                        .SetIterations(2)
                        .Wait();
                    allPixels.StopEffect();
                })
                .TearDown(() => 
                    {
                        allPixels.TurnOff();

                        Exec.Execute(candyCane);
                    });


            candyCane
                .WhenExecuted
                .SetUp(() => allPixels.TurnOff())
                .Execute(instance =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int i = 0; i < spacing; i++)
                        {
                            allPixels.Inject((i % spacing) == 0 ? Color.Red : Color.White, 0.5);

                            instance.WaitFor(S(0.2), true);
                        }
                    }
                })
                .TearDown(() =>
                    {
                        allPixels.TurnOff();
                    });


            laserSeq
                .WhenExecuted
                .SetUp(() =>
                    {
                        allPixels.TurnOff();
                    })
                .Execute(instance =>
                {
                    var cb = new ColorBrightness[6];
                    cb[0] = new ColorBrightness(Color.Black, 1.0);
                    cb[1] = new ColorBrightness(Color.Red, 1.0);
                    cb[2] = new ColorBrightness(Color.Orange, 1.0);
                    cb[3] = new ColorBrightness(Color.Yellow, 1.0);
                    cb[4] = new ColorBrightness(Color.Blue, 1.0);
                    cb[5] = new ColorBrightness(Color.White, 1.0);

                    for (int i = -6; i < allPixels.Pixels; i++)
                    {
                        allPixels.SetColors(i, cb);
                        System.Threading.Thread.Sleep(25);
                    }

                    instance.WaitFor(S(1));
                })
                .TearDown(() =>
                    {
                        allPixels.TurnOff();

                        Exec.Execute(candyCane);
                    });


            // Test Button
            buttonTest.ActiveChanged += (sender, e) =>
            {
                if (!e.NewState)
                    return;

                Exec.Cancel(candyCane);

                allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();
                allPixels.SetAllOnlyColor(Color.Purple);
                allPixels.RunEffect(new Effect2.Fader(0.0, 1.0), S(2.0)).Wait();
                allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();

                allPixels.SetAllOnlyColor(Color.Orange);
                allPixels.RunEffect(new Effect2.Fader(0.0, 1.0), S(2.0)).Wait();

                allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();

                Exec.Execute(candyCane);
            };

            lightTest.SetOnlyColor(Color.Orange);
            pulsatingEffect1.AddDevice(lightTest);

            pulsatingEffect1.AddDevice(lightStar);
        }

        public override void Run()
        {
//            lightStar.SetBrightness(1.0);
            lightStairs1.SetBrightness(1.0);
            Exec.Execute(testSeq);
            pulsatingEffect1.Start();
        }

        public override void Stop()
        {
            pulsatingEffect1.Stop();
            lightStar.TurnOff();
            lightStairs1.TurnOff();
            Exec.Cancel(candyCane);
            System.Threading.Thread.Sleep(200);
        }
    }
}
