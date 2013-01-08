using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;

namespace Animatroller.SceneRunner
{
    internal class XmasScene : IScene
    {
        protected Pixel1D testPixels = new Pixel1D("G35", 50);
        protected Dimmer explosion1 = new Dimmer("Explosion 1");
        protected Dimmer explosion2 = new Dimmer("Explosion 2");
        protected Dimmer explosion3 = new Dimmer("Explosion 3");
        protected Dimmer explosion4 = new Dimmer("Explosion 4");
        protected Animatroller.Framework.LogicalDevice.DigitalInput testButton = new Animatroller.Framework.LogicalDevice.DigitalInput("Test");

        private Random random = new Random();

        //        protected Animatroller.Framework.Effect.Pulsating pulsatingEffect;
        //        protected Animatroller.Framework.Effect.Flicker flickerEffect;
        protected Animatroller.Framework.PhysicalDevice.NetworkAudioPlayer audioPlayer;

        public XmasScene()
        {
            audioPlayer = new Animatroller.Framework.PhysicalDevice.NetworkAudioPlayer(
                Properties.Settings.Default.NetworkAudioPlayerIP,
                Properties.Settings.Default.NetworkAudioPlayerPort);

            //            pulsatingEffect = new Animatroller.Framework.Effect.Pulsating(TimeSpan.FromSeconds(1), 0.2, 0.7);
            //            flickerEffect = new Animatroller.Framework.Effect.Flicker(0.4, 0.6);

            Executor.Current.Register(this);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily("Test Button").Connect(testButton);
        }

        public void WireUp(IOExpander port)
        {
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(explosion1, 1));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(explosion2, 2));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(explosion3, 3));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(explosion4, 4));
            port.Connect(new Animatroller.Framework.PhysicalDevice.PixelRope(testPixels));
            port.DigitalInputs[0].Connect(testButton);
        }

        public void WireUp(DMXPro port)
        {
            //            port.Connect(new Animatroller.Framework.PhysicalDevice.SmallRGBStrobe(candyLight, 16));
        }

        public void Start()
        {
            var explosion = new Sequence("Explosion");
            explosion.WhenExecuted
            .Execute(instance =>
            {
                audioPlayer.PlayEffect("18384__inferno__largex");
                instance.WaitFor(TimeSpan.FromMilliseconds(300));
                int d = 100;
                explosion1.SetBrightness(1);
                instance.WaitFor(TimeSpan.FromMilliseconds(d));
                explosion1.SetBrightness(0.5);
                explosion2.SetBrightness(1);
                instance.WaitFor(TimeSpan.FromMilliseconds(d));
                explosion1.TurnOff();
                explosion2.SetBrightness(0.5);
                explosion3.SetBrightness(1);
                instance.WaitFor(TimeSpan.FromMilliseconds(d));
                explosion2.TurnOff();
                explosion3.SetBrightness(0.5);
                explosion4.SetBrightness(1);
                instance.WaitFor(TimeSpan.FromMilliseconds(d));
                explosion3.TurnOff();
                explosion4.SetBrightness(0.5);
                instance.WaitFor(TimeSpan.FromMilliseconds(d));
                explosion4.TurnOff();
            });

            var seq = new Sequence("Seq");
            seq.WhenExecuted
            .Execute(instance => 
                {
//                    audioPlayer.PlayEffect("tie_fighter");
//                    x.WaitFor(TimeSpan.FromSeconds(2));

                    audioPlayer.PlayEffect("Lazer");
                    instance.WaitFor(TimeSpan.FromMilliseconds(300));
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
                        instance.WaitFor(TimeSpan.FromMilliseconds(10));
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

        public void Run()
        {
        }

        public void Stop()
        {
        }
    }
}
