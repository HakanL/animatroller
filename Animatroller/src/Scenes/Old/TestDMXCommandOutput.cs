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
    internal class TestDMXCommandOutput : BaseScene
    {
        const int SacnUniverse = 1;

        CommandDevice medeaWiz = new CommandDevice();

        Expander.AcnStream acnOutput = new Expander.AcnStream();

        DigitalInput2 buttonTest0 = new DigitalInput2();
        DigitalInput2 buttonTest1 = new DigitalInput2();
        DigitalInput2 buttonTest2 = new DigitalInput2();


        public TestDMXCommandOutput(IEnumerable<string> args)
        {
            acnOutput.Connect(new Physical.DMXCommandOutput(medeaWiz, 1, TimeSpan.FromMilliseconds(500)), SacnUniverse);

            buttonTest0.Output.Subscribe(x =>
            {
                if (x)
                {
                    log.Information("Sending 0xff");
                    medeaWiz.SendCommand(null, 0xff);
                }
            });

            buttonTest1.Output.Subscribe(x =>
            {
                if (x)
                {
                    log.Information("Sending 0x01");
                    medeaWiz.SendCommand(null, 0x01);
                }
            });

            buttonTest2.Output.Subscribe(x =>
            {
                if (x)
                {
                    log.Information("Sending 0x02");
                    medeaWiz.SendCommand(null, 0x02);
                }
            });
        }
    }
}
