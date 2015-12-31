using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Expander = Animatroller.Framework.Expander;
using Physical = Animatroller.Framework.PhysicalDevice;
using Effect = Animatroller.Framework.Effect;

namespace Animatroller.SceneRunner
{
    internal class LockDemo : BaseScene
    {
        Expander.AcnStream acnOutput = new Expander.AcnStream();

        ColorDimmer3 lightA = new ColorDimmer3();
        ColorDimmer3 lightB = new ColorDimmer3();
        VirtualPixel1D3 pixelsRoofEdge = new VirtualPixel1D3(150);

        GroupDimmer lightGroup = new GroupDimmer();

        DigitalInput2 button1 = new DigitalInput2();
        DigitalInput2 button2 = new DigitalInput2();
        DigitalInput2 button3 = new DigitalInput2();
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();
        AnalogInput3 testLightA = new AnalogInput3();
        AnalogInput3 testLightB = new AnalogInput3();

        IControlToken testLock = null;

        Controller.Subroutine sub = new Controller.Subroutine();
        Effect.PopOut2 popOut = new Effect.PopOut2(S(1.2));

        public LockDemo(IEnumerable<string> args)
        {
            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            lightGroup.Add(lightA, lightB);

            acnOutput.Connect(new Physical.SmallRGBStrobe(lightA, 1), 20);
            acnOutput.Connect(new Physical.SmallRGBStrobe(lightB, 10), 20);
            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 0, 50), 26, 1);
            acnOutput.Connect(new Physical.Pixel1D(pixelsRoofEdge, 50, 100), 25, 1);

            testLightA.ConnectTo(x => lightA.SetBrightness(x));
            testLightA.ConnectTo(x => pixelsRoofEdge.SetBrightness(x));
            testLightB.ConnectTo(x => lightB.SetBrightness(x));

            lightA.SetColor(Color.Red, null);
            lightB.SetColor(Color.Blue, null);

            pixelsRoofEdge.SetColor(Color.Green, 0.6);

            popOut.ConnectTo(lightA);
            popOut.ConnectTo(pixelsRoofEdge);

            sub
                .LockWhenRunning(lightA, lightB)
                .RunAction(i =>
                {
                    lightA.SetBrightness(1.0, i.Token);
                    i.WaitFor(S(2.5));

                    lightB.SetBrightness(0.5, i.Token);
                    i.WaitFor(S(2.5));

                    Exec.MasterEffect.Fade(lightGroup, 1.0, 0.0, 3000, token: i.Token);

                    i.WaitFor(S(1));

                    using (var takeOver = lightGroup.TakeControl(5))
                    {
                        lightGroup.SetBrightness(1, takeOver);
                        i.WaitFor(S(1));
                    }

                    lightGroup.SetBrightness(1, i.Token);

                    i.WaitFor(S(2));
                });

            button1.Output.Subscribe(value =>
            {
                if (value)
                {
                    log.Info("Button 1 pressed!");

                    sub.Run();
                }
            });

            button2.Output.Subscribe(value =>
            {
                if (value)
                {
                    log.Info("Button 2 pressed!");

                    if (testLock != null)
                    {
                        testLock.Dispose();
                        testLock = null;
                    }
                    else
                        testLock = lightA.TakeControl();
                }
            });

            button3.Output.Subscribe(value =>
            {
                if (value)
                {
                    log.Info("Button 3 pressed!");

                    popOut.Pop(color: Color.Purple, sweepDuration: S(2));

                    Thread.Sleep(500);

                    testLightA.Value = random.NextDouble();
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
