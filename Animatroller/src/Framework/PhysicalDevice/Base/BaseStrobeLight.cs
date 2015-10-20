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
            if (logicalDevice is ISendsStrobeSpeed)
            {
                ((ISendsStrobeSpeed)logicalDevice).OutputStrobeSpeed.Subscribe(x =>
                {
                    this.strobeSpeed = x;

                    Output();
                });
            }
        }
    }
}
