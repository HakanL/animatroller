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
            MusicChristmasCanon,
            MusicBelieve,
            SantaVideo,
            DarthVader,
            MusicSarajevo,
            MusicHolyNight,
            MusicCarol
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
        Effect.Pulsating pulsatingEffectGeneral = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingEffectTree = new Effect.Pulsating(S(3), 0.0, 1.0, false);
        Effect.Pulsating pulsatingPinSpot = new Effect.Pulsating(S(2), 0.2, 1.0, false);
        Effect.Pulsating pulsatingStar = new Effect.Pulsating(S(2), 0.2, 1.0, false);

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

        DigitalOutput2 laser = new DigitalOutput2(initial: true);
        DigitalOutput2 airR2D2Olaf = new DigitalOutput2(initial: true);
        DigitalOutput2 airSantaPoppy1 = new DigitalOutput2(initial: true);
        DigitalOutput2 airSnowman = new DigitalOutput2(initial: true);
        DigitalOutput2 airTree = new DigitalOutput2(initial: true);
        DigitalOutput2 airSantaPopup = new DigitalOutput2(initial: true);
        DigitalOutput2 airReindeerBig = new DigitalOutput2();
        DigitalOutput2 snowMachine = new DigitalOutput2();
        Dimmer3 hazerFanSpeed = new Dimmer3();
        Dimmer3 hazerHazeOutput = new Dimmer3();
        Dimmer3 lightInflatableTree = new Dimmer3();

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
        Import.LorImport2 lorJingleBells = new Import.LorImport2();
        Import.LorImport2 lorSarajevo = new Import.LorImport2();
        Import.LorImport2 lorHolyNight = new Import.LorImport2();
        Import.LorImport2 lorCarol = new Import.LorImport2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        Controller.Subroutine subCandyCane = new Controller.Subroutine();
        Controller.Subroutine subHeart = new Controller.Subroutine();
        Controller.Subroutine subStarWarsCane = new Controller.Subroutine();
        Controller.Subroutine subBackground = new Controller.Subroutine();
        Controller.Subroutine subSantaVideo = new Controller.Subroutine();
        Controller.Subroutine subRandomSantaVideo = new Controller.Subroutine();
        Controller.Subroutine subMusicChristmasCanon = new Controller.Subroutine();
        Controller.Subroutine subMusicBelieve = new Controller.Subroutine();
        Controller.Subroutine subMusicSarajevo = new Controller.Subroutine();
        Controller.Subroutine subMusicHolyNight = new Controller.Subroutine();
        Controller.Subroutine subMusicCarol = new Controller.Subroutine();
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
            pulsatingEffectOlaf.ConnectTo(lightOlaf);
            pulsatingEffectPoppy.ConnectTo(lightPoppy);
            pulsatingEffectR2D2.ConnectTo(lightR2D2);
            pulsatingEffectGeneral.ConnectTo(lightHangingStar);
            pulsatingEffectGeneral.ConnectTo(lightOlaf);
            pulsatingEffectGeneral.ConnectTo(lightR2D2);
            pulsatingEffectGeneral.ConnectTo(lightPoppy);
            pulsatingEffectGeneral.ConnectTo(lightTreeStars);
            pulsatingEffectGeneral.ConnectTo(lightVader, Utils.Data(Color.Red));
            pulsatingPinSpot.ConnectTo(lightPinSpot, Utils.Data(Color.Red));
            pulsatingEffectTree.ConnectTo(pixelsTree, Utils.Data(treeColors[0]));
            pulsatingEffectTree.ConnectTo(pixelsBetweenTrees, Utils.Data(treeColors[0]));
            pulsatingEffectTree.NewIterationAction = i =>
                {
                    Color newColor = treeColors[i % treeColors.Length];
                    pulsatingEffectTree.SetAdditionalData(pixelsTree, Utils.Data(newColor));
                    pulsatingEffectTree.SetAdditionalData(pixelsBetweenTrees, Utils.Data(newColor));
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

            expanderControlPanel.DigitalInputs[0].Connect(controlButtonWhite, true);
            expanderControlPanel.DigitalInputs[1].Connect(controlButtonYellow, true);
            expanderControlPanel.DigitalInputs[2].Connect(controlButtonBlue, true);
            expanderControlPanel.DigitalInputs[3].Connect(controlButtonGreen, true);
            expanderControlPanel.DigitalInputs[4].Connect(controlButtonBlack, true);
            expanderControlPanel.DigitalInputs[5].Connect(controlButtonRed, true);

            expanderLedmx.Connect(audioLedmx);
            expanderHiFi.Connect(audioHiFi);
            expanderSnow.Connect(audioDarthVader);
            expanderVideo1.Connect(audioVideo1);

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            dmxPlayback.Load(new Import.BinaryFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "XmasLoop.bin")), 15);
            dmxPlayback.Loop = true;

            var pixelMapping = Framework.Utility.PixelMapping.GeneratePixelMappingFromGlediatorPatch(
                Path.Combine(Exec.ExpanderSharedFiles, "Glediator", "ArtNet 14-15 20x10.patch.glediator"));
            dmxPlayback.SetOutput(pixelsMatrix, pixelMapping, 0);

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
            stateMachine.ForFromSubroutine(States.MusicChristmasCanon, subMusicChristmasCanon);
            stateMachine.ForFromSubroutine(States.MusicBelieve, subMusicBelieve);
            stateMachine.ForFromSubroutine(States.MusicSarajevo, subMusicSarajevo);
            stateMachine.ForFromSubroutine(States.MusicHolyNight, subMusicHolyNight);
            stateMachine.ForFromSubroutine(States.MusicCarol, subMusicCarol);
            stateMachine.ForFromSubroutine(States.SantaVideo, subSantaVideo);
            stateMachine.ForFromSubroutine(States.DarthVader, subStarWars);

            hours
                .ControlsMasterPower(laser)
                .ControlsMasterPower(airR2D2Olaf)
                .ControlsMasterPower(airSantaPoppy1)
//                .ControlsMasterPower(airSnowman)
                .ControlsMasterPower(airTree)
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
            acnOutput.Connect(new Physical.Pixel2D(pixelsMatrix, pixelMapping2D), SacnUniversePixelMatrix);

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
            acnOutput.Connect(new Physical.GenericDimmer(airTree, 9), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.GenericDimmer(lightInflatableTree, 10), SacnUniverseRenard18);
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
                movingHead.SetPan(p * 540);
            });
            faderTilt.Output.Subscribe(t =>
            {
                movingHead.SetTilt(t * 270);
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
                    lightInflatableTree.SetBrightness(1);
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

                    saberPixels.SetColor(Color.Red, 0.4, token: i.Token);

                    subRandomSantaVideo.Run();
                    subCandyCane.Run();
                    subHeart.Run();
                    dmxPlayback.Run();

                    i.WaitUntilCancel();
                })
                .TearDown(i =>
                {
                    dmxPlayback.Stop();
                    Exec.Cancel(subHeart);
                    Exec.Cancel(subCandyCane);
                    Exec.Cancel(subRandomSantaVideo);
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
                            pixelsRoofEdge.Inject((x % spacing) == 0 ? Color.Red : Color.White, 0.5, token: i.Token);
                            pixelsGround.Inject((x % spacing) == 0 ? Color.Red : Color.White, 0.5, token: i.Token);

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
                            pixelsHeart.Inject(Color.Red, brightness, token: i.Token);

                            i.WaitFor(S(0.50), true);
                        }
                    }
                });

            subSnow
                .RunAction(ins =>
                {
                    pulsatingPinSpot.SetAdditionalData(lightPinSpot, Utils.Data(Color.White));
                    snowMachine.SetValue(true);

                    ins.WaitFor(S(15));
                })
                .TearDown(i =>
                {
                    pulsatingPinSpot.SetAdditionalData(lightPinSpot, Utils.Data(Color.Red));
                    snowMachine.SetValue(false);
                });

            subStarWarsCane
                .LockWhenRunning(
                    pixelsRoofEdge,
                    pixelsGround,
                    pixelsBetweenTrees,
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
                                    pixelsRoofEdge.InjectRev(Color.Yellow, 1.0, token: instance.Token);
                                    pixelsGround.Inject(Color.Yellow, 1.0, token: instance.Token);
                                    pixelsBetweenTrees.Inject(Color.Yellow, 1.0, token: instance.Token);
                                    pixelsMatrix.InjectRow(Color.Yellow, 1.0, token: instance.Token);
                                    break;
                                case 2:
                                case 3:
                                    pixelsRoofEdge.InjectRev(Color.Orange, 0.2, token: instance.Token);
                                    pixelsGround.Inject(Color.Orange, 0.2, token: instance.Token);
                                    pixelsBetweenTrees.Inject(Color.Orange, 0.2, token: instance.Token);
                                    pixelsMatrix.InjectRow(Color.Orange, 0.2, token: instance.Token);
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

            subMusicChristmasCanon
                .LockWhenRunning(lightSantaPopup, movingHead, lightXmasTree)
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

            subMusicBelieve
                .LockWhenRunning(lightSantaPopup, movingHead, lightXmasTree, hazerFanSpeed, hazerHazeOutput)
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    movingHead.SetColor(Color.Red, 1);
                    lightXmasTree.SetValue(true);
                    hazerFanSpeed.SetBrightness(0.3);
                    hazerHazeOutput.SetBrightness(0.1);
                    audioHiFi.PlayTrack("T.P.E. - 04 - Josh Groban - Believe.flac");
                    ins.WaitFor(S(260));
                }).TearDown(i =>
                {
                    lorBelieve.Stop();
                    audioHiFi.PauseTrack();
                });

            subMusicSarajevo
                .LockWhenRunning(lightSantaPopup, movingHead, lightXmasTree)
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    movingHead.SetColor(Color.Red, 1);
                    lightXmasTree.SetValue(true);
                    audioHiFi.PlayTrack("04 Christmas Eve _ Sarajevo (Instrum.wav");
                    ins.WaitFor(S(200));
                }).TearDown(i =>
                {
                    lorSarajevo.Stop();
                    audioHiFi.PauseTrack();
                });

            subMusicHolyNight
                .LockWhenRunning(lightSantaPopup, movingHead, lightXmasTree)
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    movingHead.SetColor(Color.Red, 1);
                    lightXmasTree.SetValue(true);
                    audioHiFi.PlayTrack("01 O Come All Ye Faithful _ O Holy N.wav");
                    ins.WaitFor(S(260));
                }).TearDown(i =>
                {
                    lorHolyNight.Stop();
                    audioHiFi.PauseTrack();
                });

            subMusicCarol
                .LockWhenRunning(lightSantaPopup, movingHead, lightXmasTree)
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    movingHead.SetColor(Color.Red, 1);
                    lightXmasTree.SetValue(true);
                    audioHiFi.PlayTrack("09 Carol of the Bells (Instrumental).wav");
                    ins.WaitFor(S(160));
                }).TearDown(i =>
                {
                    lorCarol.Stop();
                    audioHiFi.PauseTrack();
                });

            subSantaVideo
                .LockWhenRunning(laser, lightSantaPopup, movingHead, lightXmasTree)
                .RunAction(i =>
                {
                    laser.SetValue(false);
                    lightSantaPopup.SetBrightness(1);
                    movingHead.SetColor(Color.Red, 1);
                    lightXmasTree.SetValue(true);

                    switch (random.Next(3))
                    {
                        case 0:
                            // NBC_WeWishYou_Holl_H 1:22
                            expanderVideo1.SendSerial(0, new byte[] { 4 });
                            Task.Delay(5000).ContinueWith(t => lorJingleBells.Start(duration: S(68)));
                            i.WaitFor(S(80));
                            break;

                        case 1:
                            // NBC_DeckTheHalls_Holl_H 1:31
                            expanderVideo1.SendSerial(0, new byte[] { 5 });
                            Task.Delay(5000).ContinueWith(t => lorJingleBells.Start(duration: S(78)));
                            i.WaitFor(S(90));
                            break;

                        case 2:
                            // NBC_JingleBells_Holl_H 1:31
                            expanderVideo1.SendSerial(0, new byte[] { 6 });
                            Task.Delay(5000).ContinueWith(t => lorJingleBells.Start(duration: S(83)));
                            i.WaitFor(S(90));
                            break;
                    }
                })
                .TearDown(i =>
                {
                    laser.SetValue(true);
                    lorJingleBells.Stop();
                    expanderVideo1.SendSerial(0, new byte[] { 100 });
                });

            subRandomSantaVideo
                .RunAction(i =>
                {
                    while (!i.IsCancellationRequested)
                    {
                        switch (random.Next(4))
                        {
                            case 0:
                                // Nothing
                                i.WaitFor(S(60));
                                break;

                            case 1:
                                expanderVideo1.SendSerial(0, new byte[] { 1 });
                                i.WaitFor(S(70));
                                break;

                            case 2:
                                expanderVideo1.SendSerial(0, new byte[] { 2 });
                                i.WaitFor(S(100));
                                break;

                            case 3:
                                expanderVideo1.SendSerial(0, new byte[] { 3 });
                                i.WaitFor(S(60));
                                break;
                        }
                    }
                }).TearDown(i =>
                {
                    expanderVideo1.SendSerial(0, new byte[] { 100 });
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
                    lightR2D2.SetBrightness(1.0, token: instance.Token);

                    audioHiFi.PlayTrack("01. Star Wars - Main Title.wav");

                    instance.WaitFor(S(16));

                    var haloJob = haloPixels.Chaser(new IData[] {
                        Utils.Data(Color.White, 1.0),
                        Utils.Data(Color.White, 0.7),
                        Utils.Data(Color.White, 0.5),
                        Utils.Data(Color.White, 0.3)
                    }, 4, token: instance.Token);

                    pulsatingStar.Start();

                    Exec.MasterEffect.Fade(lightVader, 0.0, 1.0, 1000, token: instance.Token, additionalData: Utils.Data(Color.Red));
                    instance.WaitFor(S(2.5));

                    Exec.Cancel(subStarWarsCane);
                    instance.WaitFor(S(0.5));

                    audioDarthVader.PlayEffect("saberon.wav");
                    for (int sab = 0; sab < 33; sab++)
                    {
                        saberPixels.Inject(Color.Red, 0.5, token: instance.Token);
                        instance.WaitFor(S(0.01));
                    }
                    instance.WaitFor(S(1));
                    audioHiFi.PauseTrack();

                    lightVader.SetColor(Color.Red, 1.0, token: instance.Token);
                    audioDarthVader.PlayEffect("father.wav");
                    instance.WaitFor(S(5));

                    audioDarthVader.PlayEffect("force1.wav");
                    instance.WaitFor(S(4));

                    lightVader.SetBrightness(0.0, token: instance.Token);

                    audioDarthVader.PlayEffect("saberoff.wav");
                    instance.WaitFor(S(0.7));
                    for (int sab = 0; sab < 17; sab++)
                    {
                        saberPixels.InjectRev(Color.Black, 0, token: instance.Token);
                        saberPixels.InjectRev(Color.Black, 0, token: instance.Token);
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

            controlButtonWhite.WhenOutputChanges(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState != States.DarthVader)
                    subSnow.Run();
            });

            controlButtonYellow.WhenOutputChanges(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    stateMachine.GoToState(States.DarthVader);
            });

            controlButtonBlue.WhenOutputChanges(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    stateMachine.GoToState(States.MusicBelieve);
            });

            controlButtonGreen.WhenOutputChanges(x =>
            {
                if (x && hours.IsOpen && stateMachine.CurrentState == States.Background)
                    stateMachine.GoToState(States.MusicHolyNight);
            });

            controlButtonBlack.WhenOutputChanges(x =>
            {
                if (x)
                {
                    if (hours.IsOpen)
                    {
                        if (stateMachine.CurrentState == States.Background)
                            stateMachine.GoToState(States.MusicSarajevo);
                    }
                    else
                    {
                        audioDarthVader.PlayEffect("force1.wav");
                    }
                }
            });

            controlButtonRed.WhenOutputChanges(x =>
            {
                if (x)
                {
                    if (hours.IsOpen)
                    {
                        if (stateMachine.CurrentState == States.Background)
                            stateMachine.GoToState(States.SantaVideo);
                    }
                    else
                    {
                        audioDarthVader.PlayEffect("darthvader_powerofthedarkside.wav");
                    }
                }
            });

            controlButtonRed.IsHeld.Subscribe(x =>
            {
                if (x)
                    this.stateMachine.GoToDefaultState();
            });

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

                    case "04 Christmas Eve _ Sarajevo (Instrum.wav":
                        lorSarajevo.Start();
                        break;

                    case "01 O Come All Ye Faithful _ O Holy N.wav":
                        lorHolyNight.Start();
                        break;

                    case "09 Carol of the Bells (Instrumental).wav":
                        lorCarol.Start();
                        break;
                }
            };

            inShowMachine.Output.Subscribe(x =>
            {
                snowMachine.SetValue(x);
            });

            ImportAndMapChristmasCanon();
            ImportAndMapBelieve();
            ImportAndMapJingleBells();
            ImportAndMapSarajevo();
            ImportAndMapHolyNight();
            ImportAndMapCarol();

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
            movingHead.SetColor(GetFaderColor(), faderBright.Value);
            //}
        }

        private void ImportAndMapChristmasCanon()
        {
            lorChristmasCanon.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Cannon Rock104.lms"));

            lorChristmasCanon.Progress.Subscribe(x =>
            {
                this.log.Verbose("Christmas Canon {0:N0} ms", x);
            });

            //            lorChristmasCanon.Dump();

            lorChristmasCanon.MapDevice("Roof 1", pixelsGround, Utils.Data(Color.Red));
            lorChristmasCanon.MapDevice("Roof 2", pixelsTree, Utils.Data(Color.Green));
            lorChristmasCanon.MapDevice("Roof 3", pixelsHeart, Utils.Data(Color.Red));

            lorChristmasCanon.ControlDevice(pixelsBetweenTrees);
            lorChristmasCanon.MapDevice("Big Tree 1",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 6, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Big Tree 2",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 6, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Big Tree 3",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 6, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Big Tree 4",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 6, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Big Tree 5",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 6, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Big Tree 6",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 6, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Big Tree 7",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 6, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Big Tree 8",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 6, token: lorChristmasCanon.Token)));

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
            lorChristmasCanon.MapDevice("Column Red Right", lightFlood1, Utils.Data(Color.Red));
            lorChristmasCanon.MapDevice("Column Blue Right", lightFlood2, Utils.Data(Color.Blue));
            lorChristmasCanon.MapDevice("Column Red Right", lightFlood5, Utils.Data(Color.Red));
            lorChristmasCanon.MapDevice("Column Blue Right", lightFlood6, Utils.Data(Color.Blue));
            lorChristmasCanon.MapDevice("Rail Right", lightStairRail1);
            lorChristmasCanon.MapDevice("Rail Right", lightInflatableTree);
            lorChristmasCanon.MapDevice("Column Red Left", lightFlood3, Utils.Data(Color.Red));
            lorChristmasCanon.MapDevice("Column Blue Left", lightFlood4, Utils.Data(Color.Blue));
            lorChristmasCanon.MapDevice("Column Blue Left", lightFlood7, Utils.Data(Color.Blue));
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
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 0, 3, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 2",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 3, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 3",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 4, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 4",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 5, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 5",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 6, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 6",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 7, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 7",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 8, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 8",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 9, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 01",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 10, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 02",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 11, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 03",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 12, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 04",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 13, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 05",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 14, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 06",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 15, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 07",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 16, 0, 1, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Tree 08",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 17, 0, 3, 10, token: lorChristmasCanon.Token)));

            lorChristmasCanon.ControlDevice(pixelsRoofEdge);
            lorChristmasCanon.MapDevice("Arch 1",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 0, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 2",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 10, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 3",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 20, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 4",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 30, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 5",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 40, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 6",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 50, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 7",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 60, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 8",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 70, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 9",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 80, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 10",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 90, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 11",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 100, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 12",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 110, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 13",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 120, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 14",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 130, 10, token: lorChristmasCanon.Token)));
            lorChristmasCanon.MapDevice("Arch 15",
                new VirtualDevice(b => pixelsRoofEdge.SetColorRange(Color.Red, b, 140, 10, token: lorChristmasCanon.Token)));

            lorChristmasCanon.Prepare();
        }

        private void ImportAndMapBelieve()
        {
            lorBelieve.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Believe - Josh Groban 64 chns.lms"));

            lorBelieve.Progress.Subscribe(x =>
            {
                this.log.Verbose("Believe {0:N0} ms", x);
            });

            //lorBelieve.Dump();

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
            lorBelieve.MapDevice("Ferris Wheel 8", lightInflatableTree);

            lorBelieve.MapDevice("NATIVITY", lightHangingStar);

            lorBelieve.ControlDevice(pixelsMatrix);
            lorBelieve.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 0, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 1, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 2, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 3, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 4, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 5, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 6, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 7, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 8, 20, 1, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 9, 20, 1, token: lorBelieve.Token)));

            lorBelieve.ControlDevice(pixelsBetweenTrees);
            lorBelieve.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 3, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 9, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 15, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 21, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 27, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 11",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 12",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 33, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 13",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 14",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 39, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 15",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, token: lorBelieve.Token)));
            lorBelieve.MapDevice("Mega Tree 16",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 45, 3, token: lorBelieve.Token)));

            lorBelieve.MapDevice("Mega Star", pixelsRoofEdge, Utils.Data(Color.Red));
            lorBelieve.MapDevice("Mega Star", pixelsGround, Utils.Data(Color.White));
            lorBelieve.MapDevice("Mega Star", pixelsTree, Utils.Data(Color.Red));
            lorBelieve.MapDevice("Mega Star", pixelsHeart, Utils.Data(Color.Red));

            lorBelieve.Prepare();
        }

        private void ImportAndMapJingleBells()
        {
            lorJingleBells.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Jingle Bell Rock, Randy Travis.lms"));

            lorJingleBells.Progress.Subscribe(x =>
            {
                this.log.Verbose("Jingle Bells {0:N0} ms", x);
            });

            //            lorJingleBells.Dump();

            lorJingleBells.MapDevice("Unit 01.1 arch 1.1", lightNet1);
            lorJingleBells.MapDevice("Unit 01.2 arch 1.2", lightNet2);
            lorJingleBells.MapDevice("Unit 01.3 arch 1.3", lightNet3);
            lorJingleBells.MapDevice("Unit 01.4 arch 1.4", lightNet4);
            lorJingleBells.MapDevice("Unit 01.5 arch 1.5", lightNet5);
            lorJingleBells.MapDevice("Unit 01.6 arch 1.6", lightNet6);
            lorJingleBells.MapDevice("Unit 01.7 arch 1.7", lightNet7);
            lorJingleBells.MapDevice("Unit 01.8 arch 1.8", lightNet8);
            lorJingleBells.MapDevice("Unit 01.9 arch 2.1", lightNet9);
            lorJingleBells.MapDevice("Unit 01.10 arch 2.2", lightNet10);
            lorJingleBells.MapDevice("Unit 01.10 arch 2.2", lightInflatableTree);
            lorJingleBells.MapDevice("Unit 01.11 arch 2.3", lightHat1);
            lorJingleBells.MapDevice("Unit 01.12 arch 2.4", lightHat2);
            lorJingleBells.MapDevice("Unit 01.13arch 2.5", lightHat3);
            lorJingleBells.MapDevice("Unit 01.14 arch 2.6", lightHat4);
            lorJingleBells.MapDevice("Unit 0115 arch 2.7", lightTopper1);
            lorJingleBells.MapDevice("Unit 01.16 arch 2.8", lightTopper2);

            lorJingleBells.MapDevice("03.13 deer rudolf", lightFlood1, Utils.Data(Color.Red));
            lorJingleBells.MapDevice("03.13 deer rudolf", lightFlood2, Utils.Data(Color.Red));
            lorJingleBells.MapDevice("03.13 deer rudolf", lightFlood3, Utils.Data(Color.Red));
            lorJingleBells.MapDevice("03.13 deer rudolf", lightFlood4, Utils.Data(Color.Red));
            lorJingleBells.MapDevice("03.13 deer rudolf", lightFlood5, Utils.Data(Color.Red));
            lorJingleBells.MapDevice("03.13 deer rudolf", lightFlood6, Utils.Data(Color.Red));
            lorJingleBells.MapDevice("03.13 deer rudolf", lightFlood7, Utils.Data(Color.Red));

            lorJingleBells.ControlDevice(pixelsMatrix);
            lorJingleBells.MapDevice("Unit 02.1 Mega tree 1",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 0, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.2 Mega tree 2",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 1, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.3 Mege tree 3",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 2, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.4 Mega tree 4",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 3, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.5 Mega tree 5",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 4, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.6 Mega tree 6",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 5, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.7 Mega tree 7",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 6, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.8 Mega tree 8",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 7, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.9 Mega tree 9",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 8, 20, 1, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.10 Mega tree 10",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 9, 20, 1, token: lorJingleBells.Token)));

            lorJingleBells.ControlDevice(pixelsBetweenTrees);
            lorJingleBells.MapDevice("Unit 02.1 Mega tree 1",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.2 Mega tree 2",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Blue, b, 3, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.3 Mege tree 3",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.4 Mega tree 4",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Blue, b, 9, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.5 Mega tree 5",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.6 Mega tree 6",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Blue, b, 15, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.7 Mega tree 7",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.8 Mega tree 8",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Blue, b, 21, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.9 Mega tree 9",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.10 Mega tree 10",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Blue, b, 27, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.11 Mega tree 11",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.12 Mega tree 12",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Blue, b, 33, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.13 Mega tree 13",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.14 Mega tree 14",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Blue, b, 39, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.15 Mega tree 15",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, token: lorJingleBells.Token)));
            lorJingleBells.MapDevice("Unit 02.16 Mega tree 16",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Blue, b, 45, 3, token: lorJingleBells.Token)));

            lorJingleBells.MapDevice("03.1 mega tree topper 01", pixelsRoofEdge, Utils.Data(Color.Red));
            lorJingleBells.MapDevice("03.15 candy cane lane", pixelsGround, Utils.Data(Color.Yellow));
            lorJingleBells.MapDevice("03.15 candy cane lane", pixelsTree, Utils.Data(Color.Green));
            lorJingleBells.MapDevice("03.15 candy cane lane", pixelsHeart, Utils.Data(Color.Blue));

            lorJingleBells.MapDevice("03.1 mega tree topper 01", lightHangingStar);
            lorJingleBells.MapDevice("03.2 mini tree 01", lightStairRail1);
            lorJingleBells.MapDevice("03.3 mini tree 02", lightStairRail2);
            lorJingleBells.MapDevice("03.4 mini tree 03", lightRail1);
            lorJingleBells.MapDevice("03.5 mini tree 04", lightRail2);
            lorJingleBells.MapDevice("03.6 mini tree 05", lightRail3);
            lorJingleBells.MapDevice("03.7 mini tree 06", lightRail4);
            lorJingleBells.MapDevice("03.8 mini tree 07", lightStairs1);
            lorJingleBells.MapDevice("03.9 mini tree 08", lightStairs2);
            lorJingleBells.MapDevice("03.8 mini tree 07", lightStairs3);

            lorJingleBells.MapDevice("03.10 house eve 01", lightSanta);
            lorJingleBells.MapDevice("03.11 house eve 02", lightPoppy);
            lorJingleBells.MapDevice("03.12 house eve 03", lightSnowman);
            lorJingleBells.MapDevice("03.14 deer 02", lightReindeers);
            lorJingleBells.MapDevice("03.14 deer 02", lightOlaf);
            lorJingleBells.MapDevice("03.14 deer 02", lightReindeerBig);
            lorJingleBells.MapDevice("03.14 deer 02", lightR2D2);
            lorJingleBells.MapDevice("01.16 mega tree topper 02", lightTreeStars);

            lorJingleBells.Prepare();
        }

        private void ImportAndMapSarajevo()
        {
            lorSarajevo.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Christmas Eve (Sarajevo) 64 done 4.lms"));

            lorSarajevo.Progress.Subscribe(x =>
            {
                this.log.Verbose("Sarajevo {0:N0} ms", x);
            });

            // Lights:
            //lightNet1
            //lightNet2
            //lightNet3
            //lightNet4
            //lightNet5
            //lightNet6
            //lightNet7
            //lightNet8
            //lightNet9
            //lightNet10
            //lightTopper1
            //lightTopper2
            //lightInflatableTree
            //lightHangingStar
            //lightStairRail1
            //lightStairRail2
            //lightRail1
            //lightRail2
            //lightRail3
            //lightRail4
            //lightStairs1
            //lightStairs2
            //lightStairs3
            //lightTreeStars
            //lightSanta
            //lightPoppy
            //lightSnowman
            //lightSantaPopup
            //movingHead
            //lightHat1
            //lightHat2
            //lightHat3
            //lightHat4
            //lightReindeers
            //lightReindeerBig
            //lightFlood1
            //lightFlood2
            //lightFlood3
            //lightFlood4
            //lightFlood5
            //lightFlood6
            //lightFlood7
            //lightOlaf
            //lightR2D2


            // Channels:
            //House 2
            //Whole Yard
            //Floods G
            //Yard 2
            //Floods R
            //Mega Tree 2
            //Mega Tree 1
            //Mega Star
            //Yard 10
            //Mega Tree 12
            //Mega Tree 11
            //House 1
            //Yard 9
            //Mega Tree 13
            //Yard 4
            //Yard 6
            //Wreath R
            //Wreath W
            //Mega Tree 10
            //Mega Tree 9
            //Mega Tree 8
            //Mega Tree 16
            //Mega Tree 15
            //Mega Tree 14
            //Yard 7
            //Yard 3
            //Wreath R (1)
            //Ferris Wheel 4
            //House 3
            //Mega Tree 7
            //Mega Tree 6
            //Mega Tree 5
            //Mega Tree 4
            //Mega Tree 3
            //Wreath W(1)
            //Whole Yard(1)
            //Ferris Wheel 3
            //Yard 8
            //Ferris Wheel 2
            //Ferris Wheel 1
            //Ferris Wheel 7
            //Yard 5
            //Floods W
            //Yard 1
            //Ferris Wheel 6
            //Ferris Wheel 5
            //Floods B
            //Ferris Wheel 8
            //Strobes
            //NATIVITY
            //Blue Flake 1
            //Blue Flake 2
            //Blue Flake 3
            //Blue Flake 4
            //Blue Flake 5
            //Blue Flake 6
            //White Flake 1
            //White Flake 2
            //White Flake 3
            //White Flake 4
            //White Flake 5
            //White Flake 6
            //Eiffel Tower
            //Spotlight

            //lorSarajevo.Dump();

            lorSarajevo.MapDevice("Yard 1", lightNet1);
            lorSarajevo.MapDevice("Yard 2", lightNet2);
            lorSarajevo.MapDevice("Yard 3", lightNet3);
            lorSarajevo.MapDevice("Yard 4", lightNet4);
            lorSarajevo.MapDevice("Yard 5", lightNet5);
            lorSarajevo.MapDevice("Yard 6", lightNet6);
            lorSarajevo.MapDevice("Yard 7", lightNet7);
            lorSarajevo.MapDevice("Yard 8", lightNet8);
            lorSarajevo.MapDevice("Yard 9", lightNet9);
            lorSarajevo.MapDevice("Yard 10", lightNet10);
            lorSarajevo.MapDevice("Yard 5", lightHat1);
            lorSarajevo.MapDevice("Yard 6", lightHat2);
            lorSarajevo.MapDevice("Yard 7", lightHat3);
            lorSarajevo.MapDevice("Yard 8", lightHat4);

            lorSarajevo.MapDevice("Yard 9", lightTreeStars);
            lorSarajevo.MapDevice("Yard 10", lightReindeerBig);

            lorSarajevo.MapDevice("House 1", lightR2D2);
            lorSarajevo.MapDevice("House 2", lightOlaf);
            lorSarajevo.MapDevice("House 3", lightPoppy);

            lorSarajevo.MapDevice("Wreath W", lightStairs1);
            lorSarajevo.MapDevice("Wreath R", lightStairs2);
            lorSarajevo.MapDevice("Wreath W", lightStairs3);
            lorSarajevo.MapDevice("Wreath W", lightStairRail1);
            lorSarajevo.MapDevice("Wreath R", lightStairRail2);

            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood1);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood2);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood3);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood4);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood5);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood6);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood7);

            lorSarajevo.MapDevice("Ferris Wheel 1", lightTopper1);
            lorSarajevo.MapDevice("Ferris Wheel 2", lightTopper2);
            lorSarajevo.MapDevice("Ferris Wheel 3", lightRail1);
            lorSarajevo.MapDevice("Ferris Wheel 4", lightRail2);
            lorSarajevo.MapDevice("Ferris Wheel 5", lightReindeers);
            lorSarajevo.MapDevice("Ferris Wheel 5", lightRail3);
            lorSarajevo.MapDevice("Ferris Wheel 6", lightRail4);
            lorSarajevo.MapDevice("Ferris Wheel 7", lightSanta);
            lorSarajevo.MapDevice("Ferris Wheel 8", lightSnowman);
            lorSarajevo.MapDevice("Ferris Wheel 8", lightInflatableTree);

            lorSarajevo.MapDevice("NATIVITY", lightHangingStar);

            lorSarajevo.ControlDevice(pixelsMatrix);
            lorSarajevo.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 0, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 1, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 2, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 3, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 4, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 5, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 6, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 7, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 8, 20, 1, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 9, 20, 1, token: lorSarajevo.Token)));

            lorSarajevo.ControlDevice(pixelsBetweenTrees);
            lorSarajevo.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 3, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 9, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 15, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 21, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 27, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 11",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 12",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 33, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 13",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 14",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 39, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 15",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, token: lorSarajevo.Token)));
            lorSarajevo.MapDevice("Mega Tree 16",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 45, 3, token: lorSarajevo.Token)));

            lorSarajevo.MapDevice("Mega Star", pixelsRoofEdge, Utils.Data(Color.White));
            lorSarajevo.MapDevice("Mega Star", pixelsGround, Utils.Data(Color.White));
            lorSarajevo.MapDevice("Mega Star", pixelsTree, Utils.Data(Color.White));
            lorSarajevo.MapDevice("Mega Star", pixelsHeart, Utils.Data(Color.White));

            lorSarajevo.Prepare();

            //lorSarajevo.ListUnmappedChannels();
        }

        private void ImportAndMapHolyNight()
        {
            lorHolyNight.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Oh Come All Ye Faithful Oh Holy Night - TSO 64 done 1.lms"));

            lorHolyNight.Progress.Subscribe(x =>
            {
                this.log.Verbose("HolyNight {0:N0} ms", x);
            });

            // Lights:
            //lightNet1
            //lightNet2
            //lightNet3
            //lightNet4
            //lightNet5
            //lightNet6
            //lightNet7
            //lightNet8
            //lightNet9
            //lightNet10
            //lightTopper1
            //lightTopper2
            //lightInflatableTree
            //lightHangingStar
            //lightStairRail1
            //lightStairRail2
            //lightRail1
            //lightRail2
            //lightRail3
            //lightRail4
            //lightStairs1
            //lightStairs2
            //lightStairs3
            //lightTreeStars
            //lightSanta
            //lightPoppy
            //lightSnowman
            //lightSantaPopup
            //movingHead
            //lightHat1
            //lightHat2
            //lightHat3
            //lightHat4
            //lightReindeers
            //lightReindeerBig
            //lightFlood1
            //lightFlood2
            //lightFlood3
            //lightFlood4
            //lightFlood5
            //lightFlood6
            //lightFlood7
            //lightOlaf
            //lightR2D2


            // Channels:
            //Whole Yard
            //Whole Yard (1)
            //House 1
            //House 2
            //House 3
            //Yard 1
            //Yard 2
            //Yard 3
            //Yard 4
            //Yard 5
            //Yard 7
            //Yard 8
            //Yard 10
            //Mega Star
            //Mega Tree 1
            //Yard 6
            //Yard 9
            //Mega Tree 2
            //Mega Tree 3
            //Mega Tree 4
            //Mega Tree 5
            //Mega Tree 6
            //Mega Tree 7
            //Mega Tree 8
            //Mega Tree 9
            //Mega Tree 10
            //Mega Tree 11
            //Mega Tree 12
            //Mega Tree 13
            //Mega Tree 14
            //Mega Tree 15
            //Mega Tree 16
            //Wreath W
            //Wreath R
            //Wreath W(1)
            //Wreath R(1)
            //Ferris Wheel 1
            //Ferris Wheel 2
            //Ferris Wheel 3
            //Ferris Wheel 4
            //Ferris Wheel 5
            //Ferris Wheel 6
            //Ferris Wheel 7
            //Ferris Wheel 8
            //Floods B
            //Floods G
            //Floods R
            //Floods W
            //Strobes
            //NATIVITY
            //Street Bush 1
            //Street Bush 2
            //Street Bush 3
            //Street Bush 4
            //Street Bush 5
            //Street Bush 6
            //Street Bush 1(1)
            //Street Bush 2(1)
            //Street Bush 3(1)
            //Street Bush 4(1)
            //Street Bush 5(1)
            //Street Bush 6(1)
            //Other
            //Other(1)

            //lorHolyNight.Dump();

            lorHolyNight.MapDevice("Yard 1", lightNet1);
            lorHolyNight.MapDevice("Yard 2", lightNet2);
            lorHolyNight.MapDevice("Yard 3", lightNet3);
            lorHolyNight.MapDevice("Yard 4", lightNet4);
            lorHolyNight.MapDevice("Yard 5", lightNet5);
            lorHolyNight.MapDevice("Yard 6", lightNet6);
            lorHolyNight.MapDevice("Yard 7", lightNet7);
            lorHolyNight.MapDevice("Yard 8", lightNet8);
            lorHolyNight.MapDevice("Yard 9", lightNet9);
            lorHolyNight.MapDevice("Yard 10", lightNet10);
            lorHolyNight.MapDevice("Yard 5", lightHat1);
            lorHolyNight.MapDevice("Yard 6", lightHat2);
            lorHolyNight.MapDevice("Yard 7", lightHat3);
            lorHolyNight.MapDevice("Yard 8", lightHat4);

            lorHolyNight.MapDevice("Yard 9", lightTreeStars);
            lorHolyNight.MapDevice("Yard 10", lightReindeerBig);

            lorHolyNight.MapDevice("House 1", lightR2D2);
            lorHolyNight.MapDevice("House 2", lightOlaf);
            lorHolyNight.MapDevice("House 3", lightPoppy);

            lorHolyNight.MapDevice("Wreath W", lightStairs1);
            lorHolyNight.MapDevice("Wreath R", lightStairs2);
            lorHolyNight.MapDevice("Wreath W", lightStairs3);
            lorHolyNight.MapDevice("Wreath W", lightStairRail1);
            lorHolyNight.MapDevice("Wreath R", lightStairRail2);

            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood1);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood2);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood3);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood4);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood5);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood6);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood7);

            lorHolyNight.MapDevice("Ferris Wheel 1", lightTopper1);
            lorHolyNight.MapDevice("Ferris Wheel 2", lightTopper2);
            lorHolyNight.MapDevice("Ferris Wheel 3", lightRail1);
            lorHolyNight.MapDevice("Ferris Wheel 4", lightRail2);
            lorHolyNight.MapDevice("Ferris Wheel 5", lightReindeers);
            lorHolyNight.MapDevice("Ferris Wheel 5", lightRail3);
            lorHolyNight.MapDevice("Ferris Wheel 6", lightRail4);
            lorHolyNight.MapDevice("Ferris Wheel 7", lightSanta);
            lorHolyNight.MapDevice("Ferris Wheel 8", lightSnowman);
            lorHolyNight.MapDevice("Ferris Wheel 8", lightInflatableTree);

            lorHolyNight.MapDevice("NATIVITY", lightHangingStar);

            lorHolyNight.ControlDevice(pixelsMatrix);
            lorHolyNight.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 0, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 1, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 2, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 3, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 4, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 5, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 6, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 7, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 8, 20, 1, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 9, 20, 1, token: lorHolyNight.Token)));

            lorHolyNight.ControlDevice(pixelsBetweenTrees);
            lorHolyNight.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 3, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 9, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 15, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 21, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 27, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 11",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 12",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 33, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 13",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 14",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 39, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 15",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, token: lorHolyNight.Token)));
            lorHolyNight.MapDevice("Mega Tree 16",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 45, 3, token: lorHolyNight.Token)));

            lorHolyNight.MapDevice("Mega Star", pixelsRoofEdge, Utils.Data(Color.Red));
            lorHolyNight.MapDevice("Mega Star", pixelsGround, Utils.Data(Color.White));
            lorHolyNight.MapDevice("Mega Star", pixelsTree, Utils.Data(Color.Red));
            lorHolyNight.MapDevice("Mega Star", pixelsHeart, Utils.Data(Color.Red));

            lorHolyNight.Prepare();

            //lorHolyNight.ListUnmappedChannels();
        }

        private void ImportAndMapCarol()
        {
            lorCarol.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "David Foster - Carol of the Bells.lms"));

            lorCarol.Progress.Subscribe(x =>
            {
                this.log.Verbose("Carol {0:N0} ms", x);
            });

            // Lights:
            //lightNet1
            //lightNet2
            //lightNet3
            //lightNet4
            //lightNet5
            //lightNet6
            //lightNet7
            //lightNet8
            //lightNet9
            //lightNet10
            //lightTopper1
            //lightTopper2
            //lightInflatableTree
            //lightHangingStar
            //lightStairRail1
            //lightStairRail2
            //lightRail1
            //lightRail2
            //lightRail3
            //lightRail4
            //lightStairs1
            //lightStairs2
            //lightStairs3
            //lightTreeStars
            //lightSanta
            //lightPoppy
            //lightSnowman
            //lightSantaPopup
            //movingHead
            //lightHat1
            //lightHat2
            //lightHat3
            //lightHat4
            //lightReindeers
            //lightReindeerBig
            //lightFlood1
            //lightFlood2
            //lightFlood3
            //lightFlood4
            //lightFlood5
            //lightFlood6
            //lightFlood7
            //lightOlaf
            //lightR2D2


            // Channels:

            lorCarol.Dump();
/*
            lorCarol.MapDevice("Yard 1", lightNet1);
            lorCarol.MapDevice("Yard 2", lightNet2);
            lorCarol.MapDevice("Yard 3", lightNet3);
            lorCarol.MapDevice("Yard 4", lightNet4);
            lorCarol.MapDevice("Yard 5", lightNet5);
            lorCarol.MapDevice("Yard 6", lightNet6);
            lorCarol.MapDevice("Yard 7", lightNet7);
            lorCarol.MapDevice("Yard 8", lightNet8);
            lorCarol.MapDevice("Yard 9", lightNet9);
            lorCarol.MapDevice("Yard 10", lightNet10);
            lorCarol.MapDevice("Yard 5", lightHat1);
            lorCarol.MapDevice("Yard 6", lightHat2);
            lorCarol.MapDevice("Yard 7", lightHat3);
            lorCarol.MapDevice("Yard 8", lightHat4);

            lorCarol.MapDevice("Yard 9", lightTreeStars);
            lorCarol.MapDevice("Yard 10", lightReindeerBig);

            lorCarol.MapDevice("House 1", lightR2D2);
            lorCarol.MapDevice("House 2", lightOlaf);
            lorCarol.MapDevice("House 3", lightPoppy);

            lorCarol.MapDevice("Wreath W", lightStairs1);
            lorCarol.MapDevice("Wreath R", lightStairs2);
            lorCarol.MapDevice("Wreath W", lightStairs3);
            lorCarol.MapDevice("Wreath W", lightStairRail1);
            lorCarol.MapDevice("Wreath R", lightStairRail2);

            lorCarol.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood1);
            lorCarol.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood2);
            lorCarol.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood3);
            lorCarol.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood4);
            lorCarol.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood5);
            lorCarol.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood6);
            lorCarol.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood7);

            lorCarol.MapDevice("Ferris Wheel 1", lightTopper1);
            lorCarol.MapDevice("Ferris Wheel 2", lightTopper2);
            lorCarol.MapDevice("Ferris Wheel 3", lightRail1);
            lorCarol.MapDevice("Ferris Wheel 4", lightRail2);
            lorCarol.MapDevice("Ferris Wheel 5", lightReindeers);
            lorCarol.MapDevice("Ferris Wheel 5", lightRail3);
            lorCarol.MapDevice("Ferris Wheel 6", lightRail4);
            lorCarol.MapDevice("Ferris Wheel 7", lightSanta);
            lorCarol.MapDevice("Ferris Wheel 8", lightSnowman);
            lorCarol.MapDevice("Ferris Wheel 8", lightInflatableTree);

            lorCarol.MapDevice("NATIVITY", lightHangingStar);

            lorCarol.ControlDevice(pixelsMatrix);
            lorCarol.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 0, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 1, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 2, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 3, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 4, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 5, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 6, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.White, b, 0, 7, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Blue, b, 0, 8, 20, 1, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsMatrix.SetColorRange(Color.Red, b, 0, 9, 20, 1, lorCarol.Token)));

            lorCarol.ControlDevice(pixelsBetweenTrees);
            lorCarol.MapDevice("Mega Tree 1",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 2",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 3, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 3",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 4",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 9, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 5",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 6",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 15, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 7",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 8",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 21, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 9",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 10",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 27, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 11",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 12",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 33, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 13",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 14",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 39, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 15",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, lorCarol.Token)));
            lorCarol.MapDevice("Mega Tree 16",
                new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 45, 3, lorCarol.Token)));

            lorCarol.MapDevice("Mega Star", pixelsRoofEdge, Utils.AdditionalData(Color.Red));
            lorCarol.MapDevice("Mega Star", pixelsGround, Utils.AdditionalData(Color.White));
            lorCarol.MapDevice("Mega Star", pixelsTree, Utils.AdditionalData(Color.Red));
            lorCarol.MapDevice("Mega Star", pixelsHeart, Utils.AdditionalData(Color.Red));
*/
            lorCarol.Prepare();

            lorCarol.ListUnmappedChannels();
        }

        public override void Run()
        {
            // Read from storage
            inflatablesRunning.OnNext(Exec.GetSetKey("InflatablesRunning", false));

            //airR2D2Olaf.SetValue(false);
            //airSantaPoppy1.SetValue(false);
            airSnowman.SetValue(false);
            //airTree.SetValue(false);
            //airSantaPopup.SetValue(false);
        }

        public override void Stop()
        {
            audioHiFi.PauseBackground();
        }
    }
}
