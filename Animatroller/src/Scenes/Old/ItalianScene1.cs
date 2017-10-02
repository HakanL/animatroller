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

namespace Animatroller.Scenes
{
    internal class ItalianScene1 : BaseScene, ISceneRequiresAcnStream
    {
        private VirtualPixel1D allPixels;

        //        private Controller.Sequence testSeq;
        private Controller.Sequence candyCane;
        //        private Controller.Sequence laserSeq;

        public ItalianScene1(IEnumerable<string> args)
        {
            //            testSeq = new Controller.Sequence("Pulse");
            candyCane = new Controller.Sequence("Candy Cane");
            //            laserSeq = new Controller.Sequence("Laser");

            allPixels = new VirtualPixel1D(28);
        }

        public void WireUp(Expander.IOExpander port)
        {
            //            port.Connect(new Physical.PixelRope(allPixels, 0, 50));
            //            port.Connect(new Physical.PixelRope(allPixels, 50, 50));

            //            port.DigitalInputs[1].Connect(buttonTest);
        }

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
            port.Connect(new Physical.PixelRope(allPixels, 0, 28), 4, 1);
            //// WS2811
            //port.Connect(new Physical.PixelRope(allPixels, 28, 50), 1, 151);
        }

        public override void Start()
        {
            candyCane
                .WhenExecuted
                .SetUp(() => allPixels.TurnOff())
                .Execute(instance =>
                {
                    var cbList = new List<ColorBrightness>();
                    //cbList.Add(new ColorBrightness(Color.Green, 1.00));
                    //cbList.Add(new ColorBrightness(Color.Green, 0.70));
                    //cbList.Add(new ColorBrightness(Color.Green, 0.40));
                    //cbList.Add(new ColorBrightness(Color.White, 1.00));
                    //cbList.Add(new ColorBrightness(Color.White, 0.70));
                    //cbList.Add(new ColorBrightness(Color.White, 0.40));
                    //cbList.Add(new ColorBrightness(Color.Red, 1.00));
                    //cbList.Add(new ColorBrightness(Color.Red, 0.70));
                    //cbList.Add(new ColorBrightness(Color.Red, 0.40));
                    //cbList.Add(new ColorBrightness(Color.Black, 0.0));
                    //cbList.Add(new ColorBrightness(Color.Black, 0.0));
                    //cbList.Add(new ColorBrightness(Color.Black, 0.0));
                    //cbList.Add(new ColorBrightness(Color.Black, 0.0));

                    double b1 = 1.00;
                    double b2 = 0.70;
                    double b3 = 0.40;
                    Color c1 = Color.Blue;
                    Color c2 = Color.Yellow;
                    Color c3 = Color.Blue;
                    Color c4 = Color.Black;

                    cbList.Add(new ColorBrightness(c1, b1));
                    cbList.Add(new ColorBrightness(c1, b2));
                    cbList.Add(new ColorBrightness(c1, b3));
                    cbList.Add(new ColorBrightness(c2, b1));
                    cbList.Add(new ColorBrightness(c2, b2));
                    cbList.Add(new ColorBrightness(c2, b3));
                    cbList.Add(new ColorBrightness(c3, b1));
                    cbList.Add(new ColorBrightness(c3, b2));
                    cbList.Add(new ColorBrightness(c3, b3));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));

/*                    b1 = 1.00;
                    b2 = 0.70;
                    b3 = 0.40;
                    c1 = Color.White;
                    c2 = Color.Blue;
                    c3 = Color.Red;
                    c4 = Color.Black;

                    cbList.Add(new ColorBrightness(c1, b1));
                    cbList.Add(new ColorBrightness(c1, b2));
                    cbList.Add(new ColorBrightness(c1, b3));
                    cbList.Add(new ColorBrightness(c2, b1));
                    cbList.Add(new ColorBrightness(c2, b2));
                    cbList.Add(new ColorBrightness(c2, b3));
                    cbList.Add(new ColorBrightness(c3, b1));
                    cbList.Add(new ColorBrightness(c3, b2));
                    cbList.Add(new ColorBrightness(c3, b3));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));

                    b1 = 1.00;
                    b2 = 0.70;
                    b3 = 0.40;
                    c1 = Color.Red;
                    c2 = Color.White;
                    c3 = Color.Blue;
                    c4 = Color.Black;

                    cbList.Add(new ColorBrightness(c1, b1));
                    cbList.Add(new ColorBrightness(c1, b2));
                    cbList.Add(new ColorBrightness(c1, b3));
                    cbList.Add(new ColorBrightness(c2, b1));
                    cbList.Add(new ColorBrightness(c2, b2));
                    cbList.Add(new ColorBrightness(c2, b3));
                    cbList.Add(new ColorBrightness(c3, b1));
                    cbList.Add(new ColorBrightness(c3, b2));
                    cbList.Add(new ColorBrightness(c3, b3));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));
                    cbList.Add(new ColorBrightness(c4, 0.0));
*/
                    while (true)
                    {
                        foreach(var cb in cbList)
                        {
                            allPixels.Inject(cb);
                            instance.WaitFor(S(0.350), true);
                        }
                    }
                })
                .TearDown(() =>
                    {
                        allPixels.TurnOff();
                    });
        }

        public override void Run()
        {
            Exec.Execute(candyCane);
        }

        public override void Stop()
        {
            Exec.Cancel(candyCane);
            System.Threading.Thread.Sleep(200);
        }
    }
}
