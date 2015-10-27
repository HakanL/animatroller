using System;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseLight : BaseDevice
    {
        protected ColorBrightness colorBrightness = new ColorBrightness(Color.White, 0.0);

        protected abstract void Output();

        protected System.Drawing.Color GetColorFromColorBrightness()
        {
            var hsv = new HSV(this.colorBrightness.Color);

            double whiteOut = Executor.Current.Whiteout.Value;

            // Adjust brightness
            double adjustedValue = (hsv.Value * this.colorBrightness.Brightness) + whiteOut;

            // Adjust for WhiteOut
            HSV baseHsv;
            if (this.colorBrightness.Brightness == 0 && whiteOut > 0)
                // Base it on black instead
                baseHsv = HSV.Black;
            else
                baseHsv = hsv;

            hsv.Hue = baseHsv.Hue + (HSV.White.Hue - baseHsv.Hue) * whiteOut;
            hsv.Saturation = baseHsv.Saturation + (HSV.White.Saturation - baseHsv.Saturation) * whiteOut;
            hsv.Value = adjustedValue.Limit(0, 1) * (1 - Executor.Current.Blackout.Value);

            return hsv.Color;
        }

        protected double GetMonochromeBrightnessFromColorBrightness()
        {
            Color color = GetColorFromColorBrightness();

            return Math.Max(Math.Max(color.R, color.G), color.B) / 255.0;
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

        public BaseLight(IApiVersion3 logicalDevice)
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
            else if (logicalDevice is ISendsColorBrightness)
            {
                ((ISendsColorBrightness)logicalDevice).OutputColorBrightness.Subscribe(x =>
                {
                    this.colorBrightness = x;

                    Output();
                });
            }
            else
                this.colorBrightness.Color = Color.White;

            Executor.Current.Blackout.Subscribe(_ => Output());
            Executor.Current.Whiteout.Subscribe(_ => Output());
        }

        public override void SetInitialState()
        {
            base.SetInitialState();

            Output();
        }
    }
}
