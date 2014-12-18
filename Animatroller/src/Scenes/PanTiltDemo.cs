using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
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
        Expander.AcnStream acnOutput = new Expander.AcnStream();

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
            acnOutput.Connect(new Physical.MonopriceMovingHeadLight12chn(lightA, 200), 54);

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

            button2.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        double[] testListP = new double[1000];
                        for (int i = 0; i < testListP.Length; i++)
                            testListP[i] = 200 * Math.Sin(Math.PI * i / testListP.Length);

                        double[] testListT = new double[1000];
                        for (int i = 0; i < testListP.Length; i++)
                            testListT[i] = 270 * Math.Sin(Math.PI * i / testListP.Length);

                        var token = lightA.TakeControl();

                        lightA.SetOnlyColor(Color.Violet);

                        var tasks = new List<Task>();

                        tasks.Add(Exec.MasterEffect.Fade(lightA.GetBrightnessObserver(), 0.0, 1.0, 2000));

                        tasks.Add(Exec.MasterEffect.Custom(testListP, lightA.GetPanObserver(), 5000, 1));

                        tasks.Add(Exec.MasterEffect.Custom(testListT, lightA.GetTiltObserver(), 10000, 3));

                        Task.WhenAll(tasks.ToArray())
                            .ContinueWith(_ => token.Dispose());
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
