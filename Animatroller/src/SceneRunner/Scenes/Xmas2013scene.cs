using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
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
    internal class Xmas2013scene : BaseScene, ISceneRequiresAcnStream, ISceneSupportsSimulator
    {
        private OperatingHours hours;
        private VirtualPixel1D allPixels;
        private DigitalInput buttonTest;
        private DigitalInput buttonOverrideHours;
        private Dimmer lightStar;
        private Dimmer lightHat1;
        private Dimmer lightHat2;
        private Dimmer lightStairs1;

        private Effect.Pulsating pulsatingEffect1;
        private Effect.Flicker flickerEffect;
        private Controller.Sequence candyCane;
        private Controller.Sequence twinkleSeq;

        public Xmas2013scene(IEnumerable<string> args)
        {
            hours = new OperatingHours("Hours");

            lightStar = new Dimmer("Star");
            lightHat1 = new Dimmer("Hat 1");
            lightHat2 = new Dimmer("Hat 2");
            lightStairs1 = new Dimmer("Stair 1");

            pulsatingEffect1 = new Effect.Pulsating("Pulse FX 1", S(4), 0.4, 1.0, false);
            flickerEffect = new Effect.Flicker("Flicker", 0.5, 0.6, false);

            candyCane = new Controller.Sequence("Candy Cane");
            twinkleSeq = new Controller.Sequence("Twinkle");

            allPixels = new VirtualPixel1D("All Pixels", 100);

            buttonTest = new DigitalInput("Test");
            buttonOverrideHours = new DigitalInput("Override hours");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonTest);
            sim.AddDigitalInput_FlipFlop(buttonOverrideHours);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
            port.Connect(new Physical.PixelRope(allPixels, 0, 100), 4, 1);

            port.Connect(new Physical.GenericDimmer(lightStar, 1), 21);
            port.Connect(new Physical.GenericDimmer(lightHat1, 2), 21);
            port.Connect(new Physical.GenericDimmer(lightHat2, 3), 21);
            port.Connect(new Physical.GenericDimmer(lightStairs1, 24), 21);
        }

        private void TestAllPixels(Color color, double brightness, TimeSpan delay)
        {
            allPixels.SetAll(color, brightness);
            System.Threading.Thread.Sleep(delay);
        }

        public override void Start()
        {
            hours.AddRange("5:00 pm", "11:00 pm");

            buttonOverrideHours.ActiveChanged += (i, e) =>
                {
                    if (e.NewState)
                        hours.SetForced(true);
                    else
                        hours.SetForced(null);
                };

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

                            instance.WaitFor(S(0.2), true);
                        }
                    }
                })
                .TearDown(() =>
                    {
                        allPixels.TurnOff();
                    });

            twinkleSeq
                .WhenExecuted
                .SetUp(() => allPixels.TurnOff())
                .Execute(instance =>
                {
                    var rnd = new Random();

                    while (!instance.IsCancellationRequested)
                    {
                        allPixels.SetAll(Color.White, 0.5);
                        instance.WaitFor(S(1.0), true);

                        int pixel = rnd.Next(allPixels.Pixels);
                        allPixels.FadeTo(pixel, Color.Red, 1.0, S(2.0));
                        instance.WaitFor(S(1.0), true);
                        allPixels.FadeTo(pixel, Color.White, 0.5, S(1.0));
                    }
                })
                .TearDown(() => allPixels.TurnOff());



            // Test Button
            buttonTest.ActiveChanged += (sender, e) =>
            {
                if (!e.NewState)
                    return;

                //Exec.Cancel(candyCane);

                //allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();
                //allPixels.SetAllOnlyColor(Color.Purple);
                //allPixels.RunEffect(new Effect2.Fader(0.0, 1.0), S(2.0)).Wait();
                //allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();

                //allPixels.SetAllOnlyColor(Color.Orange);
                //allPixels.RunEffect(new Effect2.Fader(0.0, 1.0), S(2.0)).Wait();

                //allPixels.RunEffect(new Effect2.Fader(1.0, 0.0), S(2.0)).Wait();

                //Exec.Execute(candyCane);
            };

            pulsatingEffect1.AddDevice(lightStar);
            flickerEffect
                .AddDevice(lightHat1)
                .AddDevice(lightHat2);

            hours.OpenHoursChanged += (i, e) =>
            {
                if (e.IsOpenNow)
                {
                    lightStar.SetBrightness(1.0);
                    lightStairs1.SetBrightness(1.0);
                    Exec.Execute(twinkleSeq);
                    pulsatingEffect1.Start();
                    flickerEffect.Start();
                }
                else
                {
                    flickerEffect.Stop();
                    pulsatingEffect1.Stop();
                    lightStar.TurnOff();
                    lightStairs1.TurnOff();
                    Exec.Cancel(twinkleSeq);
                    System.Threading.Thread.Sleep(200);
                }
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
