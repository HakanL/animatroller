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
        PixelMapper2D tpmPixelMapper = new PixelMapper2D(20, 10, PixelMapper2D.PixelOrders.HorizontalLineTopLeft);
        PixelMapper2D opcPixelMapper = new PixelMapper2D(20, 10, PixelMapper2D.PixelOrders.HorizontalSnakeTopLeft);

        VirtualPixel2D allPixels = new VirtualPixel2D(20, 10);
        DigitalInput2 buttonTest = new DigitalInput2();
        Expander.OpcClient opcOutput = new Expander.OpcClient("192.168.1.113");

        public TpmTest(IEnumerable<string> args)
        {
            tpmSink.DataReceived.Subscribe(x =>
                {
                    switch (x.PacketNumber)
                    {
                        case 1:
                            tpmPixelMapper.FromRGBByteArray(x.Data, 0, allPixels.SetPixel);
                            break;

                        case 2:
                            tpmPixelMapper.FromRGBByteArray(x.Data, 160, allPixels.SetPixel);
                            break;
                    }

                    if (x.PacketNumber == x.TotalPackets)
                        allPixels.ShowBuffer();
                });

            opcOutput.Connect(allPixels, opcPixelMapper, 1);


            // WS2811
            //            acnOutput.Connect(new Physical.PixelRope(allPixels, 0, 200), 1, 1);

            buttonTest.Output.Subscribe(x =>
                {
                    if (x)
                        Exec.MasterEffect.Fade(allPixels.GlobalBrightnessControl, 1, 0, 2000);
                    else
                        Exec.MasterEffect.Fade(allPixels.GlobalBrightnessControl, 0, 1, 2000);
                });
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
