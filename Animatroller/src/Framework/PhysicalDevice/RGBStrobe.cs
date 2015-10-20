using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class RGBStrobe : BaseDMXStrobeLight
    {
        public RGBStrobe(IApiVersion3 logicalDevice, int dmxChannel)
            : base(logicalDevice, dmxChannel)
        {
        }

        protected override void Output()
        {
            var color = GetColorFromColorBrightness();

            DmxOutputPort.SendDimmerValues(this.baseDmxChannel, new byte[] {
                color.R,
                color.G,
                color.B,
                (byte)(this.strobeSpeed == 0 ? 0 : 28),
                0,
                this.strobeSpeed.GetByteScale() });
        }
    }
}
