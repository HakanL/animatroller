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
    internal class PixelScene1 : BaseScene
    {
        private Expander.AcnStream acnOutput = new Expander.AcnStream();
        private VirtualPixel1D allPixels;
        private DigitalInput buttonTest;

        private Controller.Sequence testSeq;
        private Controller.Sequence candyCane;
        private Controller.Sequence laserSeq;

        public PixelScene1(IEnumerable<string> args)
        {
            testSeq = new Controller.Sequence("Pulse");
            candyCane = new Controller.Sequence("Candy Cane");
            laserSeq = new Controller.Sequence("Laser");

            allPixels = new VirtualPixel1D(200);

            buttonTest = new DigitalInput("Test");

            // WS2811
            acnOutput.Connect(new Physical.PixelRope(allPixels, 0, 200), 51, 1);
//            acnOutput.Connect(new Physical.PixelRope(allPixels, 50, 100), 5, 1);
//            acnOutput.Connect(new Physical.PixelRope(allPixels, 150, 50), 4, 151);
            // GECE
//            acnOutput.Connect(new Physical.PixelRope(allPixels, 100, 50), 2, 91);
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

                            instance.WaitFor(S(0.30), true);
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
        }

        public override void Run()
        {
            Exec.Execute(testSeq);
        }

        public override void Stop()
        {
            Exec.Cancel(candyCane);
            System.Threading.Thread.Sleep(200);
        }
    }
}
