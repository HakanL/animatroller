using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Physical = Animatroller.Framework.PhysicalDevice;
using Effect = Animatroller.Framework.Effect;
using Import = Animatroller.Framework.Import;
using System.IO;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes
{
    internal partial class Xmas2018 : BaseScene
    {
        public void ConfigureOSC()
        {
            oscServer.RegisterActionSimple<double>("/HazerFan/x", (msg, data) =>
            {
                hazerFanSpeed.SetBrightness(data);
            });

            oscServer.RegisterActionSimple<double>("/HazerHaze/x", (msg, data) =>
            {
                hazerHazeOutput.SetBrightness(data);
            });
        }
    }
}
