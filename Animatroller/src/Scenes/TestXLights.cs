using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Physical = Animatroller.Framework.PhysicalDevice;
using Effect = Animatroller.Framework.Effect;
using Import = Animatroller.Framework.Import;
using System.IO;
using Animatroller.Framework.Extensions;
using System.Threading.Tasks;

namespace Animatroller.Scenes
{
    internal partial class TestXLights : BaseScene
    {
        const int SacnUniverseEdmx4a = 20;
        const int SacnUniverseEdmx4b = 21;
        const int SacnUniverseEdmx4c = 22;
        const int SacnUniverseEdmx4d = 23;
        const int SacnUniverseLedmx = 10;
        const int SacnUniverseRenard19 = 19;
        const int SacnUniverseRenard18 = 18;        // 2 x 8-channels, 1-16
        const int SacnUniverseLED100 = 5;
        const int SacnUniverseLED50 = 6;
        const int SacnUniversePixelMatrixStart = 40;
        const int SacnUniversePixelSaber = 31;

        public enum States
        {
            Background,
            Setup
        }

        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();

        Expander.AcnStream acnOutput = new Expander.AcnStream(defaultPriority: 150);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 inSetupMode = new DigitalInput2(persistState: true);

        Dimmer3 lightInflatableTree = new Dimmer3();

        Dimmer3 lightHangingStar = new Dimmer3();
        Dimmer3 lightHangingStar2 = new Dimmer3();
        Dimmer3 lightHangingStar3 = new Dimmer3();
        Dimmer3 lightStairRail1 = new Dimmer3();
        Dimmer3 lightStairRail2 = new Dimmer3();

        Dimmer3 lightSanta = new Dimmer3();
        Dimmer3 lightPoppy = new Dimmer3();
        Dimmer3 lightSnowman = new Dimmer3();
        Dimmer3 lightSantaPopup = new Dimmer3();

        Dimmer3 lightHat1 = new Dimmer3();
        Dimmer3 lightHat2 = new Dimmer3();
        Dimmer3 lightHat3 = new Dimmer3();
        Dimmer3 lightHat4 = new Dimmer3();
        Dimmer3 lightReindeerBig = new Dimmer3();
        StrobeColorDimmer3 lightVader = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood1 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood2 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood3 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood4 = new StrobeColorDimmer3();

        Dimmer3 lightOlaf = new Dimmer3();
        Dimmer3 lightR2D2 = new Dimmer3();
        VirtualPixel1D3 pixelsRoofEdge = new VirtualPixel1D3(150);
        VirtualPixel2D3 pixelsMatrix = new VirtualPixel2D3(48, 24);
        VirtualPixel1D3 saberPixels = new VirtualPixel1D3(33);
        VirtualPixel1D3 haloPixels = new VirtualPixel1D3(27);
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();

        Controller.Subroutine subBackground = new Controller.Subroutine();

        Import.DmxPlayback2 dmxPlayback = new Import.DmxPlayback2();

        public TestXLights(IEnumerable<string> args)
        {
            this.log.Debug("Arguments: {@Args}", args);

            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                {
                    Exec.ExpanderSharedFiles = parts[1];
                }
            }

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            //var fileReaderStarWars = new Import.FseqFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Star Wars 1.fseq"), Path.Combine(Exec.ExpanderSharedFiles, "Seq", "xlights_networks.xml"));
            //var fileReaderXmas = new Import.FseqFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "MerryChristmas.fseq"), Path.Combine(Exec.ExpanderSharedFiles, "Seq", "xlights_networks.xml"));
            //var fileReaderHappyNewYear = new Import.FseqFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Happy New Year.fseq"), Path.Combine(Exec.ExpanderSharedFiles, "Seq", "xlights_networks.xml"));

            var fileReaderCarol = new Import.FseqFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Carol of Bells 2017", "Carol of Bells 2017.fseq"));

            var pixelMapping2D = Framework.Utility.PixelMapping.GeneratePixelMapping(
                            48,
                            24,
                            pixelOrder: Framework.Utility.PixelOrder.VerticalSnakeStartAtTopRight,
                            maxPixelsPerPort: 300);
            dmxPlayback.SetOutput(pixelsMatrix, pixelMapping2D, 1, 1);

            var pixelMapping1D50 = Framework.Utility.PixelMapping.GeneratePixelMapping(
                pixels: 50);
            dmxPlayback.SetOutput(pixelsRoofEdge, pixelMapping1D50, 201, 1);

            var pixelMapping1D100 = Framework.Utility.PixelMapping.GeneratePixelMapping(
                pixels: 100,
                startPixel: 50);
            dmxPlayback.SetOutput(pixelsRoofEdge, pixelMapping1D100, 10, 1);

            dmxPlayback.SetOutput(lightFlood1, 631, 1);
            dmxPlayback.SetOutput(lightFlood2, 641, 1);
            dmxPlayback.SetOutput(lightStairRail1, 641, 1);

            stateMachine.ForFromSubroutine(States.Background, subBackground);

            // Setup
            var channel1 = Channel.FromId(1);
            lightPoppy.SetBrightness(1, channel1);
            lightR2D2.SetBrightness(1, channel1);
            lightOlaf.SetBrightness(1, channel1);
            lightReindeerBig.SetBrightness(1, channel1);
            lightHangingStar.SetBrightness(1, channel1);
            lightHangingStar2.SetBrightness(1, channel1);
            lightHangingStar3.SetBrightness(1, channel1);
            lightInflatableTree.SetBrightness(1, channel1);
            lightHat1.SetBrightness(1, channel1);
            lightSanta.SetBrightness(1, channel1);
            lightSantaPopup.SetBrightness(1, channel1);

            stateMachine.For(States.Setup)
                .Execute(ins =>
                {
                    //Exec.SetGlobalChannel(1);

                    dmxPlayback.Load(fileReaderCarol);
                    dmxPlayback.Run(true);
                    ins.WaitUntilCancel();
                }).
                TearDown(ins =>
                {
                    dmxPlayback.Stop();
                    Exec.SetGlobalChannel(Channel.Main);
                });

            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 0, 50, reverse: true), SacnUniverseLED50, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 50, 100), SacnUniverseLED100, 1);

            acnOutput.Connect(new Physical.Pixel2D(pixelsMatrix, pixelMapping2D), SacnUniversePixelMatrixStart);

            acnOutput.Connect(new Physical.Pixel1D(saberPixels), SacnUniversePixelSaber, 1);
            acnOutput.Connect(new Physical.Pixel1D(haloPixels), SacnUniversePixelSaber, 100);

            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 64), SacnUniverseEdmx4a);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 65), SacnUniverseEdmx4a);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 1), SacnUniverseRenard19);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 2), SacnUniverseRenard19);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairRail1, 10), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairRail2, 4), SacnUniverseRenard19);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood4, 310), SacnUniverseEdmx4b);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood1, 300), SacnUniverseEdmx4b);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood2, 330), SacnUniverseEdmx4b);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood3, 340), SacnUniverseEdmx4b);

            acnOutput.Connect(new Physical.GenericDimmer(lightOlaf, 129), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightInflatableTree, 67), SacnUniverseEdmx4a);
            acnOutput.Connect(new Physical.GenericDimmer(lightSanta, 131), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightPoppy, 128), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightSantaPopup, 256), SacnUniverseEdmx4a);

            acnOutput.Connect(new Physical.GenericDimmer(lightReindeerBig, 1), SacnUniverseRenard18);

            acnOutput.Connect(new Physical.GenericDimmer(lightR2D2, 50), SacnUniverseLedmx);

            acnOutput.Connect(new Physical.RGBStrobe(lightVader, 40), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar, 51), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar2, 9), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar3, 52), SacnUniverseLedmx);

            inSetupMode.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetDefaultState(States.Setup);
                }
                else
                {
                    stateMachine.SetDefaultState(States.Background);
                }

                stateMachine.GoToDefaultState();
            });

            subBackground
                .RunAction(i =>
                {
                    i.WaitUntilCancel();
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
