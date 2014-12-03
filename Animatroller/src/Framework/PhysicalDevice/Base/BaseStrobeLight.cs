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

        public BaseStrobeLight(ColorDimmer logicalDevice)
            : base(logicalDevice)
        {
            var strobe = logicalDevice as StrobeColorDimmer;
            if (strobe != null)
            {
                strobe.StrobeSpeedChanged += (sender, e) =>
                {
                    this.strobeSpeed = e.NewSpeed;

                    Output();
                };
            }
        }

        public BaseStrobeLight(Dimmer logicalDevice)
            : base(logicalDevice)
        {
            var strobe = logicalDevice as StrobeDimmer;
            if (strobe != null)
            {
                strobe.StrobeSpeedChanged += (sender, e) =>
                {
                    this.strobeSpeed = e.NewSpeed;

                    Output();
                };
            }
        }

        public BaseStrobeLight(ColorDimmer2 logicalDevice)
            : base(logicalDevice)
        {
            var strobe = logicalDevice as StrobeColorDimmer2;
            if (strobe != null)
            {
                strobe.InputStrobeSpeed.Subscribe(x =>
                {
                    this.strobeSpeed = x.Value;

                    Output();
                });
            }
        }

        public BaseStrobeLight(ILogicalDevice logicalDevice)
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
