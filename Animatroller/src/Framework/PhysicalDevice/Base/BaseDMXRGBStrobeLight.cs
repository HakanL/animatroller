using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseDMXRGBStrobeLight : BaseRGBStrobeLight, INeedsDmxOutput
    {
        protected int baseDmxChannel;

        public IDmxOutput DmxOutputPort { protected get; set; }

        public BaseDMXRGBStrobeLight(ColorDimmer logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public BaseDMXRGBStrobeLight(ColorDimmer2 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public BaseDMXRGBStrobeLight(StrobeColorDimmer logicalDevice, int dmxChannel)
            : this((ColorDimmer)logicalDevice, dmxChannel)
        {
            this.baseDmxChannel = dmxChannel;
        }

        public BaseDMXRGBStrobeLight(StrobeColorDimmer2 logicalDevice, int dmxChannel)
            : this((ColorDimmer2)logicalDevice, dmxChannel)
        {
            this.baseDmxChannel = dmxChannel;
        }
    }
}
