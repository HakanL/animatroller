using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Expander = Animatroller.Framework.Expander;

namespace Animatroller.SceneRunner
{
    internal class ExpanderDemo : BaseScene
    {
        Expander.MonoExpander expanderLocal = new Expander.MonoExpander("127.0.0.1:5005", 3333);
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(8088);
        AudioPlayer audioLocal = new AudioPlayer();

        DigitalInput2 in1 = new DigitalInput2();
        DigitalOutput2 out1 = new DigitalOutput2();

        public ExpanderDemo(IEnumerable<string> args)
        {
            expanderLocal.DigitalInputs[6].Connect(in1);
            expanderLocal.DigitalOutputs[7].Connect(out1);
            expanderLocal.Connect(audioLocal);

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
