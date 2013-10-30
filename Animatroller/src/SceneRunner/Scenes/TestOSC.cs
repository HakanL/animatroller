using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class TestOSC : BaseScene, ISceneSupportsSimulator, ISceneRequiresAcnStream
    {
        public struct Finger
        {
            public double X;
            public double Y;
            public double Z;
        }

        private Expander.OscServer oscServer;
        private Dimmer testDimmer;
        private Finger[] fingers;
        private Controller.Sequence loopSeq;
        private VirtualPixel1D allPixels;

        public TestOSC(IEnumerable<string> args)
        {
            allPixels = new VirtualPixel1D("All Pixels", 28 + 50);

            loopSeq = new Controller.Sequence("Loop Seq");
            fingers = new Finger[10];
            for (int i = 0; i < 10; i++)
                fingers[i] = new Finger();

            testDimmer = new Dimmer("Test");

            this.oscServer = new Expander.OscServer(5555);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.AcnStream port)
        {
            // WS2811
            port.Connect(new Physical.PixelRope(allPixels, 0, 28), 1, 1);
            // WS2811
            port.Connect(new Physical.PixelRope(allPixels, 28, 50), 1, 151);
        }

        public override void Start()
        {
            loopSeq
                .Loop
                .WhenExecuted
                .Execute(instance =>
                    {
                        testDimmer.SetBrightness(Math.Abs(this.fingers[0].X));
                        allPixels.SetAll(Color.Yellow, Math.Abs(this.fingers[0].X));
                        instance.WaitFor(S(0.1));
                    });
            
            this.oscServer.RegisterAction<double>("/1/fader*", (msg, data) =>
                {
                    int finger = int.Parse(msg.Address.Substring(8));
                    if (finger >= 1 && finger <= 10 && data.Any())
                        this.fingers[finger - 1].Y = data.First();
                });

            this.oscServer.RegisterAction<double>("/1/Xfader*", (msg, data) =>
            {
                int finger = int.Parse(msg.Address.Substring(9));
                if (finger >= 1 && finger <= 10 && data.Any())
                    this.fingers[finger - 1].X = data.First();
            });

            this.oscServer.RegisterAction<double>("/1/Zfader*", (msg, data) =>
            {
                int finger = int.Parse(msg.Address.Substring(9));
                if (finger >= 1 && finger <= 10 && data.Any())
                    this.fingers[finger - 1].Z = data.First();
            });
        }

        public override void Run()
        {
            Exec.Execute(loopSeq);
        }

        public override void Stop()
        {
        }
    }
}
