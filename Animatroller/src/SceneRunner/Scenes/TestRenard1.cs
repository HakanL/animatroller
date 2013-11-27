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
    internal class TestRenard1 : BaseScene, ISceneRequiresRenard, ISceneSupportsSimulator
    {
        private Dimmer testLight1;
        private DigitalInput buttonTest1;


        public TestRenard1(IEnumerable<string> args)
        {
            buttonTest1 = new DigitalInput("Test 1");
            testLight1 = new Dimmer("Test 1");
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_FlipFlop(buttonTest1);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp(Expander.Renard port)
        {
            port.Connect(new Physical.GenericDimmer(testLight1, 24));
        }

        public override void Start()
        {
            buttonTest1.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    testLight1.RunEffect(new Effect2.Fader(0.0, 1.0), S(1.0));
                }
                else
                {
                    if(testLight1.Brightness > 0)
                        testLight1.RunEffect(new Effect2.Fader(1.0, 0.0), S(1.0));
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
