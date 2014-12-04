using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class Xmas2014 : BaseScene
    {
        Expander.AcnStream acnOutput = new Expander.AcnStream();
        Expander.OscServer oscServer = new Expander.OscServer(8000);

        OperatingHours2 hours = new OperatingHours2();
        VirtualPixel1D allPixels;
        DigitalInput2 buttonTest = new DigitalInput2();
        DigitalInput2 buttonTest2 = new DigitalInput2();

        Dimmer3 lightHat1 = new Dimmer3();
        Dimmer3 lightHat2 = new Dimmer3();
        Dimmer3 lightHat3 = new Dimmer3();
        Dimmer3 lightHat4 = new Dimmer3();
        Dimmer3 lightStar = new Dimmer3();
        Dimmer3 snowmanKaggen = new Dimmer3();
        DigitalOutput2 airSnowman = new DigitalOutput2();
        Dimmer3 lightSnowman = new Dimmer3();
        DigitalOutput2 airSanta = new DigitalOutput2();
        Dimmer3 lightSanta = new Dimmer3();
        DigitalOutput2 reindeer = new DigitalOutput2();
        DigitalOutput2 packages = new DigitalOutput2();

        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);

        Controller.Sequence testSeq;
        Controller.Sequence candyCane;
        Controller.Sequence laserSeq;

        public Xmas2014(IEnumerable<string> args)
        {
            hours.AddRange("5:00 pm", "9:00 pm");
//            hours.SetForced(true);

            hours.Output.Log("Hours inside");

            reindeer.Power = true;
            airSnowman.Power = true;
            airSanta.Power = true;
            packages.Power = true;

            hours
/*                .ControlsMasterPower(lightHat1)
                .ControlsMasterPower(lightHat2)
                .ControlsMasterPower(lightHat3)
                .ControlsMasterPower(lightHat4)*/
                .ControlsMasterPower(packages)
                .ControlsMasterPower(airSnowman)
                .ControlsMasterPower(airSanta)
                .ControlsMasterPower(reindeer);

            pulsatingEffect1.ConnectTo(lightStar.GetBrightnessObserver());

            hours.Output.Subscribe(pulsatingEffect1.InputRun);

            hours.Output.Subscribe(x =>
                {
                    lightHat1.Brightness = x ? 1.0 : 0.0;
                    lightHat2.Brightness = x ? 1.0 : 0.0;
                    lightHat3.Brightness = x ? 1.0 : 0.0;
                    lightHat4.Brightness = x ? 1.0 : 0.0;
                    snowmanKaggen.Brightness = x ? 1.0 : 0.0;
                    lightSnowman.Brightness = x ? 1.0 : 0.0;
                    lightSanta.Brightness = x ? 1.0 : 0.0;
                });

            lightSanta.SetOutputFilter(new Effect.Blackout());

            testSeq = new Controller.Sequence("Pulse");
            candyCane = new Controller.Sequence("Candy Cane");
            laserSeq = new Controller.Sequence("Laser");

            allPixels = new VirtualPixel1D(150);

            // WS2811
            acnOutput.Connect(new Physical.PixelRope(allPixels, 0, 50), 4, 1);
            acnOutput.Connect(new Physical.PixelRope(allPixels, 50, 100), 5, 1);
            // GECE
//            acnOutput.Connect(new Physical.PixelRope(allPixels, 100, 50), 2, 91);

            acnOutput.Connect(new Physical.GenericDimmer(reindeer, 10), 20);
            acnOutput.Connect(new Physical.GenericDimmer(airSnowman, 11), 20);
            acnOutput.Connect(new Physical.GenericDimmer(airSanta, 12), 20);
            acnOutput.Connect(new Physical.GenericDimmer(lightStar, 50), 20);

            acnOutput.Connect(new Physical.GenericDimmer(packages, 1), 21);
            acnOutput.Connect(new Physical.GenericDimmer(snowmanKaggen, 2), 21);

            acnOutput.Connect(new Physical.GenericDimmer(lightHat1, 1), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat2, 2), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat3, 3), 22);
            acnOutput.Connect(new Physical.GenericDimmer(lightHat4, 4), 22);

            acnOutput.Connect(new Physical.GenericDimmer(lightSanta, 1), 23);
            acnOutput.Connect(new Physical.GenericDimmer(lightSnowman, 2), 23);


            oscServer.RegisterActionSimple<double>("/1/faderA", (msg, data) =>
            {
                lightSnowman.Brightness = data;
            });

            oscServer.RegisterActionSimple<double>("/1/faderD", (msg, data) =>
            {
                Exec.Blackout.OnNext(data);
            });
        }

        private void TestAllPixels(Color color, double brightness, TimeSpan delay)
        {
            allPixels.SetAll(color, brightness);
            System.Threading.Thread.Sleep(delay);
        }

        public override void Start()
        {
            testSeq
                .WhenExecuted
                .SetUp(() => allPixels.TurnOff())
                .Execute(instance =>
                {
                    allPixels.SetAllOnlyColor(Color.Orange);
                    allPixels.RunEffect(new Effect2.Pulse(0.0, 1.0), S(2.0))
                        .SetIterations(2)
                        .Wait();
                    allPixels.StopEffect();
                })
                .TearDown(() => 
                    {
                        allPixels.TurnOff();

                        Exec.Execute(candyCane);
                    });


            candyCane
                .WhenExecuted
                .SetUp(() => allPixels.TurnOff())
                .Execute(instance =>
                {
                    const int spacing = 4;

                    while (true)
                    {
                        for (int i = 0; i < spacing; i++)
                        {
                            allPixels.Inject((i % spacing) == 0 ? Color.Red : Color.White, 0.5);

                            instance.WaitFor(S(0.30), true);
                        }
                    }
                })
                .TearDown(() =>
                    {
                        allPixels.TurnOff();
                    });


            laserSeq
                .WhenExecuted
                .SetUp(() =>
                    {
                        allPixels.TurnOff();
                    })
                .Execute(instance =>
                {
                    var cb = new ColorBrightness[6];
                    cb[0] = new ColorBrightness(Color.Black, 1.0);
                    cb[1] = new ColorBrightness(Color.Red, 1.0);
                    cb[2] = new ColorBrightness(Color.Orange, 1.0);
                    cb[3] = new ColorBrightness(Color.Yellow, 1.0);
                    cb[4] = new ColorBrightness(Color.Blue, 1.0);
                    cb[5] = new ColorBrightness(Color.White, 1.0);

                    for (int i = -6; i < allPixels.Pixels; i++)
                    {
                        allPixels.SetColors(i, cb);
                        System.Threading.Thread.Sleep(25);
                    }

                    instance.WaitFor(S(1));
                })
                .TearDown(() =>
                    {
                        allPixels.TurnOff();

                        Exec.Execute(candyCane);
                    });


            // Test Button
            buttonTest.Output.Subscribe(x =>
            {
                if (!x)
                    return;

                Exec.Cancel(candyCane);

                allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();
                allPixels.SetAllOnlyColor(Color.Purple);
                allPixels.RunEffect(new Effect2.Fader(0.0, 1.0), S(2.0)).Wait();
                allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();

                allPixels.SetAllOnlyColor(Color.Orange);
                allPixels.RunEffect(new Effect2.Fader(0.0, 1.0), S(2.0)).Wait();

                allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();

                Exec.Execute(candyCane);
            });

            buttonTest2.Output.Subscribe(x =>
                {
                    if (!x)
                        return;

                    Exec.Cancel(candyCane);

                    Exec.Execute(laserSeq);

                    Exec.Sleep(S(2));
                });
        }

        public override void Run()
        {
            Exec.Execute(testSeq);
        }

        public override void Stop()
        {
            Exec.Cancel(candyCane);
            System.Threading.Thread.Sleep(200);
        }
    }
}
