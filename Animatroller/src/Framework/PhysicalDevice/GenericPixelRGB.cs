using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class GenericPixelRGB : BaseLight, INeedsDmxOutput
    {
        protected int baseDmxChannel;

        public IDmxOutput DmxOutputPort { protected get; set; }

        public GenericPixelRGB(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        protected override void Output()
        {
            var color = GetColorFromColorBrightness();

            DmxOutputPort.SendDimmerValues(baseDmxChannel, new byte[] { color.R, color.G, color.B });
        }
    }
}
