using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Expander = Animatroller.Framework.Expander;

namespace Animatroller.Scenes
{
    internal class ExpanderDemo2 : BaseScene
    {
        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance();
//        Expander.MonoExpanderInstance expander1 = new Expander.MonoExpanderInstance();
//        Expander.MonoExpanderInstance expander2 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(8899);
        AudioPlayer audio1 = new AudioPlayer();
        AudioPlayer audio2 = new AudioPlayer();
        AnalogInput3 masterVolume = new AnalogInput3(persistState: true, defaultValue: 1.0);

        DigitalInput2 in1 = new DigitalInput2();
        DigitalOutput2 out1 = new DigitalOutput2();

        public ExpanderDemo2(IEnumerable<string> args)
        {
            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                    expanderServer.ExpanderSharedFiles = parts[1];
            }

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);
            //            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expander1);
            //            expanderServer.AddInstance("10520fdcf14d47cba31da8b6e05d01d8", expander2);

            expanderLocal.DigitalInputs[6].Connect(in1);
            expanderLocal.DigitalOutputs[7].Connect(out1);
            expanderLocal.Connect(audio1);
            //            expander2.Connect(audio2);

            masterVolume.ConnectTo(Exec.MasterVolume);

            in1.Output.Subscribe(x =>
            {
                if (x)
//                    audio2.PlayTrack("02. Frozen - Do You Want to Build a Snowman.wav");
                      audio1.PlayEffect("WarmHugs.wav");
                        //                    audio2.PlayTrack("08 Feel the Light.wav");
                        //                    audioLocal.PlayEffect("WarmHugs.wav");

                        out1.SetValue(x);
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
