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
    internal class Halloween2014 : BaseScene
    {
        public class PhysicalDevices
        {
            public Physical.SmallRGBStrobe SmallLED;
            public Physical.GenericDimmer CatAir;
            public Physical.GenericDimmer CatLight;
        }

        private PhysicalDevices p;

        private Expander.MidiInput2 midiInput = new Expander.MidiInput2();
        private Expander.OscServer oscServer = new Expander.OscServer();
        private AudioPlayer audioCat = new AudioPlayer();
        private Expander.Raspberry raspberryCat = new Expander.Raspberry("192.168.240.115:5005", 3333);

        private ColorDimmer2 testLight1 = new ColorDimmer2("Test 1");
        private Dimmer2 testLight2 = new Dimmer2("Test 2");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest1 = new DigitalInput2("Test 1");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest2 = new DigitalInput2("Test 2");
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonTest3 = new DigitalInput2("Test 3");
        private DigitalInput2 catMotion = new DigitalInput2();
        private AnalogInput2 inputBrightness = new AnalogInput2("Brightness");
        private AnalogInput2 inputH = new AnalogInput2("Hue", true);
        private AnalogInput2 inputS = new AnalogInput2("Saturation", true);
        private AnalogInput2 inputV = new AnalogInput2("Value", true);
        private Expander.AcnStream acnOutput = new Expander.AcnStream();
        private DigitalOutput2 catAir = new DigitalOutput2();
        private DigitalOutput2 catLights = new DigitalOutput2();

        private OperatingHours2 hoursSmall = new OperatingHours2("Hours Small");
        private OperatingHours2 hoursFull = new OperatingHours2("Hours Full");

        private Controller.Sequence catSeq = new Controller.Sequence("Cat Sequence");

        public Halloween2014(IEnumerable<string> args)
        {
            p = new PhysicalDevices
            {
                SmallLED = new Physical.SmallRGBStrobe(testLight1, 1),
                CatLight = new Physical.GenericDimmer(catLights, 10),
                CatAir = new Physical.GenericDimmer(catAir, 11)
            };

            hoursSmall.AddRange("5:00 pm", "9:00 pm");
            hoursFull.AddRange("5:00 pm", "9:00 pm");

            raspberryCat.DigitalInputs[4].Connect(catMotion, true);
            raspberryCat.Connect(audioCat);

            inputBrightness.ConnectTo(testLight1.InputBrightness);
            inputBrightness.ConnectTo(testLight2.InputBrightness);

            acnOutput.Connect(p.SmallLED, 20);
            acnOutput.Connect(p.CatAir, 20);
            acnOutput.Connect(p.CatLight, 20);

            buttonTest3.Control.Subscribe(x =>
                {
                    if (x)
                        audioCat.PlayEffect("348 Spider Hiss");
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

            midiInput.Controller(1, 1).Controls(inputBrightness.Control);
            midiInput.Controller(1, 2).Controls(inputH.Control);
            midiInput.Controller(1, 3).Controls(inputS.Control);
            midiInput.Controller(1, 4).Controls(inputV.Control);

            midiInput.Note(1, 36).Controls(buttonTest1.Control);
            midiInput.Note(1, 37).Controls(buttonTest2.Control);
            midiInput.Note(1, 38).Controls(buttonTest3.Control);
            midiInput.Note(1, 39).Controls(catMotion.Control);


            buttonTest2.Output.Subscribe(catAir.InputPower);
            catMotion.Output.Subscribe(catLights.InputPower);

            catMotion.Output.Subscribe(x =>
                {
                    if (x && hoursSmall.IsOpen)
                        Executor.Current.Execute(catSeq);
                });

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

            hoursSmall.Output.Subscribe(catAir.InputPower);
        }

        public override void Start()
        {
            catSeq.WhenExecuted
                .Execute(instance =>
                {
                    var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                    var random = new Random();

                    catLights.Power = true;

                    while (true)
                    {
                        switch (random.Next(4))
                        {
                            case 0:
                                audioCat.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.0));
                                break;
                            case 1:
                                audioCat.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                            case 2:
                                audioCat.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.5));
                                break;
                            case 3:
                                audioCat.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(1.5));
                                break;
                            default:
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                        }

                        instance.CancelToken.ThrowIfCancellationRequested();

                        if (maxRuntime.Elapsed.TotalSeconds > 10)
                            break;
                    }
                })
                    .TearDown(() =>
                    {
                        catLights.Power = false;
                    });
        }

        public override void Run()
        {
            hoursSmall.SetForced(false);
        }

        public override void Stop()
        {
        }
    }
}
