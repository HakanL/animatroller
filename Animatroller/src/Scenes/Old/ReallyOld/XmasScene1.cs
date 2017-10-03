using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Scenes
{
    internal class XmasScene1 : BaseScene
    {
        private Pixel1D testPixels;
        private Dimmer explosion1;
        private Dimmer explosion2;
        private Dimmer explosion3;
        private Dimmer explosion4;
        [SimulatorButtonType(SimulatorButtonTypes.Momentarily)]
        private DigitalInput testButton;
        private Physical.NetworkAudioPlayer audioPlayer;


        public XmasScene1(IEnumerable<string> args, System.Collections.Specialized.NameValueCollection settings)
        {
            testPixels = new Pixel1D("G35", 50);
            explosion1 = new Dimmer("Explosion 1");
            explosion2 = new Dimmer("Explosion 2");
            explosion3 = new Dimmer("Explosion 3");
            explosion4 = new Dimmer("Explosion 4");
            testButton = new DigitalInput("Test");

            audioPlayer = new Physical.NetworkAudioPlayer(
                settings["NetworkAudioPlayerIP"],
                int.Parse(settings["NetworkAudioPlayerPort"]));
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
            var explosion = new Controller.Sequence("Explosion");
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

            var seq = new Controller.Sequence("Seq");
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
                    this.log.Information("Button press!");
                    Executor.Current.Execute(seq);
                    //                    audioPlayer.PlayEffect("Lazer");

                }
                else
                {
                    this.log.Information("Button depress!");

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
