using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledDevice : IControlToken
    {
        private Action<IControlToken> disposeAction;
        private Dictionary<IChannel, IData> dataPerChannel;
        private Action<IData> populateData;
        private object lockObject = new object();

        public ControlledDevice(string name, int priority, Action<IData> populateData, Action<IControlToken> dispose)
        {
            this.Name = name;
            this.Priority = priority;
            this.disposeAction = dispose;
            this.populateData = populateData;
            this.dataPerChannel = new Dictionary<IChannel, IData>();
        }

        public void Dispose()
        {
            this.disposeAction?.Invoke(this);
        }

        public bool IsOwner(IControlToken checkToken)
        {
            return this == checkToken;
        }

        public IData GetDataForDevice(IOwnedDevice device, IChannel channel)
        {
            lock (this.lockObject)
            {
                if (!this.dataPerChannel.TryGetValue(channel ?? Channel.Main, out IData data))
                {
                    data = new Data();
                    this.populateData(data);
                    this.dataPerChannel[channel ?? Channel.Main] = data;
                }

                return data;
            }
        }

        public string Name { get; private set; }

        public int Priority { get; private set; }
    }
}
