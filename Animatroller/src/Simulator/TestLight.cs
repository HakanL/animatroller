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
    public class TestLight : INeedsLabelLight, IPhysicalDevice, IUpdateableControl
    {
        private double currentBrightness;
        private Color currentColor;

        private ILogicalDevice logicalDevice;

        private Control.StrobeBulb control;

        public Control.StrobeBulb LabelLightControl
        {
            set { this.control = value; }
        }

        public void Update()
        {
            DisplayBrightnessColor(this.currentColor, this.currentBrightness);
        }

        public TestLight(Switch logicalDevice)
        {
            Executor.Current.Register(this);

            this.logicalDevice = logicalDevice;

            logicalDevice.PowerChanged += (sender, e) =>
            {
                this.currentColor = e.NewState ? Color.Green : Color.Black;
            };
        }

        public TestLight(Dimmer logicalDevice)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.BrightnessChanged += (sender, e) =>
            {
                this.currentBrightness = e.NewBrightness;
            };

            WireUpStrobe(logicalDevice as StrobeDimmer);
        }

        public TestLight(Dimmer2 logicalDevice)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.InputBrightness.Subscribe(x =>
                {
                    this.currentBrightness = x.Value;
                });
        }

        public TestLight(Dimmer3 logicalDevice)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.OutputBrightness.Subscribe(x =>
            {
                this.currentBrightness = x;
            });
        }

        public TestLight(ColorDimmer logicalDevice)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.ColorChanged += (sender, e) =>
            {
                this.currentColor = e.NewColor;
                this.currentBrightness = e.NewBrightness;
            };

            WireUpStrobe(logicalDevice as StrobeColorDimmer);
        }

        private void DisplayBrightnessColor(Color color, double brightness)
        {
            var hsv = new HSV(color);
            hsv.Value = hsv.Value * brightness;

            this.control.Color = hsv.Color;
            this.control.Text = string.Format("{0:0%}", brightness);
        }

        public TestLight(ColorDimmer2 logicalDevice)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.InputColor.Subscribe(x =>
            {
                this.currentColor = x;
            });

            logicalDevice.InputBrightness.Subscribe(x =>
                {
                    this.currentBrightness = x.Value;
                });

            //FIXME            WireUpStrobe(logicalDevice as StrobeColorDimmer);
        }

        public TestLight(ColorDimmer3 logicalDevice)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.OutputColor.Subscribe(x =>
            {
                this.currentColor = x;
            });

            logicalDevice.OutputBrightness.Subscribe(x =>
            {
                this.currentBrightness = x;
            });

            //FIXME            WireUpStrobe(logicalDevice as StrobeColorDimmer);
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

        public string Name
        {
            get { return string.Empty; }
        }
    }
}
