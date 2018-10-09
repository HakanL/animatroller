using System;
using System.Collections.Generic;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class Pixel2D : BaseDevice, INeedsPixel2DOutput
    {
        private object lockObject = new object();

        public IPixelOutput PixelOutputPort { protected get; set; }

        public Pixel2D(VirtualPixel2D3 logicalDevice, Dictionary<int, Utility.PixelMap[]> pixelMapping)
            : base(logicalDevice)
        {
            logicalDevice.AddPixelDevice(pixelMapping, dmxData =>
            {
                lock (this.lockObject)
                {
                    PixelOutputPort.SendMultiUniverseDmxData(dmxData);
                }
            });
        }
    }
}
