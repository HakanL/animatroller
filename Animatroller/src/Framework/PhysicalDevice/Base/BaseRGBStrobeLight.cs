using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseRGBStrobeLight : BaseDevice
    {
        protected ColorBrightness colorBrightness;
        protected double strobeSpeed;

        protected abstract void Output();

        protected System.Drawing.Color GetColorFromColorBrightness()
        {
            var hsv = new HSV(this.colorBrightness.Color);

            // Adjust brightness
            hsv.Value = hsv.Value * this.colorBrightness.Brightness;

            return hsv.Color;
        }

        public BaseRGBStrobeLight(ColorDimmer logicalDevice)
            : base(logicalDevice)
        {
            this.colorBrightness = new ColorBrightness();

            logicalDevice.ColorChanged += (sender, e) =>
                {
                    this.colorBrightness.Color = e.NewColor;
                    this.colorBrightness.Brightness = e.NewBrightness;

                    Output();
                };
        }

        public BaseRGBStrobeLight(ColorDimmer2 logicalDevice)
            : base(logicalDevice)
        {
            this.colorBrightness = new ColorBrightness();

            logicalDevice.InputColor.Subscribe(x =>
                {
                    this.colorBrightness.Color = x;

                    Output();
                });

            logicalDevice.InputBrightness.Subscribe(x =>
            {
                this.colorBrightness.Brightness = x.Value;

                Output();
            });
        }

        public BaseRGBStrobeLight(StrobeColorDimmer logicalDevice)
            : this((ColorDimmer)logicalDevice)
        {
            logicalDevice.StrobeSpeedChanged += (sender, e) =>
                {
                    this.strobeSpeed = e.NewSpeed;

                    Output();
                };
        }

        public BaseRGBStrobeLight(StrobeColorDimmer2 logicalDevice)
            : this((ColorDimmer2)logicalDevice)
        {
            logicalDevice.InputStrobeSpeed.Subscribe(x =>
            {
                this.strobeSpeed = x.Value;

                Output();
            });
        }
    }
}
