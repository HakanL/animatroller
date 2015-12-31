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
using Import = Animatroller.Framework.Import;
using System.IO;

namespace Animatroller.SceneRunner
{
    internal class PixelTest1 : BaseScene
    {
        const int SacnUniverse5 = 5;
        const int SacnUniverse6 = 6;
        const int SacnUniverse10 = 10;
        const int SacnUniverse11 = 11;

        Expander.AcnStream acnOutput = new Expander.AcnStream();
        VirtualPixel1D3 pixelRope = new VirtualPixel1D3(150);
        VirtualPixel2D3 pixelsMatrix = new VirtualPixel2D3(20, 10);
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
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonCandyCane = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonPlayback = new DigitalInput2();
        Import.DmxPlayback dmxPlayback = new Import.DmxPlayback();

        Controller.Subroutine subStarWarsCane = new Controller.Subroutine();
        Controller.Subroutine subCandyCane = new Controller.Subroutine();

        //Expander.OpcClient opcOutput = new Expander.OpcClient("192.168.1.113");

        public PixelTest1(IEnumerable<string> args)
        {
            string expanderFilesFolder = string.Empty;
            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                    expanderFilesFolder = parts[1];
            }

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            dmxPlayback.Load(Path.Combine(expanderFilesFolder, "Seq", "XmasLoop.bin"), 15);
            dmxPlayback.Loop = true;

            //            var pixelMapping = dmxPlayback.GeneratePixelMapping(pixelsMatrix, 8, channelShift: 1);
            var pixelMapping = Framework.Utility.PixelMapping.GeneratePixelMappingFromGlediatorPatch(
                Path.Combine(expanderFilesFolder, "Glediator", "ArtNet 14-15 20x10.patch.glediator"));
            dmxPlayback.SetOutput(pixelsMatrix, pixelMapping);
            //dmxPlayback.SetOutput(pixelRope, pixelMapping);

            acnOutput.Connect(new Physical.Pixel1D(pixelRope, 0, 50), SacnUniverse6, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelRope, 50, 100), SacnUniverse5, 1);

            pixelMapping = Framework.Utility.PixelMapping.GeneratePixelMapping(20, 10, pixelOrder: Framework.Utility.PixelOrder.HorizontalSnakeBottomLeft);
            acnOutput.Connect(new Physical.Pixel2D(pixelsMatrix, pixelMapping), SacnUniverse10, 1);

            subStarWarsCane
                .LockWhenRunning(pixelRope)
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
                                    pixelRope.InjectRev(Color.Yellow, 1.0, instance.Token);
                                    break;
                                case 2:
                                case 3:
                                    pixelRope.InjectRev(Color.Orange, 0.2, instance.Token);
                                    break;
                            }

                            instance.WaitFor(S(0.1));

                            if (instance.IsCancellationRequested)
                                break;
                        }
                    }
                });

            subCandyCane
                .LockWhenRunning(pixelRope)
                .RunAction(i =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int x = 0; x < spacing; x++)
                        {
                            pixelRope.Inject((x % spacing) == 0 ? Color.Red : Color.White, 0.5, i.Token);

                            i.WaitFor(S(0.30), true);
                        }
                    }
                });

            buttonTest.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        Color rndCol = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
                        pixelRope.Inject(rndCol, 1.0);

                        pixelsMatrix.Inject(rndCol);
                    }
                });

            buttonCane.Output.Subscribe(x =>
            {
                if (x)
                    subStarWarsCane.Run();
                else
                    subStarWarsCane.Stop();
            });

            buttonCandyCane.Output.Subscribe(x =>
            {
                if (x)
                    subCandyCane.Run();
                else
                    subCandyCane.Stop();
            });

            buttonPlayback.Output.Subscribe(x =>
            {
                if (x)
                    dmxPlayback.Run();
                else
                    dmxPlayback.Stop();
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
                {
                    pixelRope.SetColor(Color.Black, 0.0);
                    pixelsMatrix.SetColor(Color.Black, 0.0);
                }
            });

            pixelRope.SetBrightness(1);
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
