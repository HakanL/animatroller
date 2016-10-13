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
    internal class TestOSC : BaseScene
    {
        Expander.OscServer oscServer = new Expander.OscServer(8000);
        Dimmer3 testDimmer1 = new Dimmer3();
        Dimmer3 testDimmer2 = new Dimmer3();
        Expander.OscClient touchOSC = new Expander.OscClient("192.168.240.221", 8000);

        public TestOSC(IEnumerable<string> args)
        {
            this.oscServer.RegisterActionSimple<double>("/MasterVolume/x", (msg, data) =>
            {
                testDimmer1.SetBrightness(data);

                touchOSC.Send("/Hakan/value", data);
            });

            this.oscServer.RegisterActionSimple<bool>("/Switches/x", (msg, data) =>
            {
                testDimmer2.SetBrightness(data ? 1.0 : 0.0);


                touchOSC.Send("/Pads/x", data ? 1.0f : 0.0f);
            });
        }
    }
}
