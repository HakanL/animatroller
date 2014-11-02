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
    internal class SimpleTest1 : BaseScene
    {
        private ColorDimmer testLight1 = new ColorDimmer("Test 1");
        private Dimmer2 testLight2 = new Dimmer2("Test 2");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput buttonTest1 = new DigitalInput("Test 1");
        private AnalogInput inputBrightness = new AnalogInput("Brightness");
        private AnalogInput inputH = new AnalogInput("Hue", true);
        private AnalogInput inputS = new AnalogInput("Saturation", true);
        private AnalogInput inputV = new AnalogInput("Value", true);


        public SimpleTest1(IEnumerable<string> args)
        {
            inputBrightness.ConnectTo(testLight2.InputBrightness);
            //testInput1.Subscribe()
            //    .Subscribe(x =>
            //    {
            //        testLight1.SetBrightness(x.Value);
            //    });
        }

        public void WireUp(Expander.MidiInput port)
        {
            //port.AddDigitalInput_Note(buttonTest1, 0, 36);
            //port.AddAnalogInput_Note(testInput1, 0, 37);
            //port.AddAnalogInput_Note(testInput1, 1, 37);
            //port.AddAnalogInput_Controller(testInput1, 0, 1);
            //port.AddAnalogInput_Controller(inputH, 0, 2);
            //port.AddAnalogInput_Controller(inputS, 0, 3);
            //port.AddAnalogInput_Controller(inputV, 0, 4);
        }

        public override void Start()
        {
            buttonTest1.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    testLight1.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));
                }
                else
                {
                    if (testLight1.Brightness > 0)
                        testLight1.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
                }
            };

            inputBrightness.ValueChanged += (sender, e) =>
                {
//                    testLight1.SetBrightness(e.NewBrightness);
                };

            inputH.ValueChanged += (sender, e) =>
            {
                testLight1.SetOnlyColor(HSV.ColorFromHSV(e.NewBrightness.GetByteScale(), inputS.Value, inputV.Value));
            };

            inputS.ValueChanged += (sender, e) =>
            {
                testLight1.SetOnlyColor(HSV.ColorFromHSV(inputH.Value.GetByteScale(), e.NewBrightness, inputV.Value));
            };

            inputV.ValueChanged += (sender, e) =>
            {
                testLight1.SetOnlyColor(HSV.ColorFromHSV(inputH.Value.GetByteScale(), inputS.Value, e.NewBrightness));
            };
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
