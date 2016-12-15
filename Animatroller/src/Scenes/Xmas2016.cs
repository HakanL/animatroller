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

namespace Animatroller.Scenes
{
    internal partial class Xmas2016 : BaseScene
    {
        const int SacnUniverseEdmx4 = 4;
        const int SacnUniverseLedmx = 10;
        const int SacnUniverseRenardBig = 20;
        const int SacnUniverseRenardSmall = 21;
        const int SacnUniverseRenard18 = 18;
        const int SacnUniverseLED100 = 5;
        const int SacnUniverseLED50 = 6;
        const int SacnUniverseLEDTree50 = 7;
        const int SacnUniversePixelMatrix = 40;
        const int SacnUniversePixelString1 = 51;
        const int SacnUniversePixelString2 = 52;
        const int SacnUniversePixelGround = 31;
        const int SacnUniversePixelSaber = 32;
        const int SacnUniversePixelString4 = 8;
        const int SacnUniverse11 = 11;
        const int SacnUniverse12 = 12;

        const int midiChannel = 0;

        public enum States
        {
            Background,
            Music1,
            Music2,
            SantaVideo,
            DarthVader
        }

        Color[] treeColors = new Color[]
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.White
        };

        OperatingHours2 hours = new OperatingHours2();
        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();

        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderLedmx = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderHiFi = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderVideo1 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderSnow = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expanderControlPanel = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8899);
        AudioPlayer audioLedmx = new AudioPlayer();
        AudioPlayer audioHiFi = new AudioPlayer();
        AudioPlayer audioVideo1 = new AudioPlayer();
        AudioPlayer audioDarthVader = new AudioPlayer();

        AnalogInput3 faderR = new AnalogInput3(persistState: true);
        AnalogInput3 faderG = new AnalogInput3(persistState: true);
        AnalogInput3 faderB = new AnalogInput3(persistState: true);
        AnalogInput3 faderBright = new AnalogInput3(persistState: true);
        AnalogInput3 faderPan = new AnalogInput3(persistState: true);
        AnalogInput3 faderTilt = new AnalogInput3(persistState: true);


        Expander.AcnStream acnOutput = new Expander.AcnStream(priority: 150);
        Effect.Pulsating pulsatingEffectOlaf = new Effect.Pulsating(S(1), 0.5, 1.0, false);
        Effect.Pulsating pulsatingEffectPoppy = new Effect.Pulsating(S(1), 0.5, 1.0, false);
        Effect.Pulsating pulsatingEffectR2D2 = new Effect.Pulsating(S(1), 0.5, 1.0, false);
        Effect.Pulsating pulsatingEffect3 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingEffectGeneral = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingEffectTree = new Effect.Pulsating(S(3), 0.0, 1.0, false);
        Effect.Pulsating pulsatingPinSpot = new Effect.Pulsating(S(2), 0.2, 1.0, false);
        Effect.Pulsating pulsatingStar = new Effect.Pulsating(S(2), 0.2, 1.0, false);

        DigitalInput2 inOlaf = new DigitalInput2();
        DigitalInput2 inR2D2 = new DigitalInput2();
        DigitalInput2 inPoppy = new DigitalInput2();
        DigitalInput2 controlButtonYellow = new DigitalInput2();
        DigitalInput2 controlButtonBlue = new DigitalInput2();
        DigitalInput2 controlButtonWhite = new DigitalInput2();
        DigitalInput2 controlButtonGreen = new DigitalInput2();
        DigitalInput2 controlButtonBlack = new DigitalInput2();
        DigitalInput2 controlButtonRed = new DigitalInput2(holdTimeout: S(5));

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 inShowMachine = new DigitalInput2();

        DigitalOutput2 laser = new DigitalOutput2(initial: true);
        DigitalOutput2 airR2D2Olaf = new DigitalOutput2(initial: true);
        DigitalOutput2 airSantaPoppy1 = new DigitalOutput2(initial: true);
        DigitalOutput2 airSnowman = new DigitalOutput2(initial: true);
        DigitalOutput2 airSantaPopup = new DigitalOutput2(initial: true);
        DigitalOutput2 airReindeerBig = new DigitalOutput2();
        DigitalOutput2 snowMachine = new DigitalOutput2();
        Dimmer3 hazerFanSpeed = new Dimmer3();
        Dimmer3 hazerHazeOutput = new Dimmer3();

        Dimmer3 lightNet1 = new Dimmer3();
        Dimmer3 lightNet2 = new Dimmer3();
        Dimmer3 lightNet3 = new Dimmer3();
        Dimmer3 lightNet4 = new Dimmer3();
        Dimmer3 lightNet5 = new Dimmer3();
        Dimmer3 lightNet6 = new Dimmer3();
        Dimmer3 lightNet7 = new Dimmer3();
        Dimmer3 lightNet8 = new Dimmer3();
        Dimmer3 lightNet9 = new Dimmer3();
        Dimmer3 lightNet10 = new Dimmer3();
        Dimmer3 lightTopper1 = new Dimmer3();
        Dimmer3 lightTopper2 = new Dimmer3();
        Dimmer3 lightHangingStar = new Dimmer3();
        DigitalOutput2 lightXmasTree = new DigitalOutput2();
        Dimmer3 lightStairRail1 = new Dimmer3();
        Dimmer3 lightStairRail2 = new Dimmer3();
        Dimmer3 lightRail1 = new Dimmer3();
        Dimmer3 lightRail2 = new Dimmer3();
        Dimmer3 lightRail3 = new Dimmer3();
        Dimmer3 lightRail4 = new Dimmer3();
        Dimmer3 lightStairs1 = new Dimmer3();
        Dimmer3 lightStairs2 = new Dimmer3();
        Dimmer3 lightStairs3 = new Dimmer3();
        Dimmer3 lightTreeStars = new Dimmer3();
        StrobeColorDimmer3 lightPinSpot = new StrobeColorDimmer3();

        Dimmer3 lightSanta = new Dimmer3();
        Dimmer3 lightPoppy = new Dimmer3();
        Dimmer3 lightSnowman = new Dimmer3();
        Dimmer3 lightSantaPopup = new Dimmer3();
        MovingHead movingHead = new MovingHead();

        Dimmer3 lightHat1 = new Dimmer3();
        Dimmer3 lightHat2 = new Dimmer3();
        Dimmer3 lightHat3 = new Dimmer3();
        Dimmer3 lightHat4 = new Dimmer3();
        Dimmer3 lightReindeers = new Dimmer3();
        Dimmer3 lightReindeerBig = new Dimmer3();
        StrobeColorDimmer3 lightVader = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood1 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood2 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood3 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood4 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood5 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood6 = new StrobeColorDimmer3();
        StrobeColorDimmer3 lightFlood7 = new StrobeColorDimmer3();

        Dimmer3 lightOlaf = new Dimmer3();
        Dimmer3 lightR2D2 = new Dimmer3();
        VirtualPixel1D3 pixelsRoofEdge = new VirtualPixel1D3(150);
        VirtualPixel1D3 pixelsTree = new VirtualPixel1D3(50);
        VirtualPixel1D3 pixelsBetweenTrees = new VirtualPixel1D3(50);
        VirtualPixel1D3 pixelsHeart = new VirtualPixel1D3(50);
        VirtualPixel1D3 pixelsGround = new VirtualPixel1D3(50);
        VirtualPixel2D3 pixelsMatrix = new VirtualPixel2D3(20, 10);
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
        DigitalInput2 laserActivated = new DigitalInput2(persistState: true);

        Import.LorImport2 lorChristmasCanon = new Import.LorImport2();
        Import.LorImport2 lorBelieve = new Import.LorImport2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        Controller.Subroutine subCandyCane = new Controller.Subroutine();
        Controller.Subroutine subHeart = new Controller.Subroutine();
        Controller.Subroutine subStarWarsCane = new Controller.Subroutine();
        Controller.Subroutine subBackground = new Controller.Subroutine();
        Controller.Subroutine subSantaVideo = new Controller.Subroutine();
        Controller.Subroutine subMusic1 = new Controller.Subroutine();
        Controller.Subroutine subMusic2 = new Controller.Subroutine();
        Controller.Subroutine subOlaf = new Controller.Subroutine();
        Controller.Subroutine subR2D2 = new Controller.Subroutine();
        Controller.Subroutine subPoppy = new Controller.Subroutine();
        Controller.Subroutine subStarWars = new Controller.Subroutine();
        Controller.Subroutine subSnow = new Controller.Subroutine();
        Controller.Subroutine subMovingHead = new Controller.Subroutine();

        Import.DmxPlayback dmxPlayback = new Import.DmxPlayback();

        public Xmas2016(IEnumerable<string> args)
        {
            hours.AddRange("4:00 pm", "10:00 pm");

            hours.Output.Log("Hours");

            string expanderFilesFolder = string.Empty;
            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                {
                    expanderFilesFolder =
                    expanderServer.ExpanderSharedFiles = parts[1];
                }
            }

            pulsatingStar.ConnectTo(lightHangingStar);
            pulsatingEffectOlaf.ConnectTo(lightOlaf);
            pulsatingEffectPoppy.ConnectTo(lightPoppy);
            pulsatingEffectR2D2.ConnectTo(lightR2D2);
            pulsatingEffect3.ConnectTo(pixelsRoofEdge, Utils.AdditionalData(Color.Red));
            //pulsatingEffect4.ConnectTo(lightBlueButton);
            //pulsatingEffect4.ConnectTo(lightRedButton);
            pulsatingEffectGeneral.ConnectTo(lightHangingStar);
            pulsatingEffectGeneral.ConnectTo(lightOlaf);
            pulsatingEffectGeneral.ConnectTo(lightR2D2);
            pulsatingEffectGeneral.ConnectTo(lightPoppy);
            pulsatingEffectGeneral.ConnectTo(lightTreeStars);
            pulsatingEffectGeneral.ConnectTo(lightVader, Utils.AdditionalData(Color.Red));
            pulsatingPinSpot.ConnectTo(lightPinSpot, Utils.AdditionalData(Color.Red));
            pulsatingEffectTree.ConnectTo(pixelsTree, Utils.AdditionalData(treeColors[0]));
            pulsatingEffectTree.ConnectTo(pixelsBetweenTrees, Utils.AdditionalData(treeColors[0]));
            pulsatingEffectTree.NewIterationAction = i =>
                {
                    Color newColor = treeColors[i % treeColors.Length];
                    pulsatingEffectTree.SetAdditionalData(pixelsTree, Utils.AdditionalData(newColor));
                    pulsatingEffectTree.SetAdditionalData(pixelsBetweenTrees, Utils.AdditionalData(newColor));
                };

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expanderLedmx);          // rpi-eb0092ca
            expanderServer.AddInstance("76d09e6032d54e77aafec90e1fc4b35b", expanderHiFi);           // rpi-eb428ef1
            expanderServer.AddInstance("4ea781ef257442edb524493da8f52220", expanderVideo1);         // rpi-eba6cbc7
            expanderServer.AddInstance("999861affa294fd7bbf0601505e9ae09", expanderSnow);           // rpi-ebd43a38
            expanderServer.AddInstance("e41d2977931d4887a9417e8adcd87306", expanderControlPanel);   // rpi-eb6a047c

            expanderLedmx.DigitalInputs[6].Connect(inPoppy);
            expanderLedmx.DigitalInputs[5].Connect(inR2D2);
            expanderLedmx.DigitalInputs[4].Connect(inOlaf);
            expanderSnow.DigitalOutputs[6].Connect(snowMachine);

            expanderControlPanel.DigitalInputs[0].Connect(controlButtonYellow, true);
            expanderControlPanel.DigitalInputs[1].Connect(controlButtonBlue, true);
            expanderControlPanel.DigitalInputs[2].Connect(controlButtonWhite, true);
            expanderControlPanel.DigitalInputs[3].Connect(controlButtonGreen, true);
            expanderControlPanel.DigitalInputs[4].Connect(controlButtonBlack, true);
            expanderControlPanel.DigitalInputs[5].Connect(controlButtonRed, true);

            expanderLedmx.Connect(audioLedmx);
            expanderHiFi.Connect(audioHiFi);
            //expander3.Connect(video3);
            expanderSnow.Connect(audioDarthVader);
            expanderVideo1.Connect(audioVideo1);

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            dmxPlayback.Load(Path.Combine(expanderFilesFolder, "Seq", "XmasLoop.bin"), 15);
            dmxPlayback.Loop = true;

            var pixelMapping = Framework.Utility.PixelMapping.GeneratePixelMappingFromGlediatorPatch(
                Path.Combine(expanderFilesFolder, "Glediator", "ArtNet 14-15 20x10.patch.glediator"));
            dmxPlayback.SetOutput(pixelsMatrix, pixelMapping);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    hours.SetForced(true);
                else
                    hours.SetForced(null);
            });

            inflatablesRunning.Subscribe(x =>
            {
                airReindeerBig.SetValue(x);
            });

            stateMachine.ForFromSubroutine(States.Background, subBackground);
            stateMachine.ForFromSubroutine(States.Music1, subMusic1);
            stateMachine.ForFromSubroutine(States.Music2, subMusic2);
            stateMachine.ForFromSubroutine(States.SantaVideo, subSantaVideo);
            stateMachine.ForFromSubroutine(States.DarthVader, subStarWars);

            hours
                .ControlsMasterPower(laser)
                .ControlsMasterPower(airR2D2Olaf)
                .ControlsMasterPower(airSantaPoppy1)
                .ControlsMasterPower(airSnowman)
                .ControlsMasterPower(airSantaPopup);

            buttonStartInflatables.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen)
                {
                    inflatablesRunning.OnNext(true);
                    Exec.SetKey("InflatablesRunning", "True");
                }
            });

            laserActivated.WhenOutputChanges(x =>
            {
                laser.SetValue(x);
            });

            buttonTest.WhenOutputChanges(x =>
            {
            });

            buttonReset.WhenOutputChanges(x =>
            {
                if (x)
                    stateMachine.GoToDefaultState();
            });

            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 0, 50, reverse: true), SacnUniverseLED50, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 50, 100), SacnUniverseLED100, 1);

            acnOutput.Connect(new Physical.Pixel1D(pixelsTree, 0, 50), SacnUniverseLEDTree50, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsBetweenTrees, 0, 50), SacnUniversePixelString1, 1);
            //acnOutput.Connect(new Physical.Pixel1D(pixelsTree, 0, 50), SacnUniversePixelString2, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsGround, 0, 50), SacnUniversePixelGround, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsHeart, 0, 50), SacnUniversePixelString4, 1);

            var pixelMapping2D = Framework.Utility.PixelMapping.GeneratePixelMapping(20, 10, pixelOrder: Framework.Utility.PixelOrder.HorizontalSnakeBottomLeft);
            acnOutput.Connect(new Physical.Pixel2D(pixelsMatrix, pixelMapping2D), SacnUniversePixelMatrix, 1);

            acnOutput.Connect(new Physical.Pixel1D(saberPixels), SacnUniversePixelSaber, 1);
            acnOutput.Connect(new Physical.Pixel1D(haloPixels), SacnUniversePixelSaber, 100);

            acnOutput.Connect(new Physical.GenericDimmer(airReindeerBig, 10), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(airR2D2Olaf, 33), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 21), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 22), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 23), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 24), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairRail1, 19), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairRail2, 38), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail1, 20), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail2, 28), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail3, 29), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail4, 66), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightVader, 310), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.RGBStrobe(lightFlood1, 60), SacnUniverseEdmx4);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood2, 330), SacnUniverseEdmx4);
            acnOutput.Connect(new Physical.RGBStrobe(lightFlood3, 70), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.RGBStrobe(lightFlood4, 40), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood5, 340), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.RGBStrobe(lightFlood4, 40), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood5, 340), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.RGBStrobe(lightFlood6, 80), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood7, 300), SacnUniverseLedmx);

            acnOutput.Connect(new Physical.GenericDimmer(laser, 1), SacnUniverseRenard18);

            acnOutput.Connect(new Physical.GenericDimmer(lightOlaf, 128), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(airSnowman, 133), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightSnowman, 131), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightSanta, 132), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightPoppy, 134), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightSantaPopup, 263), SacnUniverseEdmx4);
            acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(movingHead, 200), SacnUniverseEdmx4);

            acnOutput.Connect(new Physical.GenericDimmer(lightReindeerBig, 65), SacnUniverseLedmx);

            acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 64), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 25), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairs3, 2), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(lightPinSpot, 20), SacnUniverseEdmx4);
            acnOutput.Connect(new Physical.GenericDimmer(lightTreeStars, 39), SacnUniverseRenard18);

            acnOutput.Connect(new Physical.GenericDimmer(lightR2D2, 37), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 11), SacnUniverseRenardBig);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 19), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(airSantaPopup, 15), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(airSantaPoppy1, 30), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 22), SacnUniverseRenardBig);

            acnOutput.Connect(new Physical.GenericDimmer(hazerFanSpeed, 500), SacnUniverseEdmx4);
            acnOutput.Connect(new Physical.GenericDimmer(hazerHazeOutput, 501), SacnUniverseEdmx4);
            //            acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 25), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(lightXmasTree, 11), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightReindeers, 40), SacnUniverseRenard18);

            //            acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 1), SacnUniverseRenardSmall);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 2), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet1, 5), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 6), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet3, 7), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightTopper1, 3), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightTopper2, 4), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar, 8), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet4, 26), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 27), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 34), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 35), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet8, 36), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet9, 50), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet10, 51), SacnUniverseLedmx);

            //acnOutput.Connect(new Physical.GenericDimmer(lightNet3, 4), SacnUniverseRenardSmall);
            //acnOutput.Connect(new Physical.GenericDimmer(lightNet4, 5), SacnUniverseRenardSmall);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTopper1, 7), SacnUniverseRenardSmall);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTopper2, 8), SacnUniverseRenardSmall);

            faderR.WhenOutputChanges(v => { SetManualColor(); });
            faderG.WhenOutputChanges(v => { SetManualColor(); });
            faderB.WhenOutputChanges(v => { SetManualColor(); });
            faderBright.WhenOutputChanges(v => { SetManualColor(); });
            faderPan.Output.Subscribe(p =>
            {
                movingHead.SetPan(p * 540, null);
            });
            faderTilt.Output.Subscribe(t =>
            {
                movingHead.SetTilt(t * 270, null);
            });

            hours.Output.Subscribe(x =>
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

            subBackground
                .AutoAddDevices(lockPriority: 100)
                .RunAction(i =>
                {
                    lightStairs1.SetBrightness(1);
                    lightStairs2.SetBrightness(1);
                    lightStairs3.SetBrightness(1);
                    lightReindeerBig.SetBrightness(1);
                    pulsatingEffectGeneral.Start();
                    pulsatingEffectTree.Start();
                    pulsatingPinSpot.Start();
                    lightNet1.SetBrightness(1);
                    lightNet2.SetBrightness(1);
                    lightNet3.SetBrightness(1);
                    lightNet4.SetBrightness(1);
                    lightNet5.SetBrightness(1);
                    lightNet6.SetBrightness(1);
                    lightNet7.SetBrightness(1);
                    lightNet8.SetBrightness(1);
                    lightNet9.SetBrightness(1);
                    lightNet10.SetBrightness(1);
                    lightTopper1.SetBrightness(1);
                    lightTopper2.SetBrightness(1);
                    lightXmasTree.SetValue(true);
                    lightStairRail1.SetBrightness(1);
                    lightStairRail2.SetBrightness(1);
                    lightRail1.SetBrightness(1);
                    lightRail2.SetBrightness(1);
                    lightRail3.SetBrightness(1);
                    lightRail4.SetBrightness(1);
                    lightSanta.SetBrightness(1);
                    lightSantaPopup.SetBrightness(1);
                    lightSnowman.SetBrightness(1);
                    lightHat1.SetBrightness(1);
                    lightHat2.SetBrightness(1);
                    lightHat3.SetBrightness(1);
                    lightHat4.SetBrightness(1);
                    lightReindeers.SetBrightness(1);
                    //lightVader.SetColor(Color.Red, 1);
                    lightFlood1.SetColor(Color.Red, 1);
                    lightFlood2.SetColor(Color.Red, 1);
                    lightFlood3.SetColor(Color.Red, 1);
                    lightFlood4.SetColor(Color.Red, 1);
                    lightFlood5.SetColor(Color.Red, 1);
                    lightFlood6.SetColor(Color.Red, 1);
                    lightFlood7.SetColor(Color.Red, 1);

                    saberPixels.SetColor(Color.Red, 0.4, i.Token);

                    subCandyCane.Run();
                    subHeart.Run();
                    dmxPlayback.Run();

                    i.WaitUntilCancel();

                    dmxPlayback.Stop();
                    Exec.Cancel(subHeart);
                    Exec.Cancel(subCandyCane);
                    pulsatingEffectGeneral.Stop();
                    pulsatingPinSpot.Stop();
                    pulsatingEffectTree.Stop();
                });

            subCandyCane
                .LockWhenRunning(pixelsRoofEdge)
                .LockWhenRunning(pixelsGround)
                .RunAction(i =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int x = 0; x < spacing; x++)
                        {
                            pixelsRoofEdge.Inject((x % spacing) == 0 ? Color.Red : Color.White, 0.5, i.Token);
                            pixelsGround.Inject((x % spacing) == 0 ? Color.Red : Color.White, 0.5, i.Token);

                            i.WaitFor(S(0.30), true);
                        }
                    }
                });

            subHeart
                .LockWhenRunning(pixelsHeart)
                .RunAction(i =>
                {
                    var levels = new double[]
                    {
                        0.1,
                        0.2,
                        0.3,
                        0.4,
                        0.6,
                        0.8,
                        0.9,
                        1.0,
                        0.9,
                        0.8,
                        0.6,
                        0.4,
                        0.3,
                        0.2,
                    };

                    while (true)
                    {
                        for (int x = 0; x < levels.Length; x++)
                        {
                            double brightness = levels[x];
                            pixelsHeart.Inject(Color.Red, brightness, i.Token);

                            i.WaitFor(S(0.50), true);
                        }
                    }
                });

            subSnow
                .RunAction(ins =>
                {
                    pulsatingPinSpot.SetAdditionalData(lightPinSpot, Utils.AdditionalData(Color.White));
                    snowMachine.SetValue(true);

                    ins.WaitFor(S(15));
                })
                .TearDown(i =>
                {
                    pulsatingPinSpot.SetAdditionalData(lightPinSpot, Utils.AdditionalData(Color.Red));
                    snowMachine.SetValue(false);
                });

            subStarWarsCane
                .LockWhenRunning(
                    pixelsRoofEdge,
                    pixelsMatrix)
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
                                    pixelsRoofEdge.InjectRev(Color.Yellow, 1.0, instance.Token);
                                    break;
                                case 2:
                                case 3:
                                    pixelsRoofEdge.InjectRev(Color.Orange, 0.2, instance.Token);
                                    break;
                            }

                            instance.WaitFor(S(0.1));

                            if (instance.IsCancellationRequested)
                                break;
                        }
                    }
                });

            subMovingHead
                .RunAction(ins =>
                    {
                        while (!ins.IsCancellationRequested)
                        {
                            ins.WaitFor(S(0.1));
                        }
                    });

            subMusic1
                .AutoAddDevices()
                .RunAction(ins =>
                    {
                        lightSantaPopup.SetBrightness(1);
                        movingHead.SetColor(Color.Red, 1);
                        lightXmasTree.SetValue(true);
                        audioHiFi.PlayTrack("21. Christmas Canon Rock.wav");
                        ins.WaitFor(S(300));
                    }).TearDown(i =>
                    {
                        lorChristmasCanon.Stop();
                        audioHiFi.PauseTrack();
                    });

            subMusic2
                .AutoAddDevices()
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    movingHead.SetColor(Color.Red, 1);
                    lightXmasTree.SetValue(true);
                    audioHiFi.PlayTrack("T.P.E. - 04 - Josh Groban - Believe.flac");
                    ins.WaitFor(S(260));
                }).TearDown(i =>
                {
                    lorBelieve.Stop();
                    audioHiFi.PauseTrack();
                });

            subSantaVideo
                .RunAction(i =>
                {
                    pulsatingEffect3.Start();
                    //switch (random.Next(6))
                    //{
                    //    case 0:
                    //        video3.PlayVideo("NBC_DeckTheHalls_Holl_H.mp4");
                    //        break;

                    //    case 1:
                    //        video3.PlayVideo("NBC_AllThruHouse_Part1_Holl_H.mp4");
                    //        break;

                    //    case 2:
                    //        video3.PlayVideo("NBC_AllThruHouse_Part2_Holl_H.mp4");
                    //        break;

                    //    case 3:
                    //        video3.PlayVideo("NBC_AllThruHouse_Part3_Holl_H.mp4");
                    //        break;

                    //    case 4:
                    //        video3.PlayVideo("NBC_JingleBells_Holl_H.mp4");
                    //        break;

                    //    case 5:
                    //        video3.PlayVideo("NBC_WeWishYou_Holl_H.mp4");
                    //        break;
                    //}

                    i.WaitFor(S(120), false);
                    pulsatingEffect3.Stop();
                });

            subPoppy
                .RunAction(i =>
                {
                    pulsatingEffectPoppy.Start(100);
                    audioVideo1.PlayNewEffect("Trolls Sounds of Silence.wav");
                    i.WaitFor(S(46));
                    pulsatingEffectPoppy.Stop();
                });

            subOlaf
                .RunAction(i =>
                {
                    pulsatingEffectOlaf.Start(100);
                    audioLedmx.PlayNewEffect("WarmHugs.wav", 0.0, 1.0);
                    i.WaitFor(S(10));
                    pulsatingEffectOlaf.Stop();
                });

            subR2D2
                .RunAction(i =>
                {
                    pulsatingEffectR2D2.Start(100);
                    audioLedmx.PlayNewEffect("Im C3PO.wav", 1.0, 0.0);
                    i.WaitFor(S(4));
                    audioLedmx.PlayNewEffect("Processing R2D2.wav", 0.5, 0.0);
                    i.WaitFor(S(5));
                    pulsatingEffectR2D2.Stop();
                });

            subStarWars
                .LockWhenRunning(
                    saberPixels,
                    haloPixels,
                    lightVader,
                    lightR2D2,
                    lightHangingStar,
                    laser)
                .RunAction(instance =>
                {
                    laser.SetValue(false);
                    //Exec.Cancel(subCandyCane);
                    subStarWarsCane.Run();
                    lightR2D2.SetBrightness(1.0, instance.Token);

                    audioHiFi.PlayTrack("01. Star Wars - Main Title.wav");

                    instance.WaitFor(S(16));

                    var haloJob = haloPixels.Chaser(new IData[] {
                        Utils.Data(Color.White, 1.0),
                        Utils.Data(Color.White, 0.7),
                        Utils.Data(Color.White, 0.5),
                        Utils.Data(Color.White, 0.3)
                    }, 4, token: instance.Token);

                    pulsatingStar.Start();

                    Exec.MasterEffect.Fade(lightVader, 0.0, 1.0, 1000, token: instance.Token, additionalData: Utils.AdditionalData(Color.Red));
                    instance.WaitFor(S(2.5));

                    Exec.Cancel(subStarWarsCane);
                    instance.WaitFor(S(0.5));

                    audioDarthVader.PlayEffect("saberon.wav");
                    for (int sab = 0; sab < 33; sab++)
                    {
                        saberPixels.Inject(Color.Red, 0.5, instance.Token);
                        instance.WaitFor(S(0.01));
                    }
                    instance.WaitFor(S(1));
                    audioHiFi.PauseTrack();

                    lightVader.SetColor(Color.Red, 1.0, instance.Token);
                    audioDarthVader.PlayEffect("father.wav");
                    instance.WaitFor(S(5));

                    audioDarthVader.PlayEffect("force1.wav");
                    instance.WaitFor(S(4));

                    lightVader.SetBrightness(0.0, instance.Token);

                    audioDarthVader.PlayEffect("saberoff.wav");
                    instance.WaitFor(S(0.7));
                    for (int sab = 0; sab < 17; sab++)
                    {
                        saberPixels.InjectRev(Color.Black, 0, instance.Token);
                        saberPixels.InjectRev(Color.Black, 0, instance.Token);
                        instance.WaitFor(S(0.01));
                    }
                    Exec.StopManagedTask(haloJob);
                    pulsatingStar.Stop();

                    instance.WaitFor(S(2));
                })
                .TearDown(i =>
                {
                    laser.SetValue(true);
                    audioHiFi.PauseTrack();
                });


            inOlaf.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    subOlaf.Run();
            });

            inR2D2.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    subR2D2.Run();
            });

            inPoppy.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    subPoppy.Run();
            });

            controlButtonYellow.WhenOutputChanges(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    stateMachine.GoToState(States.DarthVader);
            });

            controlButtonBlue.WhenOutputChanges(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    stateMachine.GoToState(States.Music2);
            });

            controlButtonWhite.WhenOutputChanges(x =>
            {
                if (x && hours.IsOpen)
                    subSnow.Run();
            });

            controlButtonGreen.WhenOutputChanges(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    stateMachine.GoToState(States.Music1);
            });

            controlButtonBlack.WhenOutputChanges(x =>
            {
                if (x && stateMachine.CurrentState != States.DarthVader)
                    audioDarthVader.PlayEffect("force1.wav");
            });

            controlButtonRed.WhenOutputChanges(x =>
            {
                if (x && stateMachine.CurrentState != States.DarthVader)
                    audioDarthVader.PlayEffect("darthvader_lackoffaith.wav");
            });

            controlButtonRed.IsHeld.Subscribe(x =>
            {
                if (x)
                    this.stateMachine.GoToDefaultState();
            });

            /*
                        inRedButton.Output.Subscribe(x =>
                        {
                            if (x)
                            {
                                if (hours.IsOpen)
                                {
                                    if (stateMachine.CurrentState == States.Background)
                                        stateMachine.GoToState(States.DarthVader);
                                }
                                else
                                    audioDarthVader.PlayEffect("darthvader_lackoffaith.wav");
                            }
                        });

                        inBlueButton.Output.Subscribe(x =>
                        {
                            if (x)
                            {
                                if (hours.IsOpen)
                                {
                                    subSnow.Run();
                                    if (stateMachine.CurrentState == States.Background)
                                        stateMachine.GoToState(States.Music2);
                                }
                                else
                                    audioDarthVader.PlayEffect("darkside.wav");
                            }
                        });
            */
            audioHiFi.AudioTrackStart += (o, e) =>
            {
                switch (e.FileName)
                {
                    case "21. Christmas Canon Rock.wav":
                        lorChristmasCanon.Start();
                        break;

                    case "T.P.E. - 04 - Josh Groban - Believe.flac":
                        lorBelieve.Start();
                        break;
                }
            };

            audioHiFi.AudioTrackDone += (o, e) =>
            {
                //                Thread.Sleep(5000);
                //    audio2.PlayTrack("08 Feel the Light.wav");
            };

            inShowMachine.Output.Subscribe(x =>
            {
                snowMachine.SetValue(x);
                //                lightRedButton.SetBrightness(x ? 1.0 : 0.0);
                //if (x)
                //    stateMachine.GoToState(States.Music1);
            });

            ImportAndMapChristmasCanon();
            ImportAndMapBelieve();

            ConfigureMIDI();
            ConfigureOSC();
        }

        private Color GetFaderColor()
        {
            return HSV.ColorFromRGB(faderR.Value, faderG.Value, faderB.Value);
        }

        private void SetManualColor()
        {
            //if (manualFaderToken != null)
            //{
            movingHead.SetColor(GetFaderColor(), faderBright.Value, null);
            //}
        }

        private void ImportAndMapChristmasCanon()
        {
            lorChristmasCanon.LoadFromFile(Path.Combine(expanderServer.ExpanderSharedFiles, "Seq", "Cannon Rock104.lms"));

            lorChristmasCanon.Progress.Subscribe(x =>
            {
                log.Trace("Christmas Canon {0:N0} ms", x);
            });

            //            lorChristmasCanon.Dump();

            lorChristmasCanon.MapDevice("Roof 1", pixelsGround, Utils.AdditionalData(Color.Red));
            lorChristmasCanon.MapDevice("Roof 2", pixelsTree, Utils.AdditionalData(Color.Green));
            lorChristmasCanon.MapDevice("Roof 3", pixelsHeart, Utils.AdditionalData(Color.Red));

            lorChristmasCanon.ControlDevice(pixelsBetweenTrees);
            lorChristmasCanon.MapDevice("Big Tree 1",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 6, t)));
            lorChristmasCanon.MapDevice("Big Tree 2",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 6, t)));
            lorChristmasCanon.MapDevice("Big Tree 3",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 6, t)));
            lorChristmasCanon.MapDevice("Big Tree 4",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 6, t)));
            lorChristmasCanon.MapDevice("Big Tree 5",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 6, t)));
            lorChristmasCanon.MapDevice("Big Tree 6",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 6, t)));
            lorChristmasCanon.MapDevice("Big Tree 7",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 6, t)));
            lorChristmasCanon.MapDevice("Big Tree 8",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 6, t)));

            lorChristmasCanon.MapDevice("Sidewalk 1", lightNet1);
            lorChristmasCanon.MapDevice("Sidewalk 2", lightNet2);
            lorChristmasCanon.MapDevice("Sidewalk 3", lightNet3);
            lorChristmasCanon.MapDevice("Sidewalk 4", lightNet4);
            lorChristmasCanon.MapDevice("Sidewalk 5", lightNet5);
            lorChristmasCanon.MapDevice("Sidewalk 6", lightNet6);
            lorChristmasCanon.MapDevice("Sidewalk 7", lightNet7);
            lorChristmasCanon.MapDevice("Sidewalk 8", lightNet8);
            lorChristmasCanon.MapDevice("Sidewalk 1", lightNet9);
            lorChristmasCanon.MapDevice("Sidewalk 2", lightNet10);

            lorChristmasCanon.MapDevice("Sidewalk 1", lightHat1);
            lorChristmasCanon.MapDevice("Sidewalk 2", lightHat2);
            lorChristmasCanon.MapDevice("Sidewalk 3", lightHat3);
            lorChristmasCanon.MapDevice("Sidewalk 4", lightHat4);

            lorChristmasCanon.MapDevice("Bush Right", lightSanta);
            lorChristmasCanon.MapDevice("Bush Right", lightTopper1);
            lorChristmasCanon.MapDevice("Column Red Right", lightFlood1, Utils.AdditionalData(Color.Red));
            lorChristmasCanon.MapDevice("Column Blue Right", lightFlood2, Utils.AdditionalData(Color.Blue));
            lorChristmasCanon.MapDevice("Column Red Right", lightFlood5, Utils.AdditionalData(Color.Red));
            lorChristmasCanon.MapDevice("Column Blue Right", lightFlood6, Utils.AdditionalData(Color.Blue));
            lorChristmasCanon.MapDevice("Rail Right", lightStairRail1);
            lorChristmasCanon.MapDevice("Column Red Left", lightFlood3, Utils.AdditionalData(Color.Red));
            lorChristmasCanon.MapDevice("Column Blue Left", lightFlood4, Utils.AdditionalData(Color.Blue));
            lorChristmasCanon.MapDevice("Column Blue Left", lightFlood7, Utils.AdditionalData(Color.Blue));
            lorChristmasCanon.MapDevice("Rail Left", lightStairRail2);
            lorChristmasCanon.MapDevice("Ice Cycles", lightHangingStar);
            lorChristmasCanon.MapDevice("Ice Cycles", lightTreeStars);
            lorChristmasCanon.MapDevice("Left Window April", lightOlaf);
            lorChristmasCanon.MapDevice("Left Wreif", lightR2D2);
            lorChristmasCanon.MapDevice("Left Wreif", lightStairs3);
            lorChristmasCanon.MapDevice("Main Door", lightPoppy);
            lorChristmasCanon.MapDevice("Right Wreif", lightReindeers);
            lorChristmasCanon.MapDevice("Right Wreif", lightStairs2);
            lorChristmasCanon.MapDevice("Right Window", lightReindeerBig);
            lorChristmasCanon.MapDevice("Right Window", lightStairs1);
            lorChristmasCanon.MapDevice("Bush Left", lightSnowman);
            lorChristmasCanon.MapDevice("Bush Left", lightTopper2);

            lorChristmasCanon.ControlDevice(pixelsMatrix);
            lorChristmasCanon.MapDevice("Tree 1",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.White, b, 0, 0, 3, 10, t)));
            lorChristmasCanon.MapDevice("Tree 2",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 3, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 3",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 4, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 4",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 5, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 5",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 6, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 6",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 7, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 7",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 8, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 8",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 9, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 01",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 10, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 02",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 11, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 03",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 12, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 04",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 13, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 05",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 14, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 06",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 15, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 07",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 16, 0, 1, 10, t)));
            lorChristmasCanon.MapDevice("Tree 08",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.White, b, 17, 0, 3, 10, t)));

            lorChristmasCanon.ControlDevice(pixelsRoofEdge);
            lorChristmasCanon.MapDevice("Arch 1",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 0, 10, t)));
            lorChristmasCanon.MapDevice("Arch 2",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 10, 10, t)));
            lorChristmasCanon.MapDevice("Arch 3",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 20, 10, t)));
            lorChristmasCanon.MapDevice("Arch 4",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 30, 10, t)));
            lorChristmasCanon.MapDevice("Arch 5",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 40, 10, t)));
            lorChristmasCanon.MapDevice("Arch 6",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 50, 10, t)));
            lorChristmasCanon.MapDevice("Arch 7",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 60, 10, t)));
            lorChristmasCanon.MapDevice("Arch 8",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 70, 10, t)));
            lorChristmasCanon.MapDevice("Arch 9",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 80, 10, t)));
            lorChristmasCanon.MapDevice("Arch 10",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 90, 10, t)));
            lorChristmasCanon.MapDevice("Arch 11",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 100, 10, t)));
            lorChristmasCanon.MapDevice("Arch 12",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 110, 10, t)));
            lorChristmasCanon.MapDevice("Arch 13",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 120, 10, t)));
            lorChristmasCanon.MapDevice("Arch 14",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 130, 10, t)));
            lorChristmasCanon.MapDevice("Arch 15",
                new VirtualDevice((b, t) => pixelsRoofEdge.SetColorRange(Color.Red, b, 140, 10, t)));

            lorChristmasCanon.Prepare();
        }

        private void ImportAndMapBelieve()
        {
            lorBelieve.LoadFromFile(Path.Combine(expanderServer.ExpanderSharedFiles, "Seq", "Believe - Josh Groban 64 chns.lms"));

            lorBelieve.Progress.Subscribe(x =>
            {
                log.Trace("Believe {0:N0} ms", x);
            });

            lorBelieve.Dump();

            lorBelieve.MapDevice("Yard 1", lightNet1);
            lorBelieve.MapDevice("Yard 2", lightNet2);
            lorBelieve.MapDevice("Yard 3", lightNet3);
            lorBelieve.MapDevice("Yard 4", lightNet4);
            lorBelieve.MapDevice("Yard 5", lightNet5);
            lorBelieve.MapDevice("Yard 6", lightNet6);
            lorBelieve.MapDevice("Yard 7", lightNet7);
            lorBelieve.MapDevice("Yard 8", lightNet8);
            lorBelieve.MapDevice("Yard 9", lightNet9);
            lorBelieve.MapDevice("Yard 10", lightNet10);
            lorBelieve.MapDevice("Yard 5", lightHat1);
            lorBelieve.MapDevice("Yard 6", lightHat2);
            lorBelieve.MapDevice("Yard 7", lightHat3);
            lorBelieve.MapDevice("Yard 8", lightHat4);

            lorBelieve.MapDevice("Yard 9", lightTreeStars);
            lorBelieve.MapDevice("Yard 10", lightReindeerBig);

            lorBelieve.MapDevice("House 1", lightR2D2);
            lorBelieve.MapDevice("House 2", lightOlaf);
            lorBelieve.MapDevice("House 3", lightPoppy);

            lorBelieve.MapDevice("Wreath W", lightStairs1);
            lorBelieve.MapDevice("Wreath R", lightStairs2);
            lorBelieve.MapDevice("Wreath W", lightStairs3);
            lorBelieve.MapDevice("Wreath W", lightStairRail1);
            lorBelieve.MapDevice("Wreath R", lightStairRail2);

            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood1);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood2);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood3);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood4);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood5);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood6);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood7);

            lorBelieve.MapDevice("Ferris Wheel 1", lightTopper1);
            lorBelieve.MapDevice("Ferris Wheel 2", lightTopper2);
            lorBelieve.MapDevice("Ferris Wheel 3", lightRail1);
            lorBelieve.MapDevice("Ferris Wheel 4", lightRail2);
            lorBelieve.MapDevice("Ferris Wheel 5", lightReindeers);
            lorBelieve.MapDevice("Ferris Wheel 5", lightRail3);
            lorBelieve.MapDevice("Ferris Wheel 6", lightRail4);
            lorBelieve.MapDevice("Ferris Wheel 7", lightSanta);
            lorBelieve.MapDevice("Ferris Wheel 8", lightSnowman);

            lorBelieve.MapDevice("NATIVITY", lightHangingStar);

            lorBelieve.ControlDevice(pixelsMatrix);
            lorBelieve.MapDevice("Mega Tree 1",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 0, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 2",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 0, 1, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 3",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 2, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 4",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 0, 3, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 5",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 4, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 6",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 0, 5, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 7",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 6, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 8",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Red, b, 0, 7, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 9",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 8, 20, 1, t)));
            lorBelieve.MapDevice("Mega Tree 10",
                new VirtualDevice((b, t) => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 9, 20, 1, t)));

            lorBelieve.ControlDevice(pixelsBetweenTrees);
            lorBelieve.MapDevice("Mega Tree 1",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, t)));
            lorBelieve.MapDevice("Mega Tree 2",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 3, 3, t)));
            lorBelieve.MapDevice("Mega Tree 3",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, t)));
            lorBelieve.MapDevice("Mega Tree 4",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 9, 3, t)));
            lorBelieve.MapDevice("Mega Tree 5",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, t)));
            lorBelieve.MapDevice("Mega Tree 6",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 15, 3, t)));
            lorBelieve.MapDevice("Mega Tree 7",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, t)));
            lorBelieve.MapDevice("Mega Tree 8",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 21, 3, t)));
            lorBelieve.MapDevice("Mega Tree 9",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, t)));
            lorBelieve.MapDevice("Mega Tree 10",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 27, 3, t)));
            lorBelieve.MapDevice("Mega Tree 11",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, t)));
            lorBelieve.MapDevice("Mega Tree 12",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 33, 3, t)));
            lorBelieve.MapDevice("Mega Tree 13",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, t)));
            lorBelieve.MapDevice("Mega Tree 14",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 39, 3, t)));
            lorBelieve.MapDevice("Mega Tree 15",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, t)));
            lorBelieve.MapDevice("Mega Tree 16",
                new VirtualDevice((b, t) => pixelsBetweenTrees.SetColorRange(Color.Red, b, 45, 3, t)));

            lorBelieve.MapDevice("Mega Star", pixelsRoofEdge, Utils.AdditionalData(Color.Red));
            lorBelieve.MapDevice("Mega Star", pixelsGround, Utils.AdditionalData(Color.White));
            lorBelieve.MapDevice("Mega Star", pixelsTree, Utils.AdditionalData(Color.Red));
            lorBelieve.MapDevice("Mega Star", pixelsHeart, Utils.AdditionalData(Color.Red));

            lorBelieve.Prepare();
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
