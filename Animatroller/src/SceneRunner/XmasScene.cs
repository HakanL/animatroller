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
using Effect = Animatroller.Framework.Effect;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class XmasScene : BaseScene
    {
        private Random random = new Random();

        protected Pixel1D testPixels;
        protected Dimmer explosion1;
        protected Dimmer explosion2;
        protected Dimmer explosion3;
        protected Dimmer explosion4;
        protected DigitalInput testButton;
        protected Physical.NetworkAudioPlayer audioPlayer;


        public XmasScene()
        {
            testPixels = new Pixel1D("G35", 50);
            explosion1 = new Dimmer("Explosion 1");
            explosion2 = new Dimmer("Explosion 2");
            explosion3 = new Dimmer("Explosion 3");
            explosion4 = new Dimmer("Explosion 4");
            testButton = new DigitalInput("Test");

            audioPlayer = new Physical.NetworkAudioPlayer(
                Properties.Settings.Default.NetworkAudioPlayerIP,
                Properties.Settings.Default.NetworkAudioPlayerPort);
        }

        public void WireUp(Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(testButton);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.IOExpander port)
        {
            port.Connect(new Physical.GenericDimmer(explosion1, 1));
            port.Connect(new Physical.GenericDimmer(explosion2, 2));
            port.Connect(new Physical.GenericDimmer(explosion3, 3));
            port.Connect(new Physical.GenericDimmer(explosion4, 4));
            port.Connect(new Physical.PixelRope(testPixels));
            port.DigitalInputs[0].Connect(testButton);
        }

        public override void Start()
        {
            var explosion = new Sequence("Explosion");
            explosion.WhenExecuted
            .Execute(instance =>
            {
                audioPlayer.PlayEffect("18384__inferno__largex");
                instance.WaitFor(MS(300));
                int d = 100;
                explosion1.SetBrightness(1);
                instance.WaitFor(MS(d));
                explosion1.SetBrightness(0.5);
                explosion2.SetBrightness(1);
                instance.WaitFor(MS(d));
                explosion1.TurnOff();
                explosion2.SetBrightness(0.5);
                explosion3.SetBrightness(1);
                instance.WaitFor(MS(d));
                explosion2.TurnOff();
                explosion3.SetBrightness(0.5);
                explosion4.SetBrightness(1);
                instance.WaitFor(MS(d));
                explosion3.TurnOff();
                explosion4.SetBrightness(0.5);
                instance.WaitFor(MS(d));
                explosion4.TurnOff();
            });

            var seq = new Sequence("Seq");
            seq.WhenExecuted
            .Execute(instance =>
                {
                    //                    audioPlayer.PlayEffect("tie_fighter");
                    //                    x.WaitFor(Seconds(2));

                    audioPlayer.PlayEffect("Lazer");
                    instance.WaitFor(MS(300));
                    audioPlayer.PlayEffect("Lazer");

                    var cb = new ColorBrightness[6];
                    cb[0] = new ColorBrightness(Color.Black, 1.0);
                    cb[1] = new ColorBrightness(Color.Red, 1.0);
                    cb[2] = new ColorBrightness(Color.Orange, 1.0);
                    cb[3] = new ColorBrightness(Color.Yellow, 1.0);
                    cb[4] = new ColorBrightness(Color.Blue, 1.0);
                    cb[5] = new ColorBrightness(Color.White, 1.0);

                    for (int i = -6; i < 50; i++)
                    {
                        testPixels.SetColors(i, cb);
                        instance.WaitFor(MS(10));
                    }

                    if (random.Next(10) > 5)
                        Executor.Current.Execute(explosion);
                });

            //            flickerEffect.AddDevice(candyLight);
            testButton.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    Console.WriteLine("Button press!");
                    Executor.Current.Execute(seq);
                    //                    audioPlayer.PlayEffect("Lazer");

                }
                else
                {
                    Console.WriteLine("Button depress!");

                    //                    testPixels.SetColor(0, Color.Blue);
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
