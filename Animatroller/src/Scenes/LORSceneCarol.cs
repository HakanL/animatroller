using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Import = Animatroller.Framework.Import;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;
using Controller = Animatroller.Framework.Controller;
using System.Threading.Tasks;
using System.Media;
using CSCore;
using CSCore.SoundOut;
using CSCore.Codecs;

namespace Animatroller.SceneRunner
{
    internal class LORSceneCarol : BaseScene
    {
        Expander.AcnStream acnOutput = new Expander.AcnStream();

        IWaveSource waveSource;
        ISoundOut soundOut = new WasapiOut();

        ColorDimmer3 lightNote1 = new ColorDimmer3();
        ColorDimmer3 lightNote2 = new ColorDimmer3();
        ColorDimmer3 lightNote3 = new ColorDimmer3();
        ColorDimmer3 lightNote4 = new ColorDimmer3();
        ColorDimmer3 lightNote5 = new ColorDimmer3();
        ColorDimmer3 lightNote6 = new ColorDimmer3();
        ColorDimmer3 lightNote7 = new ColorDimmer3();
        ColorDimmer3 lightNote8 = new ColorDimmer3();
        ColorDimmer3 lightNote9 = new ColorDimmer3();
        ColorDimmer3 lightNote10 = new ColorDimmer3();
        ColorDimmer3 lightNote11 = new ColorDimmer3();
        ColorDimmer3 lightNote12 = new ColorDimmer3();

        Dimmer3 lightSky1 = new Dimmer3();
        Dimmer3 lightSky2 = new Dimmer3();
        Dimmer3 lightSky3 = new Dimmer3();
        Dimmer3 lightSky4 = new Dimmer3();
        Dimmer3 lightSky5 = new Dimmer3();

        Dimmer3 lightRooftop = new Dimmer3();
        Dimmer3 lightStar1 = new Dimmer3();
        Dimmer3 lightStar2 = new Dimmer3();
        Dimmer3 lightStar3 = new Dimmer3();
        Dimmer3 lightStarExtra = new Dimmer3();

        Dimmer3 lightTreeA = new Dimmer3();
        Dimmer3 lightTreeB = new Dimmer3();
        
        ColorDimmer3 lightTest1 = new ColorDimmer3();
        ColorDimmer3 lightTest2 = new ColorDimmer3();
        ColorDimmer3 lightTest3 = new ColorDimmer3();
        ColorDimmer3 lightTest4 = new ColorDimmer3();
        ColorDimmer3 lightTest5 = new ColorDimmer3();
        ColorDimmer3 lightTest6 = new ColorDimmer3();
        ColorDimmer3 lightTest7 = new ColorDimmer3();
        ColorDimmer3 lightTest8 = new ColorDimmer3();

        ColorDimmer3 lightREdge = new ColorDimmer3();
        ColorDimmer3 lightBottom = new ColorDimmer3();
        ColorDimmer3 lightGarage = new ColorDimmer3();
        ColorDimmer3 lightRWindow = new ColorDimmer3();
        ColorDimmer3 lightCWindow = new ColorDimmer3();
        ColorDimmer3 lightLWindow = new ColorDimmer3();
        ColorDimmer3 lightFrontDoor = new ColorDimmer3();
        ColorDimmer3 lightBush = new ColorDimmer3();

        DigitalInput2 testButton = new DigitalInput2();
        Import.LorImport2 lorImport = new Import.LorImport2();


        public LORSceneCarol(IEnumerable<string> args)
        {
            lorImport.LoadFromFile(@"..\..\..\Test Files\David Foster - Carol of the Bells.lms");

            lorImport.MapDeviceRGB("E - 1", "D# - 2", "D - 3", lightNote1);
            lorImport.MapDeviceRGB("C# - 4", "C - 5", "B - 6", lightNote2);
            lorImport.MapDeviceRGB("A# - 7", "A - 8", "G# - 9", lightNote3);
            lorImport.MapDeviceRGB("G - 10", "F# - 11", "F - 12", lightNote4);
            lorImport.MapDeviceRGB("E - 13", "D# - 14", "D - 15", lightNote5);
            lorImport.MapDeviceRGB("C# - 16", "C - 1", "B - 2", lightNote6);
            lorImport.MapDeviceRGB("A# - 3", "A - 4", "G# - 5", lightNote7);
            lorImport.MapDeviceRGB("G - 6", "F# - 7", "F - 8", lightNote8);
            lorImport.MapDeviceRGB("E - 9", "D# - 10", "D - 11", lightNote9);
            lorImport.MapDeviceRGB("C# - 12", "C - 13", "B - 14", lightNote10);
            lorImport.MapDevice("A# - 15", lightNote11);
            lorImport.MapDevice("A - 16", lightNote12);

            lorImport.MapDevice("Sky 1", lightSky1);
            lorImport.MapDevice("Sky 2", lightSky2);
            lorImport.MapDevice("Sky 3", lightSky3);
            lorImport.MapDevice("Sky 4", lightSky4);
            lorImport.MapDevice("Sky 5", lightSky5);

            lorImport.MapDevice("Rooftop", lightRooftop);

            lorImport.MapDevice("Star1", lightStar1);
            lorImport.MapDevice("Star2", lightStar2);
            lorImport.MapDevice("Star3", lightStar3);
            lorImport.MapDevice("Star extra", lightStarExtra);


            lorImport.MapDeviceRGBW("R-Edge R", "R-Edge G", "R-Edge B", "R-Edge W", lightREdge);
            lorImport.MapDeviceRGBW("R-Bottom", "G-Bottom", "B-Bottom", "W-Bottom", lightBottom);
            lorImport.MapDeviceRGBW("Garage R", "Garage G", "Garage B", "Garage W", lightGarage);
            lorImport.MapDeviceRGBW("Rwindo R", "Rwindo G", "Rwindo B", "Rwindo W", lightRWindow);
            lorImport.MapDeviceRGBW("Cwindo R", "Cwindo G", "Cwindo B", "Cwindo W", lightCWindow);
            lorImport.MapDeviceRGBW("Lwindo R", "Lwindo G", "Lwindo B", "Lwindo W", lightLWindow);
            lorImport.MapDeviceRGBW("Ft door R", "Ft door G", "Ft door B", "FT door W", lightFrontDoor);
            lorImport.MapDeviceRGBW("Bush - red", "Bush - green", "Bush - blue", "Bush - white", lightBush);

            lorImport.MapDevice("Tree - A", lightTreeA);
            lorImport.MapDevice("Tree - B", lightTreeB);


            // lorImport.MapDevice("Spoke 1a", light);
            // lorImport.MapDevice("Spoke 2a", light);
            // lorImport.MapDevice("Spoke 3a", light);
            // lorImport.MapDevice("Spoke  4a", light);
            // lorImport.MapDevice("Spoke 5a", light);
            // lorImport.MapDevice("Spoke 6a", light);
            // lorImport.MapDevice("Spoke 7a", light);
            // lorImport.MapDevice("Spoke 8a", light);
            // lorImport.MapDevice("Spoke 9a", light);
            // lorImport.MapDevice("Spoike 10a", light);
            // lorImport.MapDevice("Spoke  11a", light);
            // lorImport.MapDevice("Spoke  12a", light);
            // lorImport.MapDevice("Spoke  13a", light);
            // lorImport.MapDevice("Spoke  14a", light);
            // lorImport.MapDevice("Spoke  15a", light);
            // lorImport.MapDevice("Spoke  16a", light);
            // lorImport.MapDevice("Pillar L8", light);
            // lorImport.MapDevice("Pillar L7", light);
            // lorImport.MapDevice("Pillar L6", light);
            // lorImport.MapDevice("Pillar L5", light);
            // lorImport.MapDevice("Pillar L4", light);
            // lorImport.MapDevice("Pillar L3", light);
            // lorImport.MapDevice("Pillar L2", light);
            // lorImport.MapDevice("Pillar L1", light);
            // lorImport.MapDevice("Pillar R8", light);
            // lorImport.MapDevice("Pillar R7", light);
            // lorImport.MapDevice("Pillar R6", light);
            // lorImport.MapDevice("Pillar R5", light);
            // lorImport.MapDevice("Pillar R4", light);
            // lorImport.MapDevice("Pillar R3", light);
            // lorImport.MapDevice("Pillar R2", light);
            // lorImport.MapDevice("Pillar R1", light);
            // lorImport.MapDevice("8  MiniTree 1r", light);
            // lorImport.MapDevice("8  MiniTree 2r", light);
            // lorImport.MapDevice("8  MiniTree 3r", light);
            // lorImport.MapDevice("8  MiniTree 4r", light);
            // lorImport.MapDevice("8  MiniTree 5r", light);
            // lorImport.MapDevice("8  MiniTree 6r", light);
            // lorImport.MapDevice("8  MiniTree 7r", light);
            // lorImport.MapDevice("8  MiniTree 8r", light);
            // lorImport.MapDevice("8  MiniTree 9r", light);
            // lorImport.MapDevice("8  MiniTree 10r", light);
            // lorImport.MapDevice("8  MiniTree 11r", light);
            // lorImport.MapDevice("8  MiniTree 12r", light);
            // lorImport.MapDevice("8  MiniTree 13r", light);
            // lorImport.MapDevice("8  MiniTree 14r", light);
            // lorImport.MapDevice("8  MiniTree 15r", light);
            // lorImport.MapDevice("8  MiniTree 16r", light);
            // lorImport.MapDevice("MiniTree 1g", light);
            // lorImport.MapDevice("MiniTree 2g", light);
            // lorImport.MapDevice("MiniTree 3g", light);
            // lorImport.MapDevice("MiniTree 4g", light);
            // lorImport.MapDevice("MiniTree 5g", light);
            // lorImport.MapDevice("MiniTree 6g", light);
            // lorImport.MapDevice("MiniTree 7g", light);
            // lorImport.MapDevice("MiniTree 8g", light);
            // lorImport.MapDevice("MiniTree 9g", light);
            // lorImport.MapDevice("MiniTree 10g", light);
            // lorImport.MapDevice("MiniTree 11g", light);
            // lorImport.MapDevice("MiniTree 12g", light);
            // lorImport.MapDevice("MiniTree 13g", light);
            // lorImport.MapDevice("MiniTree 14g", light);
            // lorImport.MapDevice("MiniTree 15g", light);
            // lorImport.MapDevice("MiniTree 16g", light);
            // lorImport.MapDevice("Hray B1", light);
            // lorImport.MapDevice("Hray B2", light);
            // lorImport.MapDevice("Hray B3", light);
            // lorImport.MapDevice("Hray B4", light);
            // lorImport.MapDevice("Hray B5", light);
            // lorImport.MapDevice("Hray B6", light);
            // lorImport.MapDevice("Hray B7", light);
            // lorImport.MapDevice("Hray B8", light);
            // lorImport.MapDevice("Hray R1", light);
            // lorImport.MapDevice("Hray R2", light);
            // lorImport.MapDevice("Hray R3", light);
            // lorImport.MapDevice("Hray R4", light);
            // lorImport.MapDevice("Hray R5", light);
            // lorImport.MapDevice("Hray R6", light);
            // lorImport.MapDevice("Hray R7", light);
            // lorImport.MapDevice("Hray R8", light);
            // lorImport.MapDevice("Vray 1", light);
            // lorImport.MapDevice("Vray 2", light);
            // lorImport.MapDevice("Vray 3", light);
            // lorImport.MapDevice("Vray 4", light);
            // lorImport.MapDevice("Vray 5", light);
            // lorImport.MapDevice("Vray 6", light);
            // lorImport.MapDevice("Vray 7", light);
            // lorImport.MapDevice("Vray 8", light);
            // lorImport.MapDevice("Vray 9", light);
            // lorImport.MapDevice("Vray 10", light);
            // lorImport.MapDevice("Vray 11", light);
            // lorImport.MapDevice("Vray 12", light);
            // lorImport.MapDevice("Vray 13", light);
            // lorImport.MapDevice("Vray 14", light);
            // lorImport.MapDevice("Vray 15", light);
            // lorImport.MapDevice("Vray 16", light);
            // lorImport.MapDevice("Vray 17", light);
            // lorImport.MapDevice("Vray 18", light);
            // lorImport.MapDevice("Vray 19", light);
            // lorImport.MapDevice("Vray 20", light);
            // lorImport.MapDevice("Vray 21", light);
            // lorImport.MapDevice("Vray 22", light);
            // lorImport.MapDevice("Vray 23", light);
            // lorImport.MapDevice("Vray 24", light);
            // lorImport.MapDevice("Vray 25", light);
            // lorImport.MapDevice("Vray 26", light);
            // lorImport.MapDevice("Vray 27", light);
            // lorImport.MapDevice("Vray 28", light);
            // lorImport.MapDevice("Vray 29", light);
            // lorImport.MapDevice("Vray 30", light);
            // lorImport.MapDevice("Vray 31", light);
            // lorImport.MapDevice("Vray 32", light);
            // lorImport.MapDevice("Arch 1-1", light);
            // lorImport.MapDevice("Arch 1-2", light);
            // lorImport.MapDevice("Arch 1-3", light);
            // lorImport.MapDevice("Arch 1-4", light);
            // lorImport.MapDevice("Arch 1-5", light);
            // lorImport.MapDevice("Arch 1-6", light);
            // lorImport.MapDevice("Arch 1-7", light);
            // lorImport.MapDevice("Arch 1-8", light);
            // lorImport.MapDevice("Arch 2-1", light);
            // lorImport.MapDevice("Arch 2-2", light);
            // lorImport.MapDevice("Arch 2-3", light);
            // lorImport.MapDevice("Arch 2-4", light);
            // lorImport.MapDevice("Arch 2-5", light);
            // lorImport.MapDevice("Arch 2-6", light);
            // lorImport.MapDevice("Arch 2-7", light);
            // lorImport.MapDevice("Arch 2-8", light);
            // lorImport.MapDevice("Arch 3-1", light);
            // lorImport.MapDevice("Arch 3-2", light);
            // lorImport.MapDevice("Arch 3-3", light);
            // lorImport.MapDevice("Arch 3-4", light);
            // lorImport.MapDevice("Arch 3-5", light);
            // lorImport.MapDevice("Arch 3-6", light);
            // lorImport.MapDevice("Arch 3-7", light);
            // lorImport.MapDevice("Arch 3-8", light);
            // lorImport.MapDevice("Arch 4-1", light);
            // lorImport.MapDevice("Arch 4-2", light);
            // lorImport.MapDevice("Arch 4-3", light);
            // lorImport.MapDevice("Arch 4-4", light);
            // lorImport.MapDevice("Arch 4-5", light);
            // lorImport.MapDevice("Arch 4-6", light);
            // lorImport.MapDevice("Arch 4-7", light);
            // lorImport.MapDevice("Arch 4-8", light);


            lorImport.Prepare();
            lorImport.Dump();


            waveSource = CodecFactory.Instance.GetCodec(@"C:\Projects\Other\ChristmasSounds\trk\09 Carol of the Bells (Instrumental).wav");

            soundOut.Initialize(waveSource);

            acnOutput.Connect(new Physical.GenericDimmer(lightREdge, 50), 20);
        }

        public override void Start()
        {
            // Set color
            testButton.Output.Subscribe(button =>
            {
                if (button)
                {
                    log.Info("Button press!");

                    log.Debug("Sound pos: {0}", waveSource.GetMilliseconds(waveSource.Position));
                }
            });
        }

        public override void Run()
        {
            lorImport.Start();
            soundOut.Play();
        }

        public override void Stop()
        {
            soundOut.Stop();
        }
    }
}
