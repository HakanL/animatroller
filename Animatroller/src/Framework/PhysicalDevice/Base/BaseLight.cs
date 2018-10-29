using System;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseLight : BaseDevice
    {
        protected ColorBrightness colorBrightness;

        public BaseLight(DigitalOutput2 logicalDevice)
            : base(logicalDevice)
        {
            this.colorBrightness = new ColorBrightness(Color.White, 0.0);

            if (logicalDevice is ISendsData sendsData)
            {
                sendsData.OutputChanged.Subscribe(x =>
                {
                    SetFromIData(logicalDevice, x);

                    Output();
                });

                SetFromIData(logicalDevice, sendsData.CurrentData);
            }
        }

        public BaseLight(IApiVersion3 logicalDevice)
            : base(logicalDevice)
        {
            this.colorBrightness = new ColorBrightness(Color.White, 0.0);

            if (logicalDevice is ISendsData sendsData)
            {
                sendsData.OutputChanged.Subscribe(x =>
                {
                    SetFromIData(logicalDevice, x);

                    Output();
                });

                SetFromIData(logicalDevice, sendsData.CurrentData);
            }

            Executor.Current.Blackout.Subscribe(_ => Output());
            Executor.Current.Whiteout.Subscribe(_ => Output());
        }

        protected abstract void Output();

        protected virtual void SetFromIData(ILogicalDevice logicalDevice, IData data)
        {
            object value;
            bool masterPower = true;
            var masterPowerDevice = logicalDevice as IHasMasterPower;
            if (masterPowerDevice != null)
                masterPower = masterPowerDevice.MasterPower;

            if (data.TryGetValue(DataElements.Brightness, out value))
                this.colorBrightness.Brightness = (double)value * (masterPower ? 1 : 0);
            else
            {
                bool? power = data.GetValue<bool>(DataElements.Power);
                if (power.HasValue)
                    this.colorBrightness.Brightness = (power.Value && masterPower) ? 1 : 0;
            }

            if (data.TryGetValue(DataElements.Color, out value))
                this.colorBrightness.Color = (Color)value;
        }

        protected Color GetColorFromColorBrightness()
        {
            return GetColorFromColorBrightness(this.colorBrightness);
        }

        public static System.Drawing.Color GetColorFromColorBrightness(Color color, double brightness)
        {
            return GetColorFromColorBrightness(new ColorBrightness(color, brightness));
        }

        public static System.Drawing.Color GetColorFromColorBrightness(ColorBrightness colorBrightness)
        {
            var hsv = new HSV(colorBrightness.Color);

            double whiteOut = Executor.Current.Whiteout.Value;

            // Adjust brightness
            double adjustedValue = (hsv.Value * colorBrightness.Brightness) + whiteOut;

            // Adjust for WhiteOut
            HSV baseHsv;
            if (colorBrightness.Brightness == 0 && whiteOut > 0)
                // Base it on black instead
                baseHsv = HSV.Black;
            else
                baseHsv = hsv;

            hsv.Saturation = baseHsv.Saturation + (HSV.White.Saturation - baseHsv.Saturation) * whiteOut;
            hsv.Value = adjustedValue.Limit(0, 1) * (1 - Executor.Current.Blackout.Value);

            return hsv.Color;
        }

        protected double GetMonochromeBrightnessFromColorBrightness()
        {
            Color color = GetColorFromColorBrightness();

            return Math.Max(Math.Max(color.R, color.G), color.B) / 255.0;
        }

        public override void SetInitialState()
        {
            base.SetInitialState();

            Output();
        }
    }
}
