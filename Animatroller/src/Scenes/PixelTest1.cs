using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    internal class PixelTest1 : BaseScene
    {
        const int SacnUniversePix1 = 30;
        const int SacnUniversePix2 = 31;

        Expander.AcnStream acnOutput = new Expander.AcnStream();
        VirtualPixel1D3 pixelsMatrix = new VirtualPixel1D3(200);
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();

        //Expander.Tpm2NetSink tpmSink = new Expander.Tpm2NetSink();
        //PixelMapper2D tpmPixelMapper = new PixelMapper2D(20, 10, PixelMapper2D.PixelOrders.HorizontalLineTopLeft);
        //PixelMapper2D opcPixelMapper = new PixelMapper2D(20, 10, PixelMapper2D.PixelOrders.HorizontalSnakeTopLeft);

        //VirtualPixel2D allPixels = new VirtualPixel2D(20, 10);
        DigitalInput2 buttonTest = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonFader = new DigitalInput2();
        DigitalInput2 buttonFader2 = new DigitalInput2();
        DigitalInput2 buttonClear = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonCane = new DigitalInput2();

        Controller.Subroutine subStarWarsCane = new Controller.Subroutine();

        //Expander.OpcClient opcOutput = new Expander.OpcClient("192.168.1.113");

        public PixelTest1(IEnumerable<string> args)
        {
            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            var file = System.IO.File.OpenRead(@"C:\Projects\Animatroller\Utils\DMXrecorder\bin\Debug\Glediator.bin");
            var binRead = new System.IO.BinaryReader(file);

            acnOutput.Connect(new Physical.PixelRope(pixelsMatrix, 0, 170), SacnUniversePix1, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsMatrix, 170, 30), SacnUniversePix2, 1);

            subStarWarsCane
                .LockWhenRunning(pixelsMatrix)
                .RunAction(instance =>
                {
                    const int spacing = 4;

                    while (!instance.CancelToken.IsCancellationRequested)
                    {
                        for (int i = 0; i < spacing; i++)
                        {
                            switch (i % spacing)
                            {
                                case 0:
                                case 1:
                                    pixelsMatrix.InjectRev(Color.Yellow, 1.0, instance.Token);
                                    break;
                                case 2:
                                case 3:
                                    pixelsMatrix.InjectRev(Color.Orange, 0.2, instance.Token);
                                    break;
                            }

                            instance.WaitFor(S(0.1));

                            if (instance.IsCancellationRequested)
                                break;
                        }
                    }
                });

            buttonTest.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        Color rndCol = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
                        pixelsMatrix.Inject(rndCol, 1.0);
                    }
                });

            buttonCane.Output.Subscribe(x =>
            {
                if (x)
                    subStarWarsCane.Run();
                else
                    subStarWarsCane.Stop();
            });

            buttonFader.Output.Subscribe(x =>
            {
                if (x)
                    Exec.MasterEffect.Fade(pixelsMatrix, 1.0, 0.0, 2000, token: Exec.MasterToken);
                else
                    Exec.MasterEffect.Fade(pixelsMatrix, 0.0, 1.0, 2000, token: Exec.MasterToken);
            });

            buttonFader2.Output.Subscribe(x =>
            {
                if (x)
                {
                    Exec.MasterEffect.Fade(pixelsMatrix, 0.0, 1.0, 2000, additionalData: Utils.AdditionalData(Color.White));
                }
            });

            buttonClear.Output.Subscribe(x =>
            {
                if (x)
                    pixelsMatrix.SetColor(Color.Black, 0.0);
            });

            pixelsMatrix.SetBrightness(1);

            var bitmap = (Bitmap)pixelsMatrix.GetFrameBuffer(null, pixelsMatrix)[DataElements.PixelBitmap];

            Task.Run(() =>
            {
                while (false && file.Position < file.Length)
                {
                    byte bStart = binRead.ReadByte();
                    if (bStart != 1)
                        throw new ArgumentException("Invalid data");

                    uint timestamp = (uint)binRead.ReadInt32();
                    ushort universe = (ushort)binRead.ReadInt16();
                    ushort len = (ushort)binRead.ReadInt16();
                    byte[] data = binRead.ReadBytes(len);
                    byte bEnd = binRead.ReadByte();
                    if (bEnd != 4)
                        throw new ArgumentException("Invalid data");

                    int pixCount = 0;
                    int pix = 0;
                    switch (universe)
                    {
                        case 8:
                            pixCount = 170;
                            pix = 0;
                            break;

                        case 9:
                            pixCount = 30;
                            pix = 170;
                            break;
                    }
                    if (pixCount == 0)
                        continue;

                    int i = 0;
                    int pixPos = 0;
                    while (pixPos++ < pixCount)
                    {
                        var col = Color.FromArgb(data[i++], data[i++], data[i++]);
                        bitmap.SetPixel(pix, 0, col);

                        pix++;
                    }

                    if (universe == 9)
                        pixelsMatrix.PushOutput(null);

                    Thread.Sleep(25);
                }
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
