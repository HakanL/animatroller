using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public abstract class BaseStrobeLight : BaseLight
    {
        protected double strobeSpeed;

        public BaseStrobeLight(IApiVersion3 logicalDevice)
            : base(logicalDevice)
        {
        }

        protected override void SetFromIData(ILogicalDevice logicalDevice, IData data)
        {
            base.SetFromIData(logicalDevice, data);

            object value;
            if (data.TryGetValue(DataElements.StrobeSpeed, out value))
                this.strobeSpeed = (double)value;
        }
    }
}
