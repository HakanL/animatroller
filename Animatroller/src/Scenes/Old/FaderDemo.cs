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
using System.Threading.Tasks;

namespace Animatroller.Scenes
{
    internal class FaderDemo : BaseScene
    {
        private Expander.AcnStream acnOutput = new Expander.AcnStream();

        private ColorDimmer3 lightA = new ColorDimmer3();
        private ColorDimmer3 lightB = new ColorDimmer3();

        private GroupDimmer lightGroup = new GroupDimmer();

        private DigitalInput2 testButton = new DigitalInput2();

        public FaderDemo(IEnumerable<string> args)
        {
            lightGroup.Add(lightA, lightB);

            acnOutput.Connect(new Physical.SmallRGBStrobe(lightA, 1), 20);
            acnOutput.Connect(new Physical.SmallRGBStrobe(lightB, 2), 20);

            // Set color
            testButton.Output.Subscribe(button =>
            {
                if (button)
                {
                    log.Info("Button press!");

                    // Test priority/control
                    //lightA.Brightness = 0.25;

                    //using (var control1 = lightA.TakeControl(1))
                    //{
                    //    lightA.Brightness = 0.33;

                    //    var observer1 = lightA.GetBrightnessObserver(control1);

                    //    observer1.OnNext(1.0);

                    //    using (var control2 = lightA.TakeControl(1))
                    //    {
                    //        var observer2 = lightA.GetBrightnessObserver(control2);

                    //        observer1.OnNext(0.5);
                    //        observer2.OnNext(0.75);
                    //    }
                    //}


                    lightA.Brightness = 0.2;

                    var control1 = lightA.TakeControl(1);
                    lightA.Brightness = 0.33;

                    var observer1 = lightA.GetBrightnessObserver();

                    // Should skip
                    observer1.OnNext(1.0);

                    var faderTask = Exec.MasterEffect.Fade(observer1, 0.0, 1.0, 5000);

                    Exec.Sleep(S(3));

                    Exec.MasterEffect.Fade(Exec.Blackout, 0.0, 1.0, 1000)
                        .ContinueWith(b =>
                        {
                            Task.Delay(2000).ContinueWith(c =>
                            {
                                Exec.Blackout.OnNext(0);
                            });
                        });

                    faderTask.ContinueWith(x =>
                    {
                        control1.Dispose();

                        Exec.MasterEffect.Fade(observer1, 1.0, 0.0, 5000);
                    });



                    //lightB.Brightness = 1.0;


                    //Exec.MasterShimmer.Shimmer(lightA, 0, 1.0, 1500);

                    //Task.Delay(500).ContinueWith(y =>
                    //    {
                    //        Exec.MasterFader.Fade(lightB, 1.0, 0.0, 1000);
                    //    });
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
