using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledGroupData : IPushDataController
    {
        private IData sharedData;
        private IControlToken token;
        private IObserver<IData> observer;

        public ControlledGroupData(IControlToken token, IObserver<IData> observer)
        {
            this.sharedData = new Data();
            this.token = token;
            this.observer = observer;
        }

        public IData Data
        {
            get { return this.sharedData; }
        }

        public void PushData(IChannel channel)
        {
            this.observer.OnNext(this.sharedData);
        }

        public void SetDataFromIData(IData source)
        {
            if (source != null)
            {
                foreach (var kvp in source)
                    this.sharedData[kvp.Key] = kvp.Value;
            }
        }
    }
}
