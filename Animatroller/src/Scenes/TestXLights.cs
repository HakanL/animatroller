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

        const int midiChannel = 0;

        public enum States
        {
            Background,
            Setup
        }

        Color[] treeColors = new Color[]
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.White
        };

        OperatingHours2 mainSchedule = new OperatingHours2();
        OperatingHours2 interactiveSchedule = new OperatingHours2();
        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();

        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderLedmx = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderHiFi = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderPoppy = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderDarth = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderControlPanel = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderInflatableTree = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8899);
        AudioPlayer audioR2D2Olaf = new AudioPlayer();
        AudioPlayer audioHiFi = new AudioPlayer();
        AudioPlayer audioPoppy = new AudioPlayer();
        AudioPlayer audioDarthVader = new AudioPlayer();
        AudioPlayer audioInflatableTree = new AudioPlayer();

        AnalogInput3 faderR = new AnalogInput3(persistState: true);
        AnalogInput3 faderG = new AnalogInput3(persistState: true);
        AnalogInput3 faderB = new AnalogInput3(persistState: true);
        AnalogInput3 faderBright = new AnalogInput3(persistState: true);

        Expander.AcnStream acnOutput = new Expander.AcnStream(priority: 150);
        Effect.Pulsating pulsatingEffectGeneral = new Effect.Pulsating(S(3), 0.5, 1.0, false);
        Effect.Pulsating pulsatingEffectTree = new Effect.Pulsating(S(3), 0.0, 1.0, false);
        Effect.Pulsating pulsatingPinSpot = new Effect.Pulsating(S(2), 0.2, 1.0, false);
        Effect.Pulsating pulsatingStar = new Effect.Pulsating(S(2), 0.2, 1.0, false);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 airActivated = new DigitalInput2(persistState: true);

        DigitalInput2 inInflatableTree = new DigitalInput2();
        DigitalInput2 inOlaf = new DigitalInput2();
        DigitalInput2 inR2D2 = new DigitalInput2();
        DigitalInput2 inPoppy = new DigitalInput2();
        DigitalInput2 controlButtonWhite = new DigitalInput2();
        DigitalInput2 controlButtonYellow = new DigitalInput2();
        DigitalInput2 controlButtonBlue = new DigitalInput2();
        DigitalInput2 controlButtonGreen = new DigitalInput2();
        DigitalInput2 controlButtonBlack = new DigitalInput2();
        DigitalInput2 controlButtonRed = new DigitalInput2(holdTimeout: S(10));

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 inShowMachine = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 inSetupMode = new DigitalInput2(persistState: true);

        DigitalOutput2 airR2D2Olaf = new DigitalOutput2();
        DigitalOutput2 airSantaPoppy1 = new DigitalOutput2();
        DigitalOutput2 airSnowmanSanta = new DigitalOutput2();
        DigitalOutput2 airTree = new DigitalOutput2();
        DigitalOutput2 airSantaPopup = new DigitalOutput2();
        DigitalOutput2 airReindeerBig = new DigitalOutput2();
        DigitalOutput2 snowMachine = new DigitalOutput2();
        Dimmer3 hazerFanSpeed = new Dimmer3();
        Dimmer3 hazerHazeOutput = new Dimmer3();
        Dimmer3 lightInflatableTree = new Dimmer3();

        Dimmer3 lightHangingStar = new Dimmer3();
        Dimmer3 lightHangingStar2 = new Dimmer3();
        Dimmer3 lightHangingStar3 = new Dimmer3();
        DigitalOutput2 lightXmasTree = new DigitalOutput2();
        DigitalOutput2 lightPackages = new DigitalOutput2();
        Dimmer3 lightStairRail1 = new Dimmer3();
        Dimmer3 lightStairRail2 = new Dimmer3();
        //Dimmer3 lightRail1 = new Dimmer3();
        //Dimmer3 lightRail2 = new Dimmer3();
        //Dimmer3 lightRail3 = new Dimmer3();
        //Dimmer3 lightRail4 = new Dimmer3();
        //Dimmer3 lightStairs1 = new Dimmer3();
        //Dimmer3 lightStairs2 = new Dimmer3();
        //Dimmer3 lightStairs3 = new Dimmer3();
        //Dimmer3 lightTreeStars = new Dimmer3();
        //StrobeColorDimmer3 lightPinSpot = new StrobeColorDimmer3();

        Dimmer3 lightSanta = new Dimmer3();
        Dimmer3 lightPoppy = new Dimmer3();
        Dimmer3 lightSnowman = new Dimmer3();
        Dimmer3 lightSantaPopup = new Dimmer3();
        //MovingHead movingHead = new MovingHead();

        Dimmer3 lightHat1 = new Dimmer3();
        Dimmer3 lightHat2 = new Dimmer3();
        Dimmer3 lightHat3 = new Dimmer3();
        Dimmer3 lightHat4 = new Dimmer3();
        //Dimmer3 lightReindeers = new Dimmer3();
        Dimmer3 lightReindeerBig = new Dimmer3();
        StrobeColorDimmer3 lightVader = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood1 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood2 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood3 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood4 = new StrobeColorDimmer3();
        //StrobeColorDimmer3 lightFlood5 = new StrobeColorDimmer3();
        //StrobeColorDimmer3 lightFlood6 = new StrobeColorDimmer3();
        //StrobeColorDimmer3 lightFlood7 = new StrobeColorDimmer3();

        Dimmer3 lightOlaf = new Dimmer3();
        Dimmer3 lightR2D2 = new Dimmer3();
        VirtualPixel1D3 pixelsRoofEdge = new VirtualPixel1D3(150);
        //VirtualPixel1D3 pixelsTree = new VirtualPixel1D3(50);
        //VirtualPixel1D3 pixelsBetweenTrees = new VirtualPixel1D3(50);
        //VirtualPixel1D3 pixelsHeart = new VirtualPixel1D3(50);
        //VirtualPixel1D3 pixelsGround = new VirtualPixel1D3(50);
        VirtualPixel2D3 pixelsMatrix = new VirtualPixel2D3(48, 24);
        VirtualPixel1D3 saberPixels = new VirtualPixel1D3(33);
        VirtualPixel1D3 haloPixels = new VirtualPixel1D3(27);
        Expander.MidiInput2 midiAkai = new Expander.MidiInput2("LPD8", true);
        Expander.OscServer oscServer = new Expander.OscServer(8000);
        Subject<bool> inflatablesRunning = new Subject<bool>();
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();
        DigitalInput2 buttonStartInflatables = new DigitalInput2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonTest = new DigitalInput2();
        DigitalInput2 buttonReset = new DigitalInput2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        Controller.Subroutine subBackground = new Controller.Subroutine();

        Import.DmxPlayback2 dmxPlayback = new Import.DmxPlayback2();

        public TestXLights(IEnumerable<string> args)
        {
            mainSchedule.AddRange("4:00 pm", "10:00 pm");
            interactiveSchedule.AddRange("4:00 pm", "8:00 pm");

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

            pulsatingStar.ConnectTo(lightHangingStar);
            pulsatingStar.ConnectTo(lightHangingStar2);
            pulsatingStar.ConnectTo(lightHangingStar3);
            pulsatingEffectGeneral.ConnectTo(lightHangingStar);
            pulsatingEffectGeneral.ConnectTo(lightHangingStar2);
            pulsatingEffectGeneral.ConnectTo(lightHangingStar3);
            pulsatingEffectGeneral.ConnectTo(lightOlaf);
            pulsatingEffectGeneral.ConnectTo(lightR2D2);
            pulsatingEffectGeneral.ConnectTo(lightPoppy);
            pulsatingEffectGeneral.ConnectTo(lightInflatableTree);
            pulsatingEffectGeneral.ConnectTo(lightVader, Utils.Data(Color.Red));

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expanderLedmx);          // rpi-eb0092ca
            expanderServer.AddInstance("d6fc4e752af04022bf3c1a1166a557bb", expanderHiFi);           // rpi-eb428ef1
            expanderServer.AddInstance("acbfada45c674077b9154f6a0e0df359", expanderPoppy);         // rpi-eba6cbc7
            expanderServer.AddInstance("999861affa294fd7bbf0601505e9ae09", expanderDarth);           // rpi-ebd43a38
            expanderServer.AddInstance("e41d2977931d4887a9417e8adcd87306", expanderControlPanel);   // rpi-eb6a047c
            expanderServer.AddInstance("1583f686014345888c15d7fc9c55ca3c", expanderInflatableTree);    // 

            expanderInflatableTree.DigitalInputs[4].Connect(inInflatableTree);
            expanderLedmx.DigitalInputs[6].Connect(inOlaf);
            expanderLedmx.DigitalInputs[5].Connect(inPoppy);
            expanderLedmx.DigitalInputs[4].Connect(inR2D2);
            expanderLedmx.DigitalOutputs[1].Connect(snowMachine);

            expanderControlPanel.DigitalInputs[0].Connect(controlButtonWhite, true);
            expanderControlPanel.DigitalInputs[1].Connect(controlButtonYellow, true);
            expanderControlPanel.DigitalInputs[2].Connect(controlButtonBlue, true);
            expanderControlPanel.DigitalInputs[3].Connect(controlButtonGreen, true);
            expanderControlPanel.DigitalInputs[4].Connect(controlButtonBlack, true);
            expanderControlPanel.DigitalInputs[5].Connect(controlButtonRed, true);

            expanderLedmx.Connect(audioPoppy);
            expanderHiFi.Connect(audioHiFi);
            expanderDarth.Connect(audioDarthVader);
            expanderPoppy.Connect(audioR2D2Olaf);
            expanderInflatableTree.Connect(audioInflatableTree);

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

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    mainSchedule.SetForced(true);
                else
                    mainSchedule.SetForced(null);
            });

            //inflatablesRunning.Subscribe(x =>
            //{
            //    airReindeerBig.SetValue(x);
            //});

            stateMachine.ForFromSubroutine(States.Background, subBackground);

            // Setup
            //airReindeerBig.SetValue(true, 1);
            //airR2D2Olaf.SetValue(true, 1);
            //airSantaPoppy1.SetValue(true, 1);
            //airSantaPopup.SetValue(true, 1);
            //airSnowmanSanta.SetValue(true, 1);
            //airTree.SetValue(true, 1);
            lightPoppy.SetBrightness(1, 1);
            lightR2D2.SetBrightness(1, 1);
            lightOlaf.SetBrightness(1, 1);
            lightReindeerBig.SetBrightness(1, 1);
            lightHangingStar.SetBrightness(1, 1);
            lightHangingStar2.SetBrightness(1, 1);
            lightHangingStar3.SetBrightness(1, 1);
            lightInflatableTree.SetBrightness(1, 1);
            lightHat1.SetBrightness(1, 1);
            //lightHat2.SetBrightness(1, 1);
            //lightHat3.SetBrightness(1, 1);
            //lightHat4.SetBrightness(1, 1);
            lightPackages.SetValue(true, 1);
            lightSanta.SetBrightness(1, 1);
            lightSantaPopup.SetBrightness(1, 1);

            stateMachine.For(States.Setup)
                .Execute(ins =>
                {
                    //Exec.SetGlobalChannel(1);

                    //dmxPlayback.Load(fileReaderXmas);
                    dmxPlayback.Load(fileReaderCarol);
                    dmxPlayback.Run(true);
                    ins.WaitUntilCancel();
                }).
                TearDown(ins =>
                {
                    dmxPlayback.Stop();
                    Exec.SetGlobalChannel(0);
                });

            stateMachine.StateOutput.Subscribe(x =>
            {
                CheckAirState();
            });

            airActivated.WhenOutputChanges(x => CheckAirState());

            buttonStartInflatables.Output.Subscribe(x =>
            {
                if (x && mainSchedule.IsOpen)
                {
                    inflatablesRunning.OnNext(true);
                    Exec.SetKey("InflatablesRunning", "True");
                }
            });

            buttonTest.WhenOutputChanges(x =>
            {
                if (x)
                {
                    //    //pixelsMatrix.Inject(Color.FromArgb(random.Next(255), random.Next(255), 128));
                    //FIXME dmxPlayback.Load(fileReaderStarWars);
                    dmxPlayback.Run(false);
                }
                //else
                //{
                //    dmxPlayback.Load(fileReaderXmas);
                //    dmxPlayback.Run(true);
                //}
            });

            buttonReset.WhenOutputChanges(x =>
            {
                if (x)
                    stateMachine.GoToDefaultState();
            });

            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 0, 50, reverse: true), SacnUniverseLED50, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 50, 100), SacnUniverseLED100, 1);

            //acnOutput.Connect(new Physical.Pixel1D(pixelsTree, 0, 50), SacnUniverseLEDTree50, 1);
            //acnOutput.Connect(new Physical.Pixel1D(pixelsBetweenTrees, 0, 50), SacnUniversePixelString1, 1);
            //acnOutput.Connect(new Physical.Pixel1D(pixelsTree, 0, 50), SacnUniversePixelString2, 1);
            //acnOutput.Connect(new Physical.Pixel1D(pixelsGround, 0, 50), SacnUniversePixelGround, 1);
            //acnOutput.Connect(new Physical.Pixel1D(pixelsHeart, 0, 50), SacnUniversePixelString4, 1);

            acnOutput.Connect(new Physical.Pixel2D(pixelsMatrix, pixelMapping2D), SacnUniversePixelMatrixStart);

            acnOutput.Connect(new Physical.Pixel1D(saberPixels), SacnUniversePixelSaber, 1);
            acnOutput.Connect(new Physical.Pixel1D(haloPixels), SacnUniversePixelSaber, 100);

            acnOutput.Connect(new Physical.GenericDimmer(airReindeerBig, 10), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(airR2D2Olaf, 12), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 64), SacnUniverseEdmx4a);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 65), SacnUniverseEdmx4a);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 1), SacnUniverseRenard19);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 2), SacnUniverseRenard19);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairRail1, 10), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairRail2, 4), SacnUniverseRenard19);
            //acnOutput.Connect(new Physical.GenericDimmer(lightRail1, 20), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightRail2, 28), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightRail3, 29), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightRail4, 66), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood4, 310), SacnUniverseEdmx4b);
            //acnOutput.Connect(new Physical.RGBStrobe(lightFlood1, 60), SacnUniverseEdmx4);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood1, 300), SacnUniverseEdmx4b);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood2, 330), SacnUniverseEdmx4b);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood3, 340), SacnUniverseEdmx4b);
            //acnOutput.Connect(new Physical.RGBStrobe(lightFlood4, 40), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.RGBStrobe(lightFlood4, 40), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood5, 340), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.RGBStrobe(lightFlood6, 80), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood7, 300), SacnUniverseLedmx);

            //acnOutput.Connect(new Physical.GenericDimmer(laser, 1), SacnUniverseRenard18);

            acnOutput.Connect(new Physical.GenericDimmer(lightOlaf, 129), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(airSnowmanSanta, 13), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(airTree, 66), SacnUniverseEdmx4a);
            acnOutput.Connect(new Physical.GenericDimmer(lightInflatableTree, 67), SacnUniverseEdmx4a);
            //acnOutput.Connect(new Physical.GenericDimmer(lightSnowman, 131), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightSanta, 131), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightPoppy, 128), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightSantaPopup, 256), SacnUniverseEdmx4a);
            //acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(movingHead, 200), SacnUniverseEdmx4);

            acnOutput.Connect(new Physical.GenericDimmer(lightReindeerBig, 1), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightPackages, 2), SacnUniverseRenard18);

            //acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 64), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 25), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightStairs3, 2), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(lightPinSpot, 20), SacnUniverseEdmx4);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTreeStars, 39), SacnUniverseRenard18);

            acnOutput.Connect(new Physical.GenericDimmer(lightR2D2, 50), SacnUniverseLedmx);
            ////acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 11), SacnUniverseRenardBig);
            ////acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 19), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(airSantaPopup, 3), SacnUniverseRenard19);
            acnOutput.Connect(new Physical.GenericDimmer(airSantaPoppy1, 11), SacnUniverseLedmx);
            ////acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 22), SacnUniverseRenardBig);

            //acnOutput.Connect(new Physical.GenericDimmer(hazerFanSpeed, 500), SacnUniverseEdmx4);
            //acnOutput.Connect(new Physical.GenericDimmer(hazerHazeOutput, 501), SacnUniverseEdmx4);
            ////            acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 25), SacnUniverseRenardBig);
            //acnOutput.Connect(new Physical.GenericDimmer(lightXmasTree, 67), SacnUniverseEdmx4a);
            //acnOutput.Connect(new Physical.GenericDimmer(lightReindeers, 40), SacnUniverseRenard18);

            ////            acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 1), SacnUniverseRenardSmall);
            ////acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 2), SacnUniverseRenardSmall);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet1, 5), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 6), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet3, 7), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTopper1, 3), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTopper2, 4), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.RGBStrobe(lightVader, 40), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar, 51), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar2, 9), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar3, 52), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet4, 26), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 27), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 34), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 35), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet8, 36), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet9, 50), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet10, 51), SacnUniverseLedmx);

            //acnOutput.Connect(new Physical.GenericDimmer(lightNet3, 4), SacnUniverseRenardSmall);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet4, 5), SacnUniverseRenardSmall);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTopper1, 7), SacnUniverseRenardSmall);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTopper2, 8), SacnUniverseRenardSmall);

            faderR.WhenOutputChanges(v => { SetManualColor(); });
            faderG.WhenOutputChanges(v => { SetManualColor(); });
            faderB.WhenOutputChanges(v => { SetManualColor(); });
            faderBright.WhenOutputChanges(v => { SetManualColor(); });

            mainSchedule.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetDefaultState(States.Background);
                    stateMachine.GoToDefaultState();
                }
                else
                {
                    //if (buttonOverrideHours.Active)
                    //    return;

                    stateMachine.SetDefaultState(null);
                    stateMachine.GoToIdle();

                    // Needed?
                    System.Threading.Thread.Sleep(200);

                    if (Initialized)
                    {
                        inflatablesRunning.OnNext(false);
                        Exec.SetKey("InflatablesRunning", "False");
                    }
                }
            });

            audioHiFi.AudioTrackStart += (o, e) =>
            {
                switch (e.FileName)
                {
                    case "21. Christmas Canon Rock.wav":
                        //FIXME                        lorChristmasCanon.Start();
                        break;
                }
            };

            inSetupMode.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetDefaultState(States.Setup);
                }
                else
                {
                    if (mainSchedule.IsOpen)
                        stateMachine.SetDefaultState(States.Background);
                    else
                        stateMachine.SetDefaultState(null);
                }

                stateMachine.GoToDefaultState();
            });
        }

        private void CheckAirState()
        {
            bool airOn = (stateMachine.CurrentState != null) && stateMachine.CurrentState != States.Setup &&
                mainSchedule.IsOpen && airActivated.Value;

            airReindeerBig.SetValue(airOn, 0);
            airR2D2Olaf.SetValue(airOn, 0);
            airSantaPoppy1.SetValue(airOn, 0);
            airSantaPopup.SetValue(airOn, 0);
            airSnowmanSanta.SetValue(airOn, 0);
            airTree.SetValue(airOn, 0);
            lightPackages.SetValue(airOn, 0);
        }

        private Color GetFaderColor()
        {
            return HSV.ColorFromRGB(faderR.Value, faderG.Value, faderB.Value);
        }

        private void SetManualColor()
        {
            //if (manualFaderToken != null)
            //{
            //movingHead.SetColor(GetFaderColor(), faderBright.Value, 0, token: null);
            //}
        }

        private bool OkToRunInteractive(bool input)
        {
            if (!input)
                return false;

            if (stateMachine.CurrentState == States.Setup)
                return true;
            else
            {
                if (stateMachine.IsInState(States.Background) && interactiveSchedule.IsOpen)
                    return true;
            }

            return false;
        }

        public override void Run()
        {
            // Read from storage
            inflatablesRunning.OnNext(Exec.GetSetKey("InflatablesRunning", false));
        }

        public override void Stop()
        {
            audioHiFi.PauseBackground();
        }
    }
}
