using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Expander = Animatroller.Framework.Expander;

namespace Animatroller.SceneRunner
{
    internal class ExpanderDemo : BaseScene
    {
        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander1 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(8088);
        AudioPlayer audioLocal = new AudioPlayer();

        DigitalInput2 in1 = new DigitalInput2();
        DigitalOutput2 out1 = new DigitalOutput2();

        public ExpanderDemo(IEnumerable<string> args)
        {
            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                    expanderServer.ExpanderSharedFiles = parts[1];
            }

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);
            expanderServer.AddInstance("ca52c6aeb9964a85b83db01c74c5df80", expander1);
            
            expander1.DigitalInputs[6].Connect(in1);
            expander1.DigitalOutputs[7].Connect(out1);
            expander1.Connect(audioLocal);

            in1.Output.Subscribe(x =>
            {
                if (x)
                    audioLocal.PlayEffect("scream.wav");

                out1.Value = x;
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
