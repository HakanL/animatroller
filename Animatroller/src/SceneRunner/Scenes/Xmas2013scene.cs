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
    internal class Xmas2013scene : BaseScene, ISceneRequiresAcnStream, ISceneSupportsSimulator, ISceneRequiresRaspExpander1
    {
        private OperatingHours hours;
        private VirtualPixel1D allPixels;
        private DigitalInput buttonTest;
        private DigitalInput buttonStartDeer;
        private DigitalInput buttonOverrideHours;
        private DigitalInput buttonBlue;
        private DigitalInput buttonRed;
        private Dimmer lightStar;
        private Dimmer lightHat1;
        private Dimmer lightHat2;
        private Dimmer lightHat3;
        private Dimmer lightHat4;
        private Dimmer lightSnow1;
        private Dimmer lightSnow2;
        private Dimmer lightStairs1;
        private Dimmer lightGarland1;
        private Dimmer lightGarland2;
        private Dimmer lightGarland3;
        private Dimmer lightGarland4;
        private Dimmer lightGarland5;
        private Dimmer lightString1;
        private Dimmer lightString2;
        private Dimmer lightXmasTree;
        private Dimmer lightDeerLarge;
        private Dimmer lightDeerSmall;
        private Dimmer lightTopperSmall;
        private Dimmer lightTopperLarge;
        private Dimmer lightNet1;
        private Dimmer lightNet2;
        private StrobeColorDimmer lightTreeUp;
        private StrobeColorDimmer lightVader;
        private StrobeColorDimmer light3wise;
        private Switch switchSanta;
        private Switch switchDeerHuge;
        private Switch switchButtonBlue;
        private Switch switchButtonRed;
        private AudioPlayer audio;

        private Effect.Pulsating pulsatingEffect1;
        private Effect.Flicker flickerEffect;
        private Controller.Sequence candyCane;
        private Controller.Sequence twinkleSeq;
        private bool hugeDeerStarted;

        public Xmas2013scene(IEnumerable<string> args)
        {
            hours = new OperatingHours("Hours");

            lightStar = new Dimmer("Star");
            lightHat1 = new Dimmer("Hat 1");
            lightHat2 = new Dimmer("Hat 2");
            lightHat3 = new Dimmer("Hat 3");
            lightHat4 = new Dimmer("Hat 4");
            lightSnow1 = new Dimmer("Snow 1");
            lightSnow2 = new Dimmer("Snow 2");
            lightStairs1 = new Dimmer("Stair 1");
            lightGarland1 = new Dimmer("Garland 1");
            lightGarland2 = new Dimmer("Garland 2");
            lightGarland3 = new Dimmer("Garland 3");
            lightGarland4 = new Dimmer("Garland 4");
            lightGarland5 = new Dimmer("Garland 5");
            lightString1 = new Dimmer("String 1");
            lightString2 = new Dimmer("String 1");
            lightXmasTree = new Dimmer("Xmas Tree");

            lightDeerLarge = new Dimmer("Deer Large");
            lightDeerSmall = new Dimmer("Deer Small");
            lightTreeUp = new StrobeColorDimmer("Tree up");
            switchSanta = new Switch("Santa");
            switchDeerHuge = new Switch("Deer Huge");
            lightTopperSmall = new Dimmer("Topper Small");
            lightTopperLarge = new Dimmer("Topper Large");
            lightNet1 = new Dimmer("Net 1");
            lightNet2 = new Dimmer("Net 2");
            lightVader = new StrobeColorDimmer("Vader");
            light3wise = new StrobeColorDimmer("3wise");

            pulsatingEffect1 = new Effect.Pulsating("Pulse FX 1", S(4), 0.4, 1.0, false);
            flickerEffect = new Effect.Flicker("Flicker", 0.5, 0.6, false);

            candyCane = new Controller.Sequence("Candy Cane");
            twinkleSeq = new Controller.Sequence("Twinkle");

            allPixels = new VirtualPixel1D("All Pixels", 100);

            buttonTest = new DigitalInput("Test");
            buttonStartDeer = new DigitalInput("Start Deer");
            buttonOverrideHours = new DigitalInput("Override hours");

            buttonBlue = new DigitalInput("Blue");
            buttonRed = new DigitalInput("Red");
            switchButtonBlue = new Switch("Blue");
            switchButtonRed = new Switch("Red");
            audio = new AudioPlayer("Audio");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(buttonTest);
            sim.AddDigitalInput_FlipFlop(buttonOverrideHours);
            sim.AddDigitalInput_Momentarily(buttonStartDeer);

            sim.AddDigitalInput_Momentarily(buttonBlue);
            sim.AddDigitalInput_Momentarily(buttonRed);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
            port.Connect(new Physical.PixelRope(allPixels, 0, 100), 4, 1);
            // GECE
            port.Connect(new Physical.PixelRope(allPixels, 0, 50), 2, 91);

            port.Connect(new Physical.GenericDimmer(lightStar, 1), 21);
            port.Connect(new Physical.GenericDimmer(lightHat1, 2), 21);
            port.Connect(new Physical.GenericDimmer(lightHat2, 3), 21);
            port.Connect(new Physical.GenericDimmer(lightHat3, 4), 21);
            port.Connect(new Physical.GenericDimmer(lightHat4, 5), 21);
            port.Connect(new Physical.GenericDimmer(lightSnow1, 6), 21);
            port.Connect(new Physical.GenericDimmer(lightSnow2, 7), 21);
            port.Connect(new Physical.GenericDimmer(lightTopperSmall, 8), 21);
            port.Connect(new Physical.GenericDimmer(lightTopperLarge, 9), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland1, 22), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland2, 10), 21);
            port.Connect(new Physical.GenericDimmer(lightString2, 11), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland3, 12), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland5, 13), 21);
            port.Connect(new Physical.GenericDimmer(lightXmasTree, 14), 21);
            port.Connect(new Physical.GenericDimmer(lightGarland4, 15), 21);
            port.Connect(new Physical.GenericDimmer(lightNet1, 17), 21);
            port.Connect(new Physical.GenericDimmer(lightString1, 19), 21);
            port.Connect(new Physical.GenericDimmer(lightNet2, 20), 21);
            port.Connect(new Physical.GenericDimmer(lightStairs1, 24), 21);

            port.Connect(new Physical.GenericDimmer(switchDeerHuge, 3), 20);
            port.Connect(new Physical.GenericDimmer(switchSanta, 4), 20);
            port.Connect(new Physical.RGBStrobe(lightTreeUp, 20), 20);
            port.Connect(new Physical.RGBStrobe(lightVader, 30), 20);
            port.Connect(new Physical.RGBStrobe(light3wise, 40), 20);
            port.Connect(new Physical.GenericDimmer(lightDeerLarge, 100), 20);
            port.Connect(new Physical.GenericDimmer(lightDeerSmall, 101), 20);
        }

        public void WireUp1(Expander.Raspberry port)
        {
            port.DigitalInputs[4].Connect(buttonRed);
            port.DigitalInputs[5].Connect(buttonBlue);
            port.DigitalOutputs[0].Connect(switchButtonBlue);
            port.DigitalOutputs[1].Connect(switchButtonRed);

            port.Connect(audio);
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

                        int pixel1 = rnd.Next(allPixels.Pixels);
                        int pixel2 = rnd.Next(allPixels.Pixels);
                        var task1 = allPixels.FadeToAsync(instance, pixel1, Color.Red, 1.0, S(2.0));
                        var task2 = allPixels.FadeToAsync(instance, pixel2, Color.Red, 1.0, S(2.0));
                        Task.WaitAll(task1, task2);

                        instance.WaitFor(S(1.0), true);

                        task1 = allPixels.FadeToAsync(instance, pixel1, Color.White, 0.5, S(1.0));
                        task2 = allPixels.FadeToAsync(instance, pixel2, Color.White, 0.5, S(1.0));
                        Task.WaitAll(task1, task2);
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

            buttonBlue.ActiveChanged += (o, e) =>
                {
                    if (e.NewState)
                    {
                        switchButtonBlue.SetPower(true);
                        audio.PlayTrack("05. Frozen - Let It Go");
                    }
                };

            buttonRed.ActiveChanged += (o, e) =>
            {
                switchButtonRed.SetPower(e.NewState);
                if (e.NewState)
                    audio.PauseTrack();
            };

            audio.AudioTrackDone += (o, e) =>
            {
                switchButtonBlue.SetPower(false);
            };

            pulsatingEffect1.AddDevice(lightStar);
            flickerEffect
                .AddDevice(lightHat1)
                .AddDevice(lightHat2)
                .AddDevice(lightHat3)
                .AddDevice(lightHat4);

            hours.OpenHoursChanged += (i, e) =>
            {
                if (e.IsOpenNow)
                {
                    lightTreeUp.SetColor(Color.Red, 1.0);

                    Exec.Execute(twinkleSeq);
                    pulsatingEffect1.Start();
                    flickerEffect.Start();
                    lightSnow1.SetBrightness(1.0);
                    lightSnow2.SetBrightness(1.0);
                }
                else
                {
                    lightSnow1.TurnOff();
                    lightSnow2.TurnOff();
                    lightTreeUp.TurnOff();
                    flickerEffect.Stop();
                    pulsatingEffect1.Stop();
                    Exec.Cancel(twinkleSeq);
                    System.Threading.Thread.Sleep(200);
                }
            };

            lightGarland1.Follow(hours);
            lightGarland2.Follow(hours);
            lightGarland3.Follow(hours);
            lightGarland4.Follow(hours);
            lightGarland5.Follow(hours);
            lightXmasTree.Follow(hours);
            lightStairs1.Follow(hours);
            lightDeerSmall.Follow(hours);
            lightDeerLarge.Follow(hours);
            switchSanta.Follow(hours);
            //light3wise.Follow(hours);
            //lightVader.Follow(hours);
            lightTopperLarge.Follow(hours);
            lightTopperSmall.Follow(hours);
            //switchButtonBlue.Follow(hours);
            //switchButtonRed.Follow(hours);
            lightNet1.Follow(hours);
            lightNet2.Follow(hours);
            lightString1.Follow(hours);
            lightString2.Follow(hours);
            switchDeerHuge.Follow(hours);
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
