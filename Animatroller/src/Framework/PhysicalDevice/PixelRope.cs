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
                            PixelOutputPort.SendPixelValue(e.Channel, color.R, color.G, color.B);
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(lockObject);
                        }
                    }
                    else
                        Console.WriteLine("Missed PixelChanged in PixelRobe");
                };

            logicalDevice.MultiPixelChanged += (sender, e) =>
                {
                    byte[] values = new byte[e.NewValues.Length * 4 - 1];
                    for (int i = 0; i < e.NewValues.Length; i++)
                    {
                        var hsv = new HSV(e.NewValues[i].Color);
                        hsv.Value = hsv.Value * e.NewValues[i].Brightness;
                        var color = hsv.Color;

                        values[i * 4 + 0] = color.R;
                        values[i * 4 + 1] = color.G;
                        values[i * 4 + 2] = color.B;
                        if (i < e.NewValues.Length - 1)
                            values[i * 4 + 3] = 32;     // Delimiter
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
                        Console.WriteLine("Missed send to PixelRope");
                };
        }
    }
}
