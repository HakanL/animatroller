using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Import = Animatroller.Framework.Import;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;
using Controller = Animatroller.Framework.Controller;
using System.Threading.Tasks;

namespace Animatroller.SceneRunner
{
    internal class LockDemo : BaseScene
    {
        private Expander.AcnStream acnOutput = new Expander.AcnStream();

        private ColorDimmer3 lightA = new ColorDimmer3();
        private ColorDimmer3 lightB = new ColorDimmer3();

        private GroupDimmer lightGroup = new GroupDimmer();

        private DigitalInput2 testButton = new DigitalInput2();

        private Controller.Subroutine sub = new Controller.Subroutine();

        public LockDemo(IEnumerable<string> args)
        {
            lightGroup.Add(lightA, lightB);

            acnOutput.Connect(new Physical.SmallRGBStrobe(lightA, 1), 20);
            acnOutput.Connect(new Physical.SmallRGBStrobe(lightB, 2), 20);

            sub.LockWhenRunning(lightA, lightB);

            sub.RunAction(i =>
            {
                lightA.Brightness = 1.0;
                i.WaitFor(S(0.5));

                lightB.Brightness = 0.5;
                i.WaitFor(S(0.5));

                Exec.MasterFader.Fade(lightA, 1.0, 0.0, 3000);
            });
        }

        public override void Start()
        {
            // Set color
            testButton.Output.Subscribe(button =>
            {
                if (button)
                {
                    log.Info("Button press!");

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
