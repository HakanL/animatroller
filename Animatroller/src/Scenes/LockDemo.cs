using System;
using System.Collections.Generic;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Expander = Animatroller.Framework.Expander;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class LockDemo : BaseScene
    {
        Expander.AcnStream acnOutput = new Expander.AcnStream();

        ColorDimmer3 lightA = new ColorDimmer3();
        ColorDimmer3 lightB = new ColorDimmer3();

        GroupDimmer lightGroup = new GroupDimmer();

        DigitalInput2 testButton = new DigitalInput2();
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();

        Controller.Subroutine sub = new Controller.Subroutine();

        public LockDemo(IEnumerable<string> args)
        {
            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            lightGroup.Add(lightA, lightB);

            acnOutput.Connect(new Physical.SmallRGBStrobe(lightA, 1), 20);
            acnOutput.Connect(new Physical.SmallRGBStrobe(lightB, 2), 20);

            lightA.SetOnlyColor(Color.Red);
            lightB.SetOnlyColor(Color.Blue);

            sub
                .LockWhenRunning(lightA, lightB)
                .RunAction(i =>
                {
                    lightA.Brightness = 1.0;
                    i.WaitFor(S(0.5));

                    lightB.Brightness = 0.5;
                    i.WaitFor(S(0.5));

                    Exec.MasterEffect.Fade(lightA, 1.0, 0.0, 3000);
                });

            // Run
            testButton.Output.Subscribe(value =>
            {
                if (value)
                {
                    log.Info("Button pressed!");

                    sub.Run();
                }
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
