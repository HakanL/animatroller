using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class PixelRope : BaseDevice, INeedsPixelOutput
    {
        private object lockObject = new object();

        public IPixelOutput PixelOutputPort { protected get; set; }

        public PixelRope(Pixel1D logicalDevice)
            : base(logicalDevice)
        {
            logicalDevice.PixelChanged += (sender, e) =>
                {
                    // Handles brightness as well

                    var hsv = new HSV(e.NewColor);
                    hsv.Value = hsv.Value * e.NewBrightness;
                    var color = hsv.Color;

                    if (System.Threading.Monitor.TryEnter(lockObject))
                    {
                        try
                        {
                            PixelOutputPort.SendPixelValue(e.Channel, new PhysicalDevice.PixelRGBByte(color.R, color.G, color.B));
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(lockObject);
                        }
                    }
                    else
                        log.Warn("Missed PixelChanged in PixelRope");
                };

            logicalDevice.MultiPixelChanged += (sender, e) =>
                {
                    var values = new PhysicalDevice.PixelRGBByte[e.NewValues.Length];
                    for (int i = 0; i < e.NewValues.Length; i++)
                    {
                        var hsv = new HSV(e.NewValues[i].Color);
                        hsv.Value = hsv.Value * e.NewValues[i].Brightness;
                        var color = hsv.Color;

                        values[i] = new PixelRGBByte(color.R, color.G, color.B);
                    }

                    if (System.Threading.Monitor.TryEnter(lockObject))
                    {
                        try
                        {
                            PixelOutputPort.SendPixelsValue(e.StartChannel, values);
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(lockObject);
                        }
                    }
                    else
                        log.Warn("Missed send to PixelRope");
                };
        }

        protected System.Drawing.Color GetColorFromColorBrightness(System.Drawing.Color color, double brightness)
        {
            var hsv = new HSV(color);

            double whiteOut = Executor.Current.Whiteout.Value;

            // Adjust brightness
            double adjustedValue = (hsv.Value * brightness) + whiteOut;

            // Adjust for WhiteOut
            HSV baseHsv;
            if (brightness == 0 && whiteOut > 0)
                // Base it on black instead
                baseHsv = HSV.Black;
            else
                baseHsv = hsv;

            hsv.Hue = baseHsv.Hue + (HSV.White.Hue - baseHsv.Hue) * whiteOut;
            hsv.Saturation = baseHsv.Saturation + (HSV.White.Saturation - baseHsv.Saturation) * whiteOut;
            hsv.Value = adjustedValue.Limit(0, 1) * (1 - Executor.Current.Blackout.Value);

            return hsv.Color;
        }

        public PixelRope(VirtualPixel1D logicalDevice, int startVirtualPosition, int positions)
            : base(logicalDevice)
        {
            logicalDevice.AddPixelDevice(startVirtualPosition, positions, (sender, e) =>
                {
                    // Handles brightness as well
                    var color = GetColorFromColorBrightness(e.NewColor, e.NewBrightness);

                    lock (this.lockObject)
                    {
                        PixelOutputPort.SendPixelValue(e.Channel, new PhysicalDevice.PixelRGBByte(color.R, color.G, color.B));
                    }
                }, (sender, e) =>
                {
                    var values = new PhysicalDevice.PixelRGBByte[e.NewValues.Length];
                    for (int i = 0; i < e.NewValues.Length; i++)
                    {
                        var color = GetColorFromColorBrightness(e.NewValues[i].Color, e.NewValues[i].Brightness);

                        values[i] = new PixelRGBByte(color.R, color.G, color.B);
                    }

                    lock (this.lockObject)
                    {
                        PixelOutputPort.SendPixelsValue(e.StartChannel, values);
                    }
                });
        }
    }
}
