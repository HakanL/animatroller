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
    internal class TpmTest : BaseScene
    {
        Expander.AcnStream acnOutput = new Expander.AcnStream();
        Expander.Tpm2NetSink tpmSink = new Expander.Tpm2NetSink();
        VirtualPixel1D allPixels = new VirtualPixel1D(200);
        DigitalInput buttonTest = new DigitalInput();

        public TpmTest(IEnumerable<string> args)
        {
            tpmSink.DataReceived.Subscribe(x =>
                {
                    switch (x.PacketNumber)
                    {
                        case 1:
                            allPixels.SetRGB(array: x.Data, arrayOffset: 0, arrayLength: 160 * 3, pixelOffset: 0);
                            break;

                        case 2:
                            allPixels.SetRGB(array: x.Data, arrayOffset: 0, arrayLength: 40 * 3, pixelOffset: 160);
                            break;
                    }
                });

            // WS2811
            //            acnOutput.Connect(new Physical.PixelRope(allPixels, 0, 200), 1, 1);
        }

        public override void Start()
        {
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
