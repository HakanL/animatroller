using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Reactive;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class TestMidi1 : BaseScene
    {
        private Expander.MidiInput2 midiInput = new Expander.MidiInput2();
        private ColorDimmer testLight1 = new ColorDimmer("Test 1");
        private Dimmer2 testLight2 = new Dimmer2("Test 2");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest1 = new DigitalInput2("Test 1");
        private AnalogInput2 inputBrightness = new AnalogInput2("Brightness");
        private AnalogInput2 inputH = new AnalogInput2("Hue", true);
        private AnalogInput2 inputS = new AnalogInput2("Saturation", true);
        private AnalogInput2 inputV = new AnalogInput2("Value", true);


        public TestMidi1(IEnumerable<string> args)
        {
            midiInput.Controller(1, 1).Controls(inputBrightness.Control);

            midiInput.Note(1, 36).Controls(buttonTest1.Control);

            inputBrightness.ConnectTo(testLight2.Brightness);

            buttonTest1.Output.Subscribe(x =>
            {
                if (x)
                {
                    testLight1.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));
                }
                else
                {
                    if (testLight1.Brightness > 0)
                        testLight1.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
                }
            });

            inputH.Output.Subscribe(x =>
            {
                testLight1.SetOnlyColor(HSV.ColorFromHSV(x.Value.GetByteScale(), inputS.Value, inputV.Value));
            });

            inputS.Output.Subscribe(x =>
            {
                testLight1.SetOnlyColor(HSV.ColorFromHSV(inputH.Value.GetByteScale(), x.Value, inputV.Value));
            });

            inputV.Output.Subscribe(x =>
            {
                testLight1.SetOnlyColor(HSV.ColorFromHSV(inputH.Value.GetByteScale(), inputS.Value, x.Value));
            });
        }

        public override void Start()
        {

            //inputBrightness.ValueChanged += (sender, e) =>
            //    {
            //        testLight1.SetBrightness(e.NewBrightness);
            //    };

        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
