using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Expander = Animatroller.Framework.Expander;
using Import = Animatroller.Framework.Import;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Scenes
{
    internal partial class Xmas2018 : BaseScene
    {
        //const int SacnUniverseEdmx4a = 20;
        //const int SacnUniverseEdmx4b = 21;
        //const int SacnUniverseEdmx4c = 22;
        //const int SacnUniverseEdmx4d = 23;
        const int SacnUniverseLedmx = 10;
        const int SacnUniverseRenard24 = 18;
        const int SacnUniverseRenard2x8 = 19;        // 2 x 8-channels, 1-16
        const int SacnUniverseLED100 = 5;
        const int SacnUniverseLED50 = 6;
        const int SacnUniversePixelMatrixStart = 40;
        const int SacnUniversePixelSaber = 31;

        const int midiChannel = 0;

        public enum States
        {
            Background,
            MusicChristmasCanon,
            MusicBelieve,
            DarthVader,
            MusicSarajevo,
            MusicHolyNight,
            MusicCarol,
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

        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderLedmx = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderHiFi = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderPoppy = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderDarth = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderControlPanel = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderInflatableTree = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
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

        Expander.AcnStream acnOutput = new Expander.AcnStream(defaultPriority: 150);
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
        DigitalInput2 inSnowMachine = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 inSetupMode = new DigitalInput2(persistState: true);

        DigitalOutput2 airR2D2Olaf = new DigitalOutput2();
        DigitalOutput2 airSantaPoppyEtc = new DigitalOutput2();
        DigitalOutput2 airSnowmanSanta = new DigitalOutput2();
        DigitalOutput2 airInflatableTree = new DigitalOutput2();
        DigitalOutput2 airSantaPopup = new DigitalOutput2();
        DigitalOutput2 airReindeerBig = new DigitalOutput2();
        DigitalOutput2 snowMachine = new DigitalOutput2();
        Dimmer3 hazerFanSpeed = new Dimmer3();
        Dimmer3 hazerHazeOutput = new Dimmer3();
        Dimmer3 lightInflatableTree = new Dimmer3();

        Dimmer3 lightHangingStar = new Dimmer3();
        //Dimmer3 lightHangingStar2 = new Dimmer3();
        //Dimmer3 lightHangingStar3 = new Dimmer3();
        //DigitalOutput2 lightXmasTree = new DigitalOutput2();
        DigitalOutput2 lightPackages = new DigitalOutput2();
        Dimmer3 lightStairRail1 = new Dimmer3();
        Dimmer3 lightStairRail2 = new Dimmer3();
        Dimmer3 lightStairPath1 = new Dimmer3();
        Dimmer3 lightStairPath2 = new Dimmer3();
        Dimmer3 lightStairPath3 = new Dimmer3();
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
        Dimmer3 lightNet11 = new Dimmer3();
        Dimmer3 lightRail1 = new Dimmer3();
        Dimmer3 lightRail2 = new Dimmer3();
        Dimmer3 lightRail3 = new Dimmer3();
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
        VirtualPixel1D3 vaderEyesPixels = new VirtualPixel1D3(2);
        Expander.MidiInput2 midiAkai = new Expander.MidiInput2("LPD8", true);
        Expander.OscServer oscServer = new Expander.OscServer(8000);
        Subject<bool> inflatablesRunning = new Subject<bool>();
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();
        DigitalInput2 buttonStartInflatables = new DigitalInput2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonTest = new DigitalInput2();
        DigitalInput2 buttonReset = new DigitalInput2();

        Import.LorImport2 lorChristmasCanon = new Import.LorImport2();
        Import.LorImport2 lorBelieve = new Import.LorImport2();
        Import.LorImport2 lorJingleBells = new Import.LorImport2();
        Import.LorImport2 lorSarajevo = new Import.LorImport2();
        Import.LorImport2 lorHolyNight = new Import.LorImport2();
        Import.LorImport2 lorCarol = new Import.LorImport2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        Controller.Subroutine subCandyCane = new Controller.Subroutine();
        Controller.Subroutine subStarWarsCane = new Controller.Subroutine();
        Controller.Subroutine subBackground = new Controller.Subroutine();
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
        Controller.Subroutine subInflatableTree = new Controller.Subroutine();

        Import.DmxPlayback dmxPlayback = new Import.DmxPlayback();

        public Xmas2018(IEnumerable<string> args)
        {
            mainSchedule.AddRange("4:00 pm", "10:00 pm");
            interactiveSchedule.AddRange("4:00 pm", "8:00 pm");

            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                {
                    Exec.ExpanderSharedFiles = parts[1];
                }
            }

            expanderControlPanel.InvertedInputs[0] = true;
            expanderControlPanel.InvertedInputs[1] = true;
            expanderControlPanel.InvertedInputs[2] = true;
            expanderControlPanel.InvertedInputs[3] = true;
            expanderControlPanel.InvertedInputs[4] = true;
            expanderControlPanel.InvertedInputs[5] = true;
            expanderControlPanel.InvertedInputs[6] = true;
            expanderControlPanel.InvertedInputs[7] = true;

            pulsatingStar.ConnectTo(lightHangingStar);
            //pulsatingStar.ConnectTo(lightHangingStar2);
            //pulsatingStar.ConnectTo(lightHangingStar3);
            pulsatingEffectGeneral.ConnectTo(lightHangingStar);
            //pulsatingEffectGeneral.ConnectTo(lightHangingStar2);
            //pulsatingEffectGeneral.ConnectTo(lightHangingStar3);
            pulsatingEffectGeneral.ConnectTo(lightOlaf);
            pulsatingEffectGeneral.ConnectTo(lightR2D2);
            pulsatingEffectGeneral.ConnectTo(lightPoppy);
            pulsatingEffectGeneral.ConnectTo(lightInflatableTree);
            pulsatingEffectGeneral.ConnectTo(lightVader, Utils.Data(Color.Red));

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expanderLedmx);          // rpi-eb0092ca
            expanderServer.AddInstance("d6fc4e752af04022bf3c1a1166a557bb", expanderHiFi);           // rpi-eb428ef1
            expanderServer.AddInstance("acbfada45c674077b9154f6a0e0df359", expanderPoppy);         // rpi-eba6cbc7
            expanderServer.AddInstance("992f8db68e874248b5ee667d23d74ac3", expanderDarth);           // rpi-ebd43a38
            expanderServer.AddInstance("e41d2977931d4887a9417e8adcd87306", expanderControlPanel);   // rpi-eb6a047c
            expanderServer.AddInstance("1583f686014345888c15d7fc9c55ca3c", expanderInflatableTree);    // 

            //expanderInflatableTree.DigitalInputs[4].Connect(inInflatableTree);
            //expanderLedmx.DigitalInputs[6].Connect(inOlaf);
            expanderLedmx.DigitalInputs[5].Connect(inOlaf);
            expanderLedmx.DigitalInputs[4].Connect(inR2D2);
            //expanderLedmx.DigitalOutputs[1].Connect(snowMachine);

            expanderControlPanel.DigitalInputs[0].Connect(controlButtonWhite);
            expanderControlPanel.DigitalInputs[1].Connect(controlButtonYellow);
            expanderControlPanel.DigitalInputs[2].Connect(controlButtonBlue);
            expanderControlPanel.DigitalInputs[3].Connect(controlButtonGreen);
            expanderControlPanel.DigitalInputs[4].Connect(controlButtonBlack);
            expanderControlPanel.DigitalInputs[5].Connect(controlButtonRed);

            //expanderLedmx.Connect(audioPoppy);
            expanderHiFi.Connect(audioHiFi);
            expanderDarth.Connect(audioDarthVader);
            expanderLedmx.Connect(audioR2D2Olaf);
            expanderInflatableTree.Connect(audioInflatableTree);

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            var fileReaderStarWars = new Import.FseqFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Star Wars 1.fseq"), Path.Combine(Exec.ExpanderSharedFiles, "Seq", "xlights_networks.xml"));
            var fileReaderXmas = new Import.FseqFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "MerryChristmas.fseq"), Path.Combine(Exec.ExpanderSharedFiles, "Seq", "xlights_networks.xml"));
            //var fileReaderHappyNewYear = new Import.FseqFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Happy New Year.fseq"), Path.Combine(Exec.ExpanderSharedFiles, "Seq", "xlights_networks.xml"));

            //var fileReaderCarol = new Import.FseqFileReader(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Carol of Bells 2017.fseq"));

            var pixelMapping2D = Framework.Utility.PixelMapping.GeneratePixelMapping(
                48,
                24,
                pixelOrder: Framework.Utility.PixelOrder.VerticalSnakeStartAtTopRight,
                maxPixelsPerPort: 300);
            dmxPlayback.SetOutput(pixelsMatrix, pixelMapping2D, 40);

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
            stateMachine.ForFromSubroutine(States.MusicChristmasCanon, subMusicChristmasCanon);
            stateMachine.ForFromSubroutine(States.MusicBelieve, subMusicBelieve);
            stateMachine.ForFromSubroutine(States.MusicSarajevo, subMusicSarajevo);
            stateMachine.ForFromSubroutine(States.MusicHolyNight, subMusicHolyNight);
            stateMachine.ForFromSubroutine(States.MusicCarol, subMusicCarol);
            stateMachine.ForFromSubroutine(States.DarthVader, subStarWars);

            // Setup
            var channel1 = Channel.FromId(1);

            //airReindeerBig.SetValue(true, 1);
            //airR2D2Olaf.SetValue(true, 1);
            //airSantaPoppy1.SetValue(true, 1);
            //airSantaPopup.SetValue(true, 1);
            //airSnowmanSanta.SetValue(true, 1);
            //airTree.SetValue(true, 1);
            lightPoppy.SetBrightness(1, channel1);
            lightR2D2.SetBrightness(1, channel1);
            lightOlaf.SetBrightness(1, channel1);
            lightReindeerBig.SetBrightness(1, channel1);
            lightStairPath1.SetBrightness(1, channel1);
            lightStairPath2.SetBrightness(1, channel1);
            lightStairPath3.SetBrightness(1, channel1);
            lightNet1.SetBrightness(1, channel1);
            lightNet2.SetBrightness(1, channel1);
            lightNet3.SetBrightness(1, channel1);
            lightNet4.SetBrightness(1, channel1);
            lightNet5.SetBrightness(1, channel1);
            lightNet6.SetBrightness(1, channel1);
            lightNet7.SetBrightness(1, channel1);
            lightNet8.SetBrightness(1, channel1);
            lightNet9.SetBrightness(1, channel1);
            lightNet10.SetBrightness(1, channel1);
            lightNet11.SetBrightness(1, channel1);
            lightHangingStar.SetBrightness(1, channel1);
            //lightHangingStar2.SetBrightness(1, channel1);
            //lightHangingStar3.SetBrightness(1, channel1);
            lightInflatableTree.SetBrightness(1, channel1);
            lightHat1.SetBrightness(1, channel1);
            lightHat2.SetBrightness(1, channel1);
            lightHat3.SetBrightness(1, channel1);
            lightHat4.SetBrightness(1, channel1);
            //lightPackages.SetValue(true, channel1);
            lightSanta.SetBrightness(1, channel1);
            lightSantaPopup.SetBrightness(1, channel1);

            stateMachine.For(States.Setup)
                .Execute(ins =>
                {
                    Exec.SetGlobalChannel(Channel.FromId(1));

                    dmxPlayback.Load(fileReaderXmas);
                    //dmxPlayback.Load(fileReaderCarol);
                    dmxPlayback.Run(true);
                    ins.WaitUntilCancel();
                }).
                TearDown(ins =>
                {
                    dmxPlayback.Stop();
                    Exec.SetGlobalChannel(Channel.Main);
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
            acnOutput.Connect(new Physical.Pixel1D(vaderEyesPixels), SacnUniversePixelSaber, 181);

            //acnOutput.Connect(new Physical.GenericDimmer(airReindeerBig, 10), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(airR2D2Olaf, 12), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 64), SacnUniverseEdmx4a);
            //acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 65), SacnUniverseEdmx4a);
            //acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 1), SacnUniverseRenard19);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 2), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 3), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 4), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 5), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairRail1, 10), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairRail2, 4), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail1, 11), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail2, 5), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightRail3, 9), SacnUniverseRenard2x8);
            //acnOutput.Connect(new Physical.GenericDimmer(lightRail4, 66), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood4, 310), SacnUniverseEdmx4b);
            //acnOutput.Connect(new Physical.RGBStrobe(lightFlood1, 60), SacnUniverseEdmx4);
            //acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood1, 300), SacnUniverseEdmx4b);
            //acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood2, 330), SacnUniverseEdmx4b);
            //acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood3, 340), SacnUniverseEdmx4b);
            //acnOutput.Connect(new Physical.RGBStrobe(lightFlood4, 40), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.RGBStrobe(lightFlood4, 40), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood5, 340), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.RGBStrobe(lightFlood6, 80), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.MarcGamutParH7(lightFlood7, 300), SacnUniverseLedmx);

            //acnOutput.Connect(new Physical.GenericDimmer(laser, 1), SacnUniverseRenard18);

            acnOutput.Connect(new Physical.GenericDimmer(lightOlaf, 129), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(airSnowmanSanta, 13), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(airInflatableTree, 200), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightInflatableTree, 6), SacnUniverseRenard24);
            //acnOutput.Connect(new Physical.GenericDimmer(lightSnowman, 131), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightSanta, 131), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightPoppy, 128), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightSantaPopup, 256), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(movingHead, 200), SacnUniverseEdmx4);

            acnOutput.Connect(new Physical.GenericDimmer(lightReindeerBig, 64), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightPackages, 12), SacnUniverseLedmx);

            acnOutput.Connect(new Physical.GenericDimmer(lightStairPath1, 67), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairPath2, 1), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightStairPath3, 1), SacnUniverseRenard2x8);
            //acnOutput.Connect(new Physical.MonopriceRGBWPinSpot(lightPinSpot, 20), SacnUniverseEdmx4);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTreeStars, 39), SacnUniverseRenard18);

            acnOutput.Connect(new Physical.GenericDimmer(lightR2D2, 66), SacnUniverseLedmx);
            ////acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 11), SacnUniverseRenardBig);
            ////acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 19), SacnUniverseRenardBig);
            acnOutput.Connect(new Physical.GenericDimmer(airSantaPopup, 10), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(airSantaPoppyEtc, 11), SacnUniverseLedmx);
            ////acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 22), SacnUniverseRenardBig);

            //acnOutput.Connect(new Physical.GenericDimmer(hazerFanSpeed, 500), SacnUniverseEdmx4);
            //acnOutput.Connect(new Physical.GenericDimmer(hazerHazeOutput, 501), SacnUniverseEdmx4);
            ////            acnOutput.Connect(new Physical.GenericDimmer(lightStairs2, 25), SacnUniverseRenardBig);
            //acnOutput.Connect(new Physical.GenericDimmer(lightXmasTree, 67), SacnUniverseEdmx4a);
            //acnOutput.Connect(new Physical.GenericDimmer(lightReindeers, 40), SacnUniverseRenard18);

            ////            acnOutput.Connect(new Physical.GenericDimmer(lightStairs1, 1), SacnUniverseRenardSmall);
            ////acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 2), SacnUniverseRenardSmall);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet1, 7), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet2, 8), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet3, 9), SacnUniverseRenard24);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet4, 6), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet5, 3), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet6, 7), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet7, 8), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet8, 2), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet9, 10), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet10, 11), SacnUniverseRenard2x8);
            acnOutput.Connect(new Physical.GenericDimmer(lightNet11, 12), SacnUniverseRenard2x8);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTopper1, 3), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightTopper2, 4), SacnUniverseRenard18);
            acnOutput.Connect(new Physical.MarcGamutParH7(lightVader, 310), SacnUniverseLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar, 65), SacnUniverseLedmx);
            //acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar2, 9), SacnUniverseRenard18);
            //acnOutput.Connect(new Physical.GenericDimmer(lightHangingStar3, 52), SacnUniverseLedmx);

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

            subBackground
                .AutoAddDevices(lockPriority: 100)
                .RunAction(i =>
                {
                    lightReindeerBig.SetBrightness(1);
                    lightStairPath1.SetBrightness(1);
                    lightStairPath2.SetBrightness(1);
                    lightStairPath3.SetBrightness(1);
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
                    lightNet11.SetBrightness(1);
                    pulsatingEffectGeneral.Start();
                    pulsatingEffectTree.Start();
                    pulsatingPinSpot.Start();
                    //lightXmasTree.SetValue(true);
                    lightStairRail1.SetBrightness(1);
                    lightStairRail2.SetBrightness(1);
                    lightSanta.SetBrightness(1);
                    lightSantaPopup.SetBrightness(1);
                    lightSnowman.SetBrightness(1);
                    lightHat1.SetBrightness(1);
                    lightHat2.SetBrightness(1);
                    lightHat3.SetBrightness(1);
                    lightHat4.SetBrightness(1);
                    lightRail1.SetBrightness(1);
                    lightRail2.SetBrightness(1);
                    lightRail3.SetBrightness(1);
                    lightFlood1.SetColor(Color.Red, 1, Channel.Main);
                    lightFlood2.SetColor(Color.Red, 1, Channel.Main);
                    lightFlood3.SetColor(Color.Red, 1, Channel.Main);
                    lightFlood4.SetColor(Color.Red, 1, Channel.Main);

                    saberPixels.SetColor(Color.Red, 0.2, Channel.Main, token: i.Token);
                    vaderEyesPixels.SetColor(Color.Green, 0.4, Channel.Main, token: i.Token);

                    subCandyCane.Run();

                    dmxPlayback.Load(fileReaderXmas);
                    //FIXME dmxPlayback.Load(fileReaderHappyNewYear);
                    dmxPlayback.Run(true);

                    i.WaitUntilCancel();
                })
                .TearDown(i =>
                {
                    dmxPlayback.Stop();
                    Exec.Cancel(subCandyCane);
                    pulsatingEffectGeneral.Stop();
                    pulsatingPinSpot.Stop();
                    pulsatingEffectTree.Stop();
                });

            subCandyCane
                .LockWhenRunning(pixelsRoofEdge)
                .RunAction(i =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int x = 0; x < spacing; x++)
                        {
                            pixelsRoofEdge.Inject((x % spacing) == 0 ? Color.Red : Color.White, 0.5, token: i.Token);

                            i.WaitFor(S(0.30), true);
                        }
                    }
                });

            subSnow
                .RunAction(ins =>
                {
                    //                    pulsatingPinSpot.SetAdditionalData(lightPinSpot, Utils.Data(Color.White));
                    snowMachine.SetValue(true);

                    ins.WaitFor(S(15), true);
                })
                .TearDown(i =>
                {
                    //                    pulsatingPinSpot.SetAdditionalData(lightPinSpot, Utils.Data(Color.Red));
                    snowMachine.SetValue(false);
                });

            subStarWarsCane
                .LockWhenRunning(
                    pixelsRoofEdge)
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
                                    //pixelsMatrix.InjectRow(Color.Yellow, 1.0, token: instance.Token);
                                    break;
                                case 2:
                                case 3:
                                    pixelsRoofEdge.InjectRev(Color.Orange, 0.2, token: instance.Token);
                                    //pixelsMatrix.InjectRow(Color.Orange, 0.2, token: instance.Token);
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
                .LockWhenRunning(lightSantaPopup)
                .RunAction(ins =>
                    {
                        lightSantaPopup.SetBrightness(1);
                        //lightXmasTree.SetValue(true);
                        audioHiFi.PlayTrack("21. Christmas Canon Rock.wav");
                        ins.WaitFor(S(300), true);
                    }).TearDown(i =>
                    {
                        lorChristmasCanon.Stop();
                        audioHiFi.PauseTrack();
                    });

            subMusicBelieve
                .LockWhenRunning(lightSantaPopup, hazerFanSpeed, hazerHazeOutput)
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    hazerFanSpeed.SetBrightness(0.3);
                    hazerHazeOutput.SetBrightness(0.1);
                    audioHiFi.PlayTrack("T.P.E. - 04 - Josh Groban - Believe.flac");
                    ins.WaitFor(S(260), true);
                }).TearDown(i =>
                {
                    lorBelieve.Stop();
                    audioHiFi.PauseTrack();
                });

            subMusicSarajevo
                .LockWhenRunning(lightSantaPopup)
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    //lightXmasTree.SetValue(true);
                    audioHiFi.PlayTrack("04 Christmas Eve _ Sarajevo (Instrum.wav");
                    ins.WaitFor(S(200), true);
                }).TearDown(i =>
                {
                    lorSarajevo.Stop();
                    audioHiFi.PauseTrack();
                });

            subMusicHolyNight
                .LockWhenRunning(lightSantaPopup)
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    //lightXmasTree.SetValue(true);
                    audioHiFi.PlayTrack("01 O Come All Ye Faithful _ O Holy N.wav");
                    ins.WaitFor(S(260), true);
                }).TearDown(i =>
                {
                    lorHolyNight.Stop();
                    audioHiFi.PauseTrack();
                });

            subMusicCarol
                .LockWhenRunning(lightSantaPopup)
                .RunAction(ins =>
                {
                    lightSantaPopup.SetBrightness(1);
                    //lightXmasTree.SetValue(true);
                    audioHiFi.PlayTrack("09 Carol of the Bells (Instrumental).wav");
                    ins.WaitFor(S(160), true);
                }).TearDown(i =>
                {
                    lorCarol.Stop();
                    audioHiFi.PauseTrack();
                });

            subPoppy
                .LockWhenRunning(100, lightPoppy)
                .RunAction(ins =>
                {
                    var levelsPlayback = new Framework.Import.LevelsPlayback();
                    levelsPlayback.Output.Controls(b => lightPoppy.SetBrightness(b, token: ins.Token));

                    audioPoppy.PlayEffect("Trolls Sounds of Silence.wav", levelsPlayback);
                    var cts = levelsPlayback.Start(ins.Token);
                    ins.WaitFor(S(46));
                    cts.Cancel();
                });

            subOlaf
                .LockWhenRunning(100, lightOlaf)
                .RunAction(ins =>
                {
                    var levelsPlayback = new Framework.Import.LevelsPlayback();
                    levelsPlayback.Output.Controls(b => lightOlaf.SetBrightness(b, token: ins.Token));

                    audioR2D2Olaf.PlayNewEffect("WarmHugs.wav", 0.0, 1.0, levelsPlayback);
                    var cts = levelsPlayback.Start(ins.Token);
                    ins.WaitFor(S(10));
                    cts.Cancel();
                });

            subInflatableTree
                .LockWhenRunning(150, lightInflatableTree)
                .RunAction(ins =>
                {
                    var levelsPlayback = new Framework.Import.LevelsPlayback();
                    levelsPlayback.Output.Controls(b => lightInflatableTree.SetBrightness(b, token: ins.Token));

                    audioInflatableTree.PlayEffect("07 Nu Har Vi Ljus Här I Vårt Hus.wav", levelsPlayback);
                    var cts = levelsPlayback.Start(ins.Token);
                    ins.WaitFor(S(10));
                    cts.Cancel();
                });

            subR2D2
                .LockWhenRunning(100, lightR2D2)
                .RunAction(ins =>
                {
                    var levelsPlayback = new Framework.Import.LevelsPlayback();
                    levelsPlayback.Output.Controls(b => lightR2D2.SetBrightness(b, token: ins.Token));

                    audioR2D2Olaf.PlayNewEffect("Im C3PO.wav", 1.0, 0.0, levelsPlayback);
                    var cts = levelsPlayback.Start(ins.Token);
                    ins.WaitFor(S(4));
                    cts.Cancel();

                    audioR2D2Olaf.PlayNewEffect("Processing R2D2.wav", 0.5, 0.0, levelsPlayback);
                    cts = levelsPlayback.Start(ins.Token);
                    ins.WaitFor(S(5));
                    cts.Cancel();
                });

            subStarWars
                .LockWhenRunning(
                    lockPriority: 200,
                    saberPixels,
                    haloPixels,
                    vaderEyesPixels,
                    lightVader,
                    lightR2D2,
                    lightHangingStar)
                .RunAction(instance =>
                {
                    //Exec.Cancel(subCandyCane);
                    subStarWarsCane.Run();
                    lightR2D2.SetBrightness(1.0, token: instance.Token);

                    dmxPlayback.Load(fileReaderStarWars);

                    audioHiFi.PlayTrack("01. Star Wars - Main Title.wav");
                    dmxPlayback.Run(true);

                    instance.WaitFor(S(16), true);

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

                    vaderEyesPixels.SetColor(Color.Green, brightness: 1.0, token: instance.Token);
                    audioDarthVader.PlayEffect("saberon.wav");
                    for (int sab = 0; sab < 33; sab++)
                    {
                        saberPixels.Inject(Color.Red, 0.5, token: instance.Token);
                        instance.WaitFor(S(0.01));
                    }
                    instance.WaitFor(S(1));
                    audioHiFi.PauseTrack();

                    lightVader.SetColor(Color.Red, 1.0, Channel.Main, token: instance.Token);
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
                    vaderEyesPixels.SetBrightness(brightness: 0, token: instance.Token);

                    instance.WaitFor(S(2));
                })
                .TearDown(i =>
                {
                    audioHiFi.PauseTrack();
                });


            inInflatableTree.Output.Subscribe(x =>
            {
                if (OkToRunInteractive(x))
                    subInflatableTree.Run();
            });

            inOlaf.Output.Subscribe(x =>
            {
                if (OkToRunInteractive(x))
                    subOlaf.Run();
            });

            inR2D2.Output.Subscribe(x =>
            {
                if (OkToRunInteractive(x))
                    subR2D2.Run();
            });

            inPoppy.Output.Subscribe(x =>
            {
                if (OkToRunInteractive(x))
                    subPoppy.Run();
            });

            controlButtonWhite.WhenOutputChanges(x =>
            {
                if (OkToRunInteractive(x))
                {
                    if (stateMachine.CurrentState == States.DarthVader)
                        // Don't allow when Darth is running
                        return;
                    subSnow.Run();
                }
            });

            controlButtonYellow.WhenOutputChanges(x =>
            {
                if (OkToRunInteractive(x))
                    stateMachine.GoToState(States.DarthVader);
            });

            controlButtonBlue.WhenOutputChanges(x =>
            {
                if (OkToRunInteractive(x))
                    stateMachine.GoToState(States.MusicBelieve);
            });

            controlButtonGreen.WhenOutputChanges(x =>
            {
                if (OkToRunInteractive(x))
                    stateMachine.GoToState(States.MusicCarol);
            });

            controlButtonBlack.WhenOutputChanges(x =>
            {
                if (x)
                {
                    if (OkToRunInteractive(x))
                        stateMachine.GoToState(States.MusicSarajevo);
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
                    if (OkToRunInteractive(x))
                        stateMachine.GoToState(States.MusicChristmasCanon);
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

            inSnowMachine.Output.Subscribe(x =>
            {
                snowMachine.SetValue(x);
            });

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

            ImportAndMapChristmasCanon();
            ImportAndMapBelieve();
            ImportAndMapSarajevo();
            ImportAndMapHolyNight();
            ImportAndMapCarol();

            ConfigureMIDI();
            ConfigureOSC();
        }

        private void CheckAirState()
        {
            bool airOn = (stateMachine.CurrentState != null) && stateMachine.CurrentState != States.Setup &&
                mainSchedule.IsOpen && airActivated.Value;

            airReindeerBig.SetValue(airOn, Channel.Main);
            airR2D2Olaf.SetValue(airOn, Channel.Main);
            airSantaPoppyEtc.SetValue(airOn, Channel.Main);
            airSantaPopup.SetValue(airOn, Channel.Main);
            airSnowmanSanta.SetValue(airOn, Channel.Main);
            airInflatableTree.SetValue(airOn, Channel.Main);
            lightPackages.SetValue(airOn, Channel.Main);
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

        private void ImportAndMapChristmasCanon()
        {
            lorChristmasCanon.LoadFromFile(Path.Combine(Exec.ExpanderSharedFiles, "Seq", "Cannon Rock104.lms"));

            lorChristmasCanon.Progress.Subscribe(x =>
            {
                this.log.Verbose("Christmas Canon {0:N0} ms", x);
            });

            //            lorChristmasCanon.Dump();

            //lorChristmasCanon.MapDevice("Roof 1", pixelsGround, Utils.Data(Color.Red));
            //lorChristmasCanon.MapDevice("Roof 2", pixelsTree, Utils.Data(Color.Green));
            //lorChristmasCanon.MapDevice("Roof 3", pixelsHeart, Utils.Data(Color.Red));

            //lorChristmasCanon.ControlDevice(pixelsBetweenTrees);
            //lorChristmasCanon.MapDevice("Big Tree 1",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 6, token: lorChristmasCanon.Token)));
            //lorChristmasCanon.MapDevice("Big Tree 2",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 6, token: lorChristmasCanon.Token)));
            //lorChristmasCanon.MapDevice("Big Tree 3",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 6, token: lorChristmasCanon.Token)));
            //lorChristmasCanon.MapDevice("Big Tree 4",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 6, token: lorChristmasCanon.Token)));
            //lorChristmasCanon.MapDevice("Big Tree 5",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 6, token: lorChristmasCanon.Token)));
            //lorChristmasCanon.MapDevice("Big Tree 6",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 6, token: lorChristmasCanon.Token)));
            //lorChristmasCanon.MapDevice("Big Tree 7",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 6, token: lorChristmasCanon.Token)));
            //lorChristmasCanon.MapDevice("Big Tree 8",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 6, token: lorChristmasCanon.Token)));

            //lorChristmasCanon.MapDevice("Sidewalk 1", lightNet1);
            //lorChristmasCanon.MapDevice("Sidewalk 2", lightNet2);
            //lorChristmasCanon.MapDevice("Sidewalk 3", lightNet3);
            //lorChristmasCanon.MapDevice("Sidewalk 4", lightNet4);
            //lorChristmasCanon.MapDevice("Sidewalk 5", lightNet5);
            //lorChristmasCanon.MapDevice("Sidewalk 6", lightNet6);
            //lorChristmasCanon.MapDevice("Sidewalk 7", lightNet7);
            //lorChristmasCanon.MapDevice("Sidewalk 8", lightNet8);
            //lorChristmasCanon.MapDevice("Sidewalk 1", lightNet9);
            //lorChristmasCanon.MapDevice("Sidewalk 2", lightNet10);

            lorChristmasCanon.MapDevice("Sidewalk 1", lightHat1);
            lorChristmasCanon.MapDevice("Sidewalk 2", lightHat2);
            lorChristmasCanon.MapDevice("Sidewalk 3", lightHat3);
            lorChristmasCanon.MapDevice("Sidewalk 4", lightHat4);

            lorChristmasCanon.MapDevice("Bush Right", lightSanta);
            //lorChristmasCanon.MapDevice("Bush Right", lightTopper1);
            lorChristmasCanon.MapDevice("Column Red Right", lightFlood1, Utils.Data(Color.Red));
            lorChristmasCanon.MapDevice("Column Blue Right", lightFlood2, Utils.Data(Color.Blue));
            //lorChristmasCanon.MapDevice("Column Red Right", lightFlood5, Utils.Data(Color.Red));
            //lorChristmasCanon.MapDevice("Column Blue Right", lightFlood6, Utils.Data(Color.Blue));
            lorChristmasCanon.MapDevice("Rail Right", lightStairRail1);
            lorChristmasCanon.MapDevice("Rail Right", lightInflatableTree);
            lorChristmasCanon.MapDevice("Column Red Left", lightFlood3, Utils.Data(Color.Red));
            lorChristmasCanon.MapDevice("Column Blue Left", lightFlood4, Utils.Data(Color.Blue));
            //lorChristmasCanon.MapDevice("Column Blue Left", lightFlood7, Utils.Data(Color.Blue));
            lorChristmasCanon.MapDevice("Rail Left", lightStairRail2);
            lorChristmasCanon.MapDevice("Ice Cycles", lightHangingStar);
            //lorChristmasCanon.MapDevice("Ice Cycles", lightTreeStars);
            lorChristmasCanon.MapDevice("Left Window April", lightOlaf);
            lorChristmasCanon.MapDevice("Left Wreif", lightR2D2);
            //lorChristmasCanon.MapDevice("Left Wreif", lightStairs3);
            lorChristmasCanon.MapDevice("Main Door", lightPoppy);
            //lorChristmasCanon.MapDevice("Right Wreif", lightReindeers);
            //lorChristmasCanon.MapDevice("Right Wreif", lightStairs2);
            lorChristmasCanon.MapDevice("Right Window", lightReindeerBig);
            //lorChristmasCanon.MapDevice("Right Window", lightStairs1);
            lorChristmasCanon.MapDevice("Bush Left", lightSnowman);
            //lorChristmasCanon.MapDevice("Bush Left", lightTopper2);

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

            //lorBelieve.MapDevice("Yard 1", lightNet1);
            //lorBelieve.MapDevice("Yard 2", lightNet2);
            //lorBelieve.MapDevice("Yard 3", lightNet3);
            //lorBelieve.MapDevice("Yard 4", lightNet4);
            //lorBelieve.MapDevice("Yard 5", lightNet5);
            //lorBelieve.MapDevice("Yard 6", lightNet6);
            //lorBelieve.MapDevice("Yard 7", lightNet7);
            //lorBelieve.MapDevice("Yard 8", lightNet8);
            //lorBelieve.MapDevice("Yard 9", lightNet9);
            //lorBelieve.MapDevice("Yard 10", lightNet10);
            lorBelieve.MapDevice("Yard 5", lightHat1);
            lorBelieve.MapDevice("Yard 6", lightHat2);
            lorBelieve.MapDevice("Yard 7", lightHat3);
            lorBelieve.MapDevice("Yard 8", lightHat4);

            //lorBelieve.MapDevice("Yard 9", lightTreeStars);
            lorBelieve.MapDevice("Yard 10", lightReindeerBig);

            lorBelieve.MapDevice("House 1", lightR2D2);
            lorBelieve.MapDevice("House 2", lightOlaf);
            lorBelieve.MapDevice("House 3", lightPoppy);

            //lorBelieve.MapDevice("Wreath W", lightStairs1);
            //lorBelieve.MapDevice("Wreath R", lightStairs2);
            //lorBelieve.MapDevice("Wreath W", lightStairs3);
            lorBelieve.MapDevice("Wreath W", lightStairRail1);
            lorBelieve.MapDevice("Wreath R", lightStairRail2);

            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood1);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood2);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood3);
            lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood4);
            //lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood5);
            //lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood6);
            //lorBelieve.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood7);

            //lorBelieve.MapDevice("Ferris Wheel 1", lightTopper1);
            //lorBelieve.MapDevice("Ferris Wheel 2", lightTopper2);
            //lorBelieve.MapDevice("Ferris Wheel 3", lightRail1);
            //lorBelieve.MapDevice("Ferris Wheel 4", lightRail2);
            //lorBelieve.MapDevice("Ferris Wheel 5", lightReindeers);
            //lorBelieve.MapDevice("Ferris Wheel 5", lightRail3);
            //lorBelieve.MapDevice("Ferris Wheel 6", lightRail4);
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

            //lorBelieve.ControlDevice(pixelsBetweenTrees);
            //lorBelieve.MapDevice("Mega Tree 1",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 2",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 3, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 3",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 4",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 9, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 5",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 6",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 15, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 7",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 8",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 21, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 9",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 10",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 27, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 11",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 12",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 33, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 13",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 14",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 39, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 15",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, token: lorBelieve.Token)));
            //lorBelieve.MapDevice("Mega Tree 16",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 45, 3, token: lorBelieve.Token)));

            lorBelieve.MapDevice("Mega Star", pixelsRoofEdge, Utils.Data(Color.Red));
            //lorBelieve.MapDevice("Mega Star", pixelsGround, Utils.Data(Color.White));
            //lorBelieve.MapDevice("Mega Star", pixelsTree, Utils.Data(Color.Red));
            //lorBelieve.MapDevice("Mega Star", pixelsHeart, Utils.Data(Color.Red));

            lorBelieve.Prepare();
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

            //lorSarajevo.MapDevice("Yard 1", lightNet1);
            //lorSarajevo.MapDevice("Yard 2", lightNet2);
            //lorSarajevo.MapDevice("Yard 3", lightNet3);
            //lorSarajevo.MapDevice("Yard 4", lightNet4);
            //lorSarajevo.MapDevice("Yard 5", lightNet5);
            //lorSarajevo.MapDevice("Yard 6", lightNet6);
            //lorSarajevo.MapDevice("Yard 7", lightNet7);
            //lorSarajevo.MapDevice("Yard 8", lightNet8);
            //lorSarajevo.MapDevice("Yard 9", lightNet9);
            //lorSarajevo.MapDevice("Yard 10", lightNet10);
            lorSarajevo.MapDevice("Yard 5", lightHat1);
            lorSarajevo.MapDevice("Yard 6", lightHat2);
            lorSarajevo.MapDevice("Yard 7", lightHat3);
            lorSarajevo.MapDevice("Yard 8", lightHat4);

            //lorSarajevo.MapDevice("Yard 9", lightTreeStars);
            lorSarajevo.MapDevice("Yard 10", lightReindeerBig);

            lorSarajevo.MapDevice("House 1", lightR2D2);
            lorSarajevo.MapDevice("House 2", lightOlaf);
            lorSarajevo.MapDevice("House 3", lightPoppy);

            //lorSarajevo.MapDevice("Wreath W", lightStairs1);
            //lorSarajevo.MapDevice("Wreath R", lightStairs2);
            //lorSarajevo.MapDevice("Wreath W", lightStairs3);
            lorSarajevo.MapDevice("Wreath W", lightStairRail1);
            lorSarajevo.MapDevice("Wreath R", lightStairRail2);

            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood1);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood2);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood3);
            lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood4);
            //lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood5);
            //lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood6);
            //lorSarajevo.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood7);

            //lorSarajevo.MapDevice("Ferris Wheel 1", lightTopper1);
            //lorSarajevo.MapDevice("Ferris Wheel 2", lightTopper2);
            //lorSarajevo.MapDevice("Ferris Wheel 3", lightRail1);
            //lorSarajevo.MapDevice("Ferris Wheel 4", lightRail2);
            //lorSarajevo.MapDevice("Ferris Wheel 5", lightReindeers);
            //lorSarajevo.MapDevice("Ferris Wheel 5", lightRail3);
            //lorSarajevo.MapDevice("Ferris Wheel 6", lightRail4);
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

            //lorSarajevo.ControlDevice(pixelsBetweenTrees);
            //lorSarajevo.MapDevice("Mega Tree 1",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 2",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 3, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 3",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 4",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 9, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 5",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 6",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 15, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 7",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 8",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 21, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 9",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 10",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 27, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 11",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 12",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 33, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 13",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 14",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 39, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 15",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, token: lorSarajevo.Token)));
            //lorSarajevo.MapDevice("Mega Tree 16",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 45, 3, token: lorSarajevo.Token)));

            lorSarajevo.MapDevice("Mega Star", pixelsRoofEdge, Utils.Data(Color.White));
            //lorSarajevo.MapDevice("Mega Star", pixelsGround, Utils.Data(Color.White));
            //lorSarajevo.MapDevice("Mega Star", pixelsTree, Utils.Data(Color.White));
            //lorSarajevo.MapDevice("Mega Star", pixelsHeart, Utils.Data(Color.White));

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

            //lorHolyNight.MapDevice("Yard 1", lightNet1);
            //lorHolyNight.MapDevice("Yard 2", lightNet2);
            //lorHolyNight.MapDevice("Yard 3", lightNet3);
            //lorHolyNight.MapDevice("Yard 4", lightNet4);
            //lorHolyNight.MapDevice("Yard 5", lightNet5);
            //lorHolyNight.MapDevice("Yard 6", lightNet6);
            //lorHolyNight.MapDevice("Yard 7", lightNet7);
            //lorHolyNight.MapDevice("Yard 8", lightNet8);
            //lorHolyNight.MapDevice("Yard 9", lightNet9);
            //lorHolyNight.MapDevice("Yard 10", lightNet10);
            //lorHolyNight.MapDevice("Yard 5", lightHat1);
            lorHolyNight.MapDevice("Yard 6", lightHat2);
            lorHolyNight.MapDevice("Yard 7", lightHat3);
            lorHolyNight.MapDevice("Yard 8", lightHat4);

            //lorHolyNight.MapDevice("Yard 9", lightTreeStars);
            lorHolyNight.MapDevice("Yard 10", lightReindeerBig);

            lorHolyNight.MapDevice("House 1", lightR2D2);
            lorHolyNight.MapDevice("House 2", lightOlaf);
            lorHolyNight.MapDevice("House 3", lightPoppy);

            //lorHolyNight.MapDevice("Wreath W", lightStairs1);
            //lorHolyNight.MapDevice("Wreath R", lightStairs2);
            //lorHolyNight.MapDevice("Wreath W", lightStairs3);
            lorHolyNight.MapDevice("Wreath W", lightStairRail1);
            lorHolyNight.MapDevice("Wreath R", lightStairRail2);

            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood1);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood2);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood3);
            lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood4);
            //lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood5);
            //lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood6);
            //lorHolyNight.MapDeviceRGBW("Floods R", "Floods G", "Floods B", "Floods W", lightFlood7);

            //lorHolyNight.MapDevice("Ferris Wheel 1", lightTopper1);
            //lorHolyNight.MapDevice("Ferris Wheel 2", lightTopper2);
            //lorHolyNight.MapDevice("Ferris Wheel 3", lightRail1);
            //lorHolyNight.MapDevice("Ferris Wheel 4", lightRail2);
            //lorHolyNight.MapDevice("Ferris Wheel 5", lightReindeers);
            //lorHolyNight.MapDevice("Ferris Wheel 5", lightRail3);
            //lorHolyNight.MapDevice("Ferris Wheel 6", lightRail4);
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

            //lorHolyNight.ControlDevice(pixelsBetweenTrees);
            //lorHolyNight.MapDevice("Mega Tree 1",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 0, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 2",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 3, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 3",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 6, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 4",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 9, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 5",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 12, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 6",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 15, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 7",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 18, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 8",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 21, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 9",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 24, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 10",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 27, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 11",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 30, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 12",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 33, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 13",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 36, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 14",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 39, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 15",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 42, 3, token: lorHolyNight.Token)));
            //lorHolyNight.MapDevice("Mega Tree 16",
            //    new VirtualDevice(b => pixelsBetweenTrees.SetColorRange(Color.Red, b, 45, 3, token: lorHolyNight.Token)));

            lorHolyNight.MapDevice("Mega Star", pixelsRoofEdge, Utils.Data(Color.Red));
            //lorHolyNight.MapDevice("Mega Star", pixelsGround, Utils.Data(Color.White));
            //lorHolyNight.MapDevice("Mega Star", pixelsTree, Utils.Data(Color.Red));
            //lorHolyNight.MapDevice("Mega Star", pixelsHeart, Utils.Data(Color.Red));

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
        }

        public override void Stop()
        {
            audioHiFi.PauseBackground();
        }
    }
}
