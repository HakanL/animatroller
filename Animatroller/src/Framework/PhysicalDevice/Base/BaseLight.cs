using System;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseLight : BaseDevice
    {
        protected ColorBrightness colorBrightness;// = new ColorBrightness(Color.White, 0.0);

        protected abstract void Output();

        protected virtual void SetFromIData(IData data)
        {
            object value;

            if (data.TryGetValue(DataElements.Brightness, out value))
                this.colorBrightness.Brightness = (double)value;

            if (data.TryGetValue(DataElements.Color, out value))
                this.colorBrightness.Color = (Color)value;
        }

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
            this.colorBrightness = new ColorBrightness(Color.White, 0.0);

            logicalDevice.Output.Subscribe(x =>
            {
                this.colorBrightness.Brightness = x ? 1.0 : 0.0;

                Output();
            });
        }

        public BaseLight(IApiVersion3 logicalDevice)
            : base(logicalDevice)
        {
            this.colorBrightness = new ColorBrightness(Color.White, 0.0);

            var sendsData = logicalDevice as ISendsData;
            if (sendsData != null)
            {
                sendsData.OutputData.Subscribe(x =>
                {
                    SetFromIData(x);

                    Output();
                });

                SetFromIData(sendsData.CurrentData);
            }

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
