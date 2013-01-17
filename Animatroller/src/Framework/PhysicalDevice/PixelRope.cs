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
                        log.Error("Missed PixelChanged in PixelRope");
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
                        log.Info("Missed send to PixelRope");
                };
        }
    }
}
