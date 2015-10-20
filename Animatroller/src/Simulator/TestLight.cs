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
        private double? pan;
        private double? tilt;

        public Control.StrobeBulb LabelLightControl
        {
            set { this.control = value; }
        }

        public TestLight(IApiVersion3 logicalDevice)
            : base(logicalDevice)
        {
            var movingHeadDevice = logicalDevice as MovingHead;

            if(movingHeadDevice != null)
            {
                movingHeadDevice.OutputPan.Subscribe(x => this.pan = x);
                movingHeadDevice.OutputTilt.Subscribe(x => this.tilt = x);
            }
        }

        protected override void Output()
        {
            this.control.Color = GetColorFromColorBrightness();
            string ownedStatus = string.Empty;

            foreach (ILogicalDevice device in this.logicalDevices)
            {
                if (device is IOwnedDevice && ((IOwnedDevice)device).IsOwned)
                {
                    ownedStatus = "*";
                    break;
                }
            }

            this.control.Text = string.Format("{1}{0:0%}", GetMonochromeBrightnessFromColorBrightness(), ownedStatus);

            this.control.ColorGel = this.colorBrightness.Color;
            this.control.Intensity = this.colorBrightness.Brightness;

            this.control.Pan = this.pan;
            this.control.Tilt = this.tilt;
        }

        public void Update()
        {
            Output();
        }
    }
}
