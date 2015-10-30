using System;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class PixelRope : BaseDevice, INeedsPixelOutput
    {
        private object lockObject = new object();
//        protected ColorBrightness[] pixelData;

        public IPixelOutput PixelOutputPort { protected get; set; }

        //public PixelRope(Pixel1D logicalDevice)
        //    : base(logicalDevice)
        //{
        //    logicalDevice.PixelChanged += (sender, e) =>
        //        {
        //            // Handles brightness as well

        //            var hsv = new HSV(e.NewColor);
        //            hsv.Value = hsv.Value * e.NewBrightness;
        //            var color = hsv.Color;

        //            if (System.Threading.Monitor.TryEnter(lockObject))
        //            {
        //                try
        //                {
        //                    PixelOutputPort.SendPixelValue(e.Channel, new PhysicalDevice.PixelRGBByte(color.R, color.G, color.B));
        //                }
        //                finally
        //                {
        //                    System.Threading.Monitor.Exit(lockObject);
        //                }
        //            }
        //            else
        //                log.Warn("Missed PixelChanged in PixelRope");
        //        };

        //    logicalDevice.MultiPixelChanged += (sender, e) =>
        //        {
        //            var values = new PhysicalDevice.PixelRGBByte[e.NewValues.Length];
        //            for (int i = 0; i < e.NewValues.Length; i++)
        //            {
        //                var hsv = new HSV(e.NewValues[i].Color);
        //                hsv.Value = hsv.Value * e.NewValues[i].Brightness;
        //                var color = hsv.Color;

        //                values[i] = new PixelRGBByte(color.R, color.G, color.B);
        //            }

        //            if (System.Threading.Monitor.TryEnter(lockObject))
        //            {
        //                try
        //                {
        //                    PixelOutputPort.SendPixelsValue(e.StartChannel, values);
        //                }
        //                finally
        //                {
        //                    System.Threading.Monitor.Exit(lockObject);
        //                }
        //            }
        //            else
        //                log.Warn("Missed send to PixelRope");
        //        };
        //}

        public PixelRope(VirtualPixel1D2 logicalDevice, int startVirtualPosition, int positions)
            : base(logicalDevice)
        {
//            this.pixelData = new ColorBrightness[positions];

            //logicalDevice.OutputData.Subscribe(x =>
            //{
            //    SetFromIData(x);

            //    Output();
            //});

            //SetFromIData(logicalDevice.CurrentData);


            logicalDevice.AddPixelDevice(startVirtualPosition, positions, x =>
            {
                var values = new PhysicalDevice.PixelRGBByte[x.Length];

                for (int i = 0; i < x.Length; i++)
                {
                    var color = x[i];

                    values[i] = new PixelRGBByte(color.R, color.G, color.B);
                }

                lock (this.lockObject)
                {
                    PixelOutputPort.SendPixelsValue(0, values);
                }
            });
            //logicalDevice.AddPixelDevice(startVirtualPosition, positions, (sender, e) =>
            //    {
            //        // Handles brightness as well
            //        var color = BaseLight.GetColorFromColorBrightness(e.NewColor, e.NewBrightness);

            //        lock (this.lockObject)
            //        {
            //            PixelOutputPort.SendPixelValue(e.Channel, new PhysicalDevice.PixelRGBByte(color.R, color.G, color.B));
            //        }
            //    }, (sender, e) =>
            //    {
            //        var values = new PhysicalDevice.PixelRGBByte[e.NewValues.Length];
            //        for (int i = 0; i < e.NewValues.Length; i++)
            //        {
            //            var color = BaseLight.GetColorFromColorBrightness(e.NewValues[i].Color, e.NewValues[i].Brightness);

            //            values[i] = new PixelRGBByte(color.R, color.G, color.B);
            //        }

            //        lock (this.lockObject)
            //        {
            //            PixelOutputPort.SendPixelsValue(e.StartChannel, values);
            //        }
            //    });
        }

        //public override void SetInitialState()
        //{
        //    base.SetInitialState();

        //    //Output();
        //}

        //private void SetFromIData(IData data)
        //{
        //    object value;

        //    if (data.TryGetValue(DataElements.PixelBrightness, out value))
        //    {
        //        double[] pixelBrightness = (double[])value;
        //        for (int i = 0; i < Math.Min(this.pixelData.Length, pixelBrightness.Length); i++)
        //            this.pixelData[i].Brightness = pixelBrightness[i];
        //    }

        //    if (data.TryGetValue(DataElements.PixelColor, out value))
        //    {
        //        Color[] pixelColor = (Color[])value;
        //        for (int i = 0; i < Math.Min(this.pixelData.Length, pixelColor.Length); i++)
        //            this.pixelData[i].Color = pixelColor[i];
        //    }
        //}
    }
}
