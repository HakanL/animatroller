using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.PhysicalDevice
{
    public class FogMachineA : BaseDMXStrobeLight
    {
        private double pumpThroughput;

        public FogMachineA(IApiVersion3 pumpLogicalDevice, IApiVersion3 lightLogicalDevice, int startDmxChannel)
            : base(lightLogicalDevice, startDmxChannel)
        {
            var sendsData = pumpLogicalDevice as ISendsData;
            if (sendsData != null)
            {
                sendsData.OutputChanged.Subscribe(x =>
                {
                    SetPumpFromIData(pumpLogicalDevice, x);

                    Output();
                });

                SetPumpFromIData(pumpLogicalDevice, sendsData.CurrentData);
            }
        }

        protected virtual void SetPumpFromIData(ILogicalDevice logicalDevice, IData data)
        {
            bool masterPower = true;
            if (logicalDevice is IHasMasterPower masterPowerDevice)
                masterPower = masterPowerDevice.MasterPower;

            if (data.TryGetValue(DataElements.Throughput, out object value))
                this.pumpThroughput = (double)value * (masterPower ? 1 : 0);
            else
            {
                bool? power = data.GetValue<bool>(DataElements.Power);
                if (power.HasValue)
                    this.pumpThroughput = (power.Value && masterPower) ? 1 : 0;
            }
        }

        protected override void Output()
        {
            var color = GetColorFromColorBrightness();

            DmxOutputPort.SendDimmerValues(this.baseDmxChannel, new byte[] {
                this.pumpThroughput.GetByteScale(),
                color.R,
                color.G,
                color.B,
                (byte)(this.strobeSpeed == 0 ? 0 : 255),
                this.strobeSpeed.GetByteScale() });
        }
    }
}
