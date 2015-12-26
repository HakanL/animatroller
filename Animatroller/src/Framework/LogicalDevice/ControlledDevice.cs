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
        private IData data;

        public ControlledDevice(string name, int priority, Action<IData> populateData, Action<IControlToken> dispose)
        {
            this.Name = name;
            this.Priority = priority;
            this.disposeAction = dispose;
            this.data = new Data();
            populateData(this.data);
        }

        public void Dispose()
        {
            if (this.disposeAction != null)
                this.disposeAction(this);
        }

        public bool IsOwner(IControlToken checkToken)
        {
            return this == checkToken;
        }

        public IData GetDataForDevice(IOwnedDevice device)
        {
            return this.data;
        }

        public string Name { get; private set; }

        public int Priority { get; private set; }
    }
}
