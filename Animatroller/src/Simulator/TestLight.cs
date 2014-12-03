using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Simulator
{
    public class TestLight : Animatroller.Framework.PhysicalDevice.BaseStrobeLight, INeedsLabelLight, IUpdateableControl
    {
        private Control.StrobeBulb control;

        public Control.StrobeBulb LabelLightControl
        {
            set { this.control = value; }
        }

        public TestLight(Switch logicalDevice)
            : base(logicalDevice)
        {
        }

        public TestLight(Dimmer logicalDevice)
            : base(logicalDevice)
        {
        }

        public TestLight(Dimmer2 logicalDevice)
            : base(logicalDevice)
        {
        }

        public TestLight(ColorDimmer logicalDevice)
            : base(logicalDevice)
        {
        }

        public TestLight(ColorDimmer2 logicalDevice)
            : base(logicalDevice)
        {
        }

        public TestLight(ILogicalDevice logicalDevice)
            : base(logicalDevice)
        {
        }

        protected override void Output()
        {
            this.control.Color = GetColorFromColorBrightness();
            this.control.Text = string.Format("{0:0%}", GetMonochromeBrightnessFromColorBrightness());
        }

        public void Update()
        {
            Output();
        }
    }
}
