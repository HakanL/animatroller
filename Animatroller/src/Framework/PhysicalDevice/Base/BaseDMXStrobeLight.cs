using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseDMXStrobeLight : BaseStrobeLight, INeedsDmxOutput
    {
        protected int baseDmxChannel;

        public IDmxOutput DmxOutputPort { protected get; set; }

        public BaseDMXStrobeLight(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice)
        {
            this.baseDmxChannel = dmxChannel;
        }
    }
}
