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
    internal class TestUsbDmxPro : BaseScene
    {
        Dimmer3 testLight1 = new Dimmer3();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonTest1 = new DigitalInput2();

        Expander.DMXPro dmxPro = new Expander.DMXPro("COM1");


        public TestUsbDmxPro(IEnumerable<string> args)
        {
            buttonTest1.Output.Subscribe(x =>
            {
                testLight1.SetBrightness(x ? 1.0 : 0.0);
            });
        }
    }
}
