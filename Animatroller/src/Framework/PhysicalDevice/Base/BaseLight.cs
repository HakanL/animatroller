using System;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseLight : BaseDevice
    {
        protected ColorBrightness colorBrightness = new ColorBrightness();

        protected abstract void Output();

        protected System.Drawing.Color GetColorFromColorBrightness()
        {
            var hsv = new HSV(this.colorBrightness.Color);

            // Adjust brightness
            hsv.Value = hsv.Value * this.colorBrightness.Brightness;

            return hsv.Color;
        }

        protected double GetMonochromeBrightnessFromColorBrightness()
        {
            Color color = GetColorFromColorBrightness();

            return Math.Max(Math.Max(color.R, color.G), color.B) / 255.0;
        }

        public BaseLight(Dimmer logicalDevice)
            : base(logicalDevice)
        {
            logicalDevice.BrightnessChanged += (sender, e) =>
            {
                this.colorBrightness.Brightness = e.NewBrightness;

                Output();
            };
        }

        public BaseLight(Switch logicalDevice)
            : base(logicalDevice)
        {
            logicalDevice.PowerChanged += (sender, e) =>
                {
                    this.colorBrightness.Brightness = e.NewState ? 1.0 : 0.0;

                    Output();
                };
        }

        public BaseLight(DigitalOutput2 logicalDevice)
            : base(logicalDevice)
        {
            logicalDevice.Output.Subscribe(x =>
            {
                this.colorBrightness.Brightness = x ? 1.0 : 0.0;

                Output();
            });
        }

        public BaseLight(Dimmer2 logicalDevice)
            : base(logicalDevice)
        {
            logicalDevice.InputBrightness.Subscribe(x =>
            {
                this.colorBrightness.Brightness = x.Value;

                Output();
            });
        }

        public BaseLight(ILogicalDevice logicalDevice)
            : base(logicalDevice)
        {
            if (logicalDevice is ISendsBrightness)
            {
                ((ISendsBrightness)logicalDevice).OutputBrightness.Subscribe(x =>
                {
                    this.colorBrightness.Brightness = x;

                    Output();
                });
            }

            if (logicalDevice is ISendsColor)
            {
                ((ISendsColor)logicalDevice).OutputColor.Subscribe(x =>
                {
                    this.colorBrightness.Color = x;

                    Output();
                });
            }
            else
                this.colorBrightness.Color = Color.White;
        }

        public override void StartDevice()
        {
            base.StartDevice();

            Output();
        }
    }
}
