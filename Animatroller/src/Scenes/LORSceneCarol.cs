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
        IWaveSource waveSource;
        ISoundOut soundOut = new WasapiOut();

        //private ColorDimmer light1_1;
        //private ColorDimmer light1_2;
        //private ColorDimmer light1_3;
        //private ColorDimmer light1_4;
        //private ColorDimmer light1_5;
        //private ColorDimmer light1_6;
        //private ColorDimmer light1_7;
        //private ColorDimmer light1_8;
        //private ColorDimmer light1_9;
        //private ColorDimmer light1_10;
        //private ColorDimmer light1_11;
        //private ColorDimmer light1_12;
        //private ColorDimmer light1_13;
        //private ColorDimmer light1_14;
        //private ColorDimmer light1_15;
        //private ColorDimmer light1_16;

        //private ColorDimmer light2_1;
        //private ColorDimmer light2_2;
        //private ColorDimmer light2_3;
        //private ColorDimmer light2_4;
        //private ColorDimmer light2_5;
        //private ColorDimmer light2_6;
        //private ColorDimmer light2_7;
        //private ColorDimmer light2_8;
        //private ColorDimmer light2_9;
        //private ColorDimmer light2_10;
        //private ColorDimmer light2_11;
        //private ColorDimmer light2_12;
        //private ColorDimmer light2_13;
        //private ColorDimmer light2_14;
        //private ColorDimmer light2_15;
        //private ColorDimmer light2_16;

        //private ColorDimmer light3_1;
        //private ColorDimmer light3_2;
        //private ColorDimmer light3_3;
        //private ColorDimmer light3_4;
        //private ColorDimmer light3_5;
        //private ColorDimmer light3_6;
        //private ColorDimmer light3_7;
        //private ColorDimmer light3_8;
        //private ColorDimmer light3_9;
        //private ColorDimmer light3_10;
        //private ColorDimmer light3_11;
        //private ColorDimmer light3_12;
        //private ColorDimmer light3_13;
        //private ColorDimmer light3_14;
        //private ColorDimmer light3_15;
        //private ColorDimmer light3_16;

        //private ColorDimmer light4_1;
        //private ColorDimmer light4_2;
        //private ColorDimmer light4_3;
        //private ColorDimmer light4_4;
        //private ColorDimmer light4_5;
        //private ColorDimmer light4_6;
        //private ColorDimmer light4_7;
        //private ColorDimmer light4_8;
        //private ColorDimmer light4_9;
        //private ColorDimmer light4_10;
        //private ColorDimmer light4_11;
        //private ColorDimmer light4_12;
        //private ColorDimmer light4_13;
        //private ColorDimmer light4_14;
        //private ColorDimmer light4_15;
        //private ColorDimmer light4_16;

        private ColorDimmer3 lightTest1 = new ColorDimmer3();
        private ColorDimmer3 lightTest2 = new ColorDimmer3();
        private ColorDimmer3 lightTest3 = new ColorDimmer3();
        private ColorDimmer3 lightTest4 = new ColorDimmer3();
        private ColorDimmer3 lightTest5 = new ColorDimmer3();
        private ColorDimmer3 lightTest6 = new ColorDimmer3();
        private ColorDimmer3 lightTest7 = new ColorDimmer3();
        private ColorDimmer3 lightTest8 = new ColorDimmer3();
        
        private ColorDimmer3 lightRoof1 = new ColorDimmer3();
        private ColorDimmer3 lightRoof2 = new ColorDimmer3();
        private ColorDimmer3 lightRoof3 = new ColorDimmer3();
        private ColorDimmer3 lightRoof4 = new ColorDimmer3();

        private ColorDimmer3 lightSidewalk1 = new ColorDimmer3();
        private ColorDimmer3 lightSidewalk2 = new ColorDimmer3();
        private ColorDimmer3 lightSidewalk3 = new ColorDimmer3();
        private ColorDimmer3 lightSidewalk4 = new ColorDimmer3();

        private ColorDimmer3 light5_2 = new ColorDimmer3();
        //private ColorDimmer light5_2;
        //private ColorDimmer light5_3;
        //private ColorDimmer light5_4;
        //private ColorDimmer light5_5;
        //private ColorDimmer light5_6;
        //private ColorDimmer light5_7;
        //private ColorDimmer light5_8;
        //private ColorDimmer light5_9;
        //private ColorDimmer light5_10;
        //private ColorDimmer light5_11;
        //private ColorDimmer light5_12;
        //private ColorDimmer light5_13;
        //private ColorDimmer light5_14;
        //private ColorDimmer light5_15;
        //private ColorDimmer light5_16;

        private VirtualPixel1D allPixels;

        private DigitalInput2 testButton = new DigitalInput2();
        private Import.LorImport2 lorImport = new Import.LorImport2();


        public LORSceneCarol(IEnumerable<string> args)
        {
            allPixels = new VirtualPixel1D("All Pixels", 80);
            allPixels.SetAll(Color.White, 0);

            lorImport.LoadFromFile(@"..\..\..\Test Files\David Foster - Carol of the Bells.lms");

            lorImport.MapDevice("E - 1", lightTest1);
            lorImport.MapDevice("D# - 2", lightTest2);
            lorImport.MapDevice("D - 3", lightTest3);
            lorImport.MapDevice("C# - 4", lightTest4);
            lorImport.MapDevice("C - 5", lightTest5);
            lorImport.MapDevice("B - 6", lightTest6);
            lorImport.MapDevice("A# - 7", lightTest7);
            lorImport.MapDevice("A - 8", lightTest8);

            lorImport.MapDevice("E - 9", lightRoof1);
            lorImport.MapDevice("D# - 10", lightRoof2);
            lorImport.MapDevice("D - 11", lightRoof3);
            lorImport.MapDevice("C# - 12", lightRoof4);

/*            lorImport.MapDevice("Sidewalk 1", lightSidewalk1);
            lorImport.MapDevice("Sidewalk 2", lightSidewalk2);
            lorImport.MapDevice("Sidewalk 3", lightSidewalk3);
            lorImport.MapDevice("Sidewalk 4", lightSidewalk4);
            lorImport.MapDevice("Sidewalk 5", lightSidewalk1);
            lorImport.MapDevice("Sidewalk 6", lightSidewalk2);
            lorImport.MapDevice("Sidewalk 7", lightSidewalk3);
            lorImport.MapDevice("Sidewalk 8", lightSidewalk4);

            lorImport.MapDevice("Arch 1", light5_2);
*/

            lorImport.Prepare();
            lorImport.Dump();


            waveSource = CodecFactory.Instance.GetCodec(@"C:\Projects\Other\ChristmasSounds\trk\09 Carol of the Bells (Instrumental).wav");

            soundOut.Initialize(waveSource);


            //            light5_1 = lorImport.MapDevice(1, 1, name => new StrobeColorDimmer(name));


            /*
                        light1_1 = lorImport.MapDevice(1, 1, name => new StrobeColorDimmer(name));
                        light1_2 = lorImport.MapDevice(1, 2, name => new StrobeColorDimmer(name));
                        light1_3 = lorImport.MapDevice(1, 3, name => new StrobeColorDimmer(name));
                        light1_4 = lorImport.MapDevice(1, 4, name => new StrobeColorDimmer(name));
                        light1_5 = lorImport.MapDevice(1, 5, name => new StrobeColorDimmer(name));
                        light1_6 = lorImport.MapDevice(1, 6, name => new StrobeColorDimmer(name));
                        light1_7 = lorImport.MapDevice(1, 7, name => new StrobeColorDimmer(name));
                        light1_8 = lorImport.MapDevice(1, 8, name => new StrobeColorDimmer(name));
                        light1_9 = lorImport.MapDevice(1, 9, name => new StrobeColorDimmer(name));
                        light1_10 = lorImport.MapDevice(1, 10, name => new StrobeColorDimmer(name));
                        light1_11 = lorImport.MapDevice(1, 11, name => new StrobeColorDimmer(name));
                        light1_12 = lorImport.MapDevice(1, 12, name => new StrobeColorDimmer(name));
                        light1_13 = lorImport.MapDevice(1, 13, name => new StrobeColorDimmer(name));
                        light1_14 = lorImport.MapDevice(1, 14, name => new StrobeColorDimmer(name));
                        light1_15 = lorImport.MapDevice(1, 15, name => new StrobeColorDimmer(name));
                        light1_16 = lorImport.MapDevice(1, 16, name => new StrobeColorDimmer(name));

                        light2_1 = lorImport.MapDevice(2, 1, name => new StrobeColorDimmer(name));
                        light2_2 = lorImport.MapDevice(2, 2, name => new StrobeColorDimmer(name));
                        light2_3 = lorImport.MapDevice(2, 3, name => new StrobeColorDimmer(name));
                        light2_4 = lorImport.MapDevice(2, 4, name => new StrobeColorDimmer(name));
                        light2_5 = lorImport.MapDevice(2, 5, name => new StrobeColorDimmer(name));
                        light2_6 = lorImport.MapDevice(2, 6, name => new StrobeColorDimmer(name));
                        light2_7 = lorImport.MapDevice(2, 7, name => new StrobeColorDimmer(name));
                        light2_8 = lorImport.MapDevice(2, 8, name => new StrobeColorDimmer(name));
                        light2_9 = lorImport.MapDevice(2, 9, name => new StrobeColorDimmer(name));
                        light2_10 = lorImport.MapDevice(2, 10, name => new StrobeColorDimmer(name));
                        light2_11 = lorImport.MapDevice(2, 11, name => new StrobeColorDimmer(name));
                        light2_12 = lorImport.MapDevice(2, 12, name => new StrobeColorDimmer(name));
                        light2_13 = lorImport.MapDevice(2, 13, name => new StrobeColorDimmer(name));
                        light2_14 = lorImport.MapDevice(2, 14, name => new StrobeColorDimmer(name));
                        light2_15 = lorImport.MapDevice(2, 15, name => new StrobeColorDimmer(name));
                        light2_16 = lorImport.MapDevice(2, 16, name => new StrobeColorDimmer(name));

                        light3_1 = lorImport.MapDevice(3, 1, name => new StrobeColorDimmer(name));
                        light3_2 = lorImport.MapDevice(3, 2, name => new StrobeColorDimmer(name));
                        light3_3 = lorImport.MapDevice(3, 3, name => new StrobeColorDimmer(name));
                        light3_4 = lorImport.MapDevice(3, 4, name => new StrobeColorDimmer(name));
                        light3_5 = lorImport.MapDevice(3, 5, name => new StrobeColorDimmer(name));
                        light3_6 = lorImport.MapDevice(3, 6, name => new StrobeColorDimmer(name));
                        light3_7 = lorImport.MapDevice(3, 7, name => new StrobeColorDimmer(name));
                        light3_8 = lorImport.MapDevice(3, 8, name => new StrobeColorDimmer(name));
                        light3_9 = lorImport.MapDevice(3, 9, name => new StrobeColorDimmer(name));
                        light3_10 = lorImport.MapDevice(3, 10, name => new StrobeColorDimmer(name));
                        light3_11 = lorImport.MapDevice(3, 11, name => new StrobeColorDimmer(name));
                        light3_12 = lorImport.MapDevice(3, 12, name => new StrobeColorDimmer(name));
                        light3_13 = lorImport.MapDevice(3, 13, name => new StrobeColorDimmer(name));
                        light3_14 = lorImport.MapDevice(3, 14, name => new StrobeColorDimmer(name));
                        light3_15 = lorImport.MapDevice(3, 15, name => new StrobeColorDimmer(name));
                        light3_16 = lorImport.MapDevice(3, 16, name => new StrobeColorDimmer(name));

                        light4_1 = lorImport.MapDevice(4, 1, name => new StrobeColorDimmer(name));
                        light4_2 = lorImport.MapDevice(4, 2, name => new StrobeColorDimmer(name));
                        light4_3 = lorImport.MapDevice(4, 3, name => new StrobeColorDimmer(name));
                        light4_4 = lorImport.MapDevice(4, 4, name => new StrobeColorDimmer(name));
                        light4_5 = lorImport.MapDevice(4, 5, name => new StrobeColorDimmer(name));
                        light4_6 = lorImport.MapDevice(4, 6, name => new StrobeColorDimmer(name));
                        light4_7 = lorImport.MapDevice(4, 7, name => new StrobeColorDimmer(name));
                        light4_8 = lorImport.MapDevice(4, 8, name => new StrobeColorDimmer(name));
                        light4_9 = lorImport.MapDevice(4, 9, name => new StrobeColorDimmer(name));
                        light4_10 = lorImport.MapDevice(4, 10, name => new StrobeColorDimmer(name));
                        light4_11 = lorImport.MapDevice(4, 11, name => new StrobeColorDimmer(name));
                        light4_12 = lorImport.MapDevice(4, 12, name => new StrobeColorDimmer(name));
                        light4_13 = lorImport.MapDevice(4, 13, name => new StrobeColorDimmer(name));
                        light4_14 = lorImport.MapDevice(4, 14, name => new StrobeColorDimmer(name));
                        light4_15 = lorImport.MapDevice(4, 15, name => new StrobeColorDimmer(name));
                        light4_16 = lorImport.MapDevice(4, 16, name => new StrobeColorDimmer(name));

                        light5_1 = lorImport.MapDevice(5, 1, name => new StrobeColorDimmer(name));
                        light5_2 = lorImport.MapDevice(5, 2, name => new StrobeColorDimmer(name));
                        light5_3 = lorImport.MapDevice(5, 3, name => new StrobeColorDimmer(name));
                        light5_4 = lorImport.MapDevice(5, 4, name => new StrobeColorDimmer(name));
                        light5_5 = lorImport.MapDevice(5, 5, name => new StrobeColorDimmer(name));
                        light5_6 = lorImport.MapDevice(5, 6, name => new StrobeColorDimmer(name));
                        light5_7 = lorImport.MapDevice(5, 7, name => new StrobeColorDimmer(name));
                        light5_8 = lorImport.MapDevice(5, 8, name => new StrobeColorDimmer(name));
                        light5_9 = lorImport.MapDevice(5, 9, name => new StrobeColorDimmer(name));
                        light5_10 = lorImport.MapDevice(5, 10, name => new StrobeColorDimmer(name));
                        light5_11 = lorImport.MapDevice(5, 11, name => new StrobeColorDimmer(name));
                        light5_12 = lorImport.MapDevice(5, 12, name => new StrobeColorDimmer(name));
                        light5_13 = lorImport.MapDevice(5, 13, name => new StrobeColorDimmer(name));
                        light5_14 = lorImport.MapDevice(5, 14, name => new StrobeColorDimmer(name));
                        light5_15 = lorImport.MapDevice(5, 15, name => new StrobeColorDimmer(name));
                        light5_16 = lorImport.MapDevice(5, 16, name => new StrobeColorDimmer(name));
            */

            /*TEST            light5_1 = lorImport.MapDevice(new Import.LorImport.UnitCircuit(5, 1), name => new StrobeColorDimmer(name));

                        for (int unit = 1; unit <= 5; unit++)
                        {
                            for (int circuit = 1; circuit <= 16; circuit++)
                            {
                                int pixelPos = (unit - 1) * 16 + circuit - 1;

                                var pixel = lorImport.MapDevice(new Import.LorImport.UnitCircuit(unit, circuit), name => new SinglePixel(name, allPixels, pixelPos));

            //FIXME                    var color = lorImport.GetChannelColor(unit, circuit);
            //FIXME                    allPixels.SetColor(pixelPos, color, 0);

                                log.Debug("Mapping unit {0}  circuit {1} to pixel {2} [{3}]", unit, circuit, pixelPos, pixel.Name);
                            }
                        }
            */
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
