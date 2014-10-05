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
        public class PhysicalDevices
        {
            public Physical.SmallRGBStrobe SmallLED;
            public Physical.GenericDimmer CatAir;
            public Physical.GenericDimmer CatLight;
        }

        private PhysicalDevices p;

        private Expander.MidiInput2 midiInput = new Expander.MidiInput2();
        private ColorDimmer2 testLight1 = new ColorDimmer2("Test 1");
        private Dimmer2 testLight2 = new Dimmer2("Test 2");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest1 = new DigitalInput2("Test 1");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest2 = new DigitalInput2("Test 2");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest3 = new DigitalInput2("Test 3");
        private AnalogInput2 inputBrightness = new AnalogInput2("Brightness");
        private AnalogInput2 inputH = new AnalogInput2("Hue", true);
        private AnalogInput2 inputS = new AnalogInput2("Saturation", true);
        private AnalogInput2 inputV = new AnalogInput2("Value", true);
        private Expander.AcnStream acnOutput = new Expander.AcnStream();
        private DigitalOutput2 catAir = new DigitalOutput2();
        private DigitalOutput2 catLight = new DigitalOutput2();

        public TestMidi1(IEnumerable<string> args)
        {
            p = new PhysicalDevices
            {
                SmallLED = new Physical.SmallRGBStrobe(testLight1, 1),
                CatAir = new Physical.GenericDimmer(catAir, 10),
                CatLight = new Physical.GenericDimmer(catLight, 11)
            };

            inputBrightness.ConnectTo(testLight1.InputBrightness);
            inputBrightness.ConnectTo(testLight2.InputBrightness);

            acnOutput.Connect(p.SmallLED, 20);
            acnOutput.Connect(p.CatAir, 20);
            acnOutput.Connect(p.CatLight, 20);

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

            midiInput.Controller(1, 1).Controls(inputBrightness.Control);
            midiInput.Controller(1, 2).Controls(inputH.Control);
            midiInput.Controller(1, 3).Controls(inputS.Control);
            midiInput.Controller(1, 4).Controls(inputV.Control);

            midiInput.Note(1, 36).Controls(buttonTest1.Control);
            midiInput.Note(1, 37).Controls(buttonTest2.Control);
            midiInput.Note(1, 38).Controls(buttonTest3.Control);


            buttonTest2.Output.Subscribe(catAir.InputPower);
            buttonTest3.Output.Subscribe(catLight.InputPower);

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
        }

        public override void Start()
        {
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
