using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework
{
    internal class MasterToken : IControlToken
    {
        private Dictionary<IReceivesData, IData> dataPerDevice;

        public MasterToken()
        {
            this.dataPerDevice = new Dictionary<IReceivesData, IData>();
        }

        public int Priority
        {
            get { return 0; }
        }

        public void Dispose()
        {
        }

        public IData GetDataForDevice(IOwnedDevice device)
        {
            var receivesData = device as IReceivesData;
            if (receivesData == null)
                throw new ArgumentException();

            IData data;

            lock (this.dataPerDevice)
            {
                if (!this.dataPerDevice.TryGetValue(receivesData, out data))
                {
                    data = new Data();

                    receivesData.BuildDefaultData(data);

                    this.dataPerDevice.Add(receivesData, data);
                }
            }

            return data;
        }

        public bool IsOwner(IControlToken checkToken)
        {
            return checkToken == this;
        }
    }
}
