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

namespace Animatroller.Scenes
{
    internal class TestStreamingACN : BaseScene
    {
        const int SacnUniverse = 4;

        Expander.AcnStream acnOutput = new Expander.AcnStream();
        Dimmer3 testLight1 = new Dimmer3();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonTest1 = new DigitalInput2();


        public TestStreamingACN(IEnumerable<string> args)
        {
            acnOutput.Connect(new Physical.GenericDimmer(testLight1, 65), SacnUniverse);

            buttonTest1.Output.Subscribe(x =>
            {
                testLight1.SetBrightness(x ? 1.0 : 0.0);
            });
        }
    }
}
