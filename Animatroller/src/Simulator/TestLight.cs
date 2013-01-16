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
    public class TestLight : INeedsLabelLight, IPhysicalDevice
    {
        private ILogicalDevice logicalDevice;
        private Control.StrobeBulb control;
        public Control.StrobeBulb LabelLightControl
        {
            set { this.control = value; }
        }

        public TestLight(Switch logicalDevice)
        {
            Executor.Current.Register(this);

            this.logicalDevice = logicalDevice;

            logicalDevice.PowerChanged += (sender, e) =>
            {
                this.control.Color = e.NewState ? Color.Green : Color.Black;
            };
        }

        public TestLight(Dimmer logicalDevice)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.BrightnessChanged += (sender, e) =>
            {
                this.control.Color = Color.FromArgb(e.NewBrightness.GetByteScale(), e.NewBrightness.GetByteScale(), e.NewBrightness.GetByteScale());
                this.control.Text = string.Format("{0:0%}", e.NewBrightness);
            };

            WireUpStrobe(logicalDevice as StrobeDimmer);
        }

        public TestLight(ColorDimmer logicalDevice)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.ColorChanged += (sender, e) =>
            {
                var hsv = new HSV(e.NewColor);
                hsv.Value = hsv.Value * e.NewBrightness;

                this.control.Color = hsv.Color;
                this.control.Text = string.Format("{0:0%}", e.NewBrightness);
            };

            WireUpStrobe(logicalDevice as StrobeColorDimmer);
        }

        public ILogicalDevice ConnectedDevice
        {
            get { return this.logicalDevice; }
        }

        private void WireUpStrobe(IStrobe strobeLight)
        {
            if (strobeLight == null)
                return;

            strobeLight.StrobeSpeedChanged += (sender, e) =>
            {
                if (e.NewSpeed == 0)
                    this.control.StrobeDelayMS = 0;
                else
                    this.control.StrobeDelayMS = (int)(50 / e.NewSpeed);
            };
        }

        public void StartDevice()
        {
        }
    }
}
