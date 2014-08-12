using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
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
    internal class TestMidi1 : BaseScene, ISceneRequiresMidiInput, ISceneSupportsSimulator
    {
        private ColorDimmer testLight1;
        private DigitalInput buttonTest1;
        private AnalogInput testInput1;
        private AnalogInput inputH;
        private AnalogInput inputS;
        private AnalogInput inputV;


        public TestMidi1(IEnumerable<string> args)
        {
            buttonTest1 = new DigitalInput("Test 1");
            testLight1 = new ColorDimmer("Test 1");
            testInput1 = new AnalogInput("Test 1");
            inputH = new AnalogInput("Hue");
            inputS = new AnalogInput("Saturation");
            inputV = new AnalogInput("Value");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_FlipFlop(buttonTest1);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.MidiInput port)
        {
            port.AddDigitalInput_Note(buttonTest1, 36);
            port.AddAnalogInput_Note(testInput1, 37);
            port.AddAnalogInput_Controller(testInput1, 1);
            port.AddAnalogInput_Controller(inputH, 2);
            port.AddAnalogInput_Controller(inputS, 3);
            port.AddAnalogInput_Controller(inputV, 4);
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

            testInput1.ValueChanged += (sender, e) =>
                {
                    testLight1.SetBrightness(e.NewBrightness);
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
