using System;
using System.Collections.Generic;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Expander = Animatroller.Framework.Expander;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class PanTiltDemo : BaseScene
    {
        //        Expander.AcnStream acnOutput = new Expander.AcnStream();

        MovingHead lightA = new MovingHead();
        ColorDimmer3 lightB = new ColorDimmer3();

        //        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 button1 = new DigitalInput2();
        DigitalInput2 button2 = new DigitalInput2();
        AnalogInput3 test1 = new AnalogInput3();

        Controller.CueList cueList = new Controller.CueList(iterations: 1);

        public PanTiltDemo(IEnumerable<string> args)
        {
            // Patch physical
            //            acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(light, 200), 20);

            // Logging
            lightA.OutputPan.Log("Pan");
            lightA.OutputTilt.Log("Tilt");

            // Build preset
            var preset1 = new Preset
            {
                Brightness = 0.5,
                Color = Color.Firebrick,
                Pan = 0,
                Tilt = 0
            };

            var preset2 = new Preset
            {
                Brightness = 1.0,
                Color = Color.Yellow,
                Pan = 300,
                Tilt = 200
            };

            // Cues
            cueList.AddCue(new Cue
            {
                Preset = preset1,
                FadeS = 4.0
            }, lightA, lightB);

            cueList.AddCue(new Cue
            {
                Preset = preset2,
                FadeS = 2.0
            }, lightA, lightB);

            cueList.AddCue(new Cue
                {
                    Color = Color.Green,
                    AddDevice = lightA,
                    FadeS = 2,
                    Trigger = Cue.Triggers.Follow,
                    TriggerTimeS = 6
                });

            // BO
            cueList.AddCue(new Cue
            {
                Intensity = 0,
                AddDevice = lightA,
                FadeS = 1,
                Pan = 0,
                Tilt = 0
            }, lightB);

            cueList.AddCue(new Cue
            {
                Preset = preset2,
                FadeS = 2.0
            }, lightA, lightB);


            cueList.CueCompleted.Subscribe(x =>
                {
                    log.Debug("Cue {0} processing time: {1:N0}", x.Item1, x.Item2);
                });

            // Inputs
            button1.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        cueList.Go();
                    }
                });

            test1.ConnectTo(lightA);
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
