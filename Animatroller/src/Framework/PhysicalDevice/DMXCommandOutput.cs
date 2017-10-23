using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.PhysicalDevice
{
    public class DMXCommandOutput : BaseDevice, INeedsDmxOutput
    {
        private int startDmxChannel;

        public DMXCommandOutput(IApiVersion3 logicalDevice, int startDmxChannel)
            : base(logicalDevice)
        {
            this.startDmxChannel = startDmxChannel;

            var sendsData = logicalDevice as ISendsData;
            if (sendsData != null)
            {
                sendsData.OutputChanged.Subscribe(x =>
                {
                    OutputFromIData(logicalDevice, x);
                });
            }
        }

        public IDmxOutput DmxOutputPort { protected get; set; }

        protected virtual void OutputFromIData(ILogicalDevice logicalDevice, IData data)
        {
            if (data.TryGetValue(DataElements.Command, out object value))
            {
                if (value is byte[] arr)
                {
                    foreach (byte b in arr)
                    {
                        DmxOutputPort.SendDimmerValue(this.startDmxChannel, b);
                    }
                }
            }
        }
    }
}
