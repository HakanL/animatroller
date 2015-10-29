using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledDevice : IControlToken
    {
        private static ControlledDevice empty = new ControlledDevice(string.Empty, -1, null);
        private Action disposeAction;
        private IData data;

        public ControlledDevice(string name, int priority, Action dispose)
        {
            this.Name = name;
            this.Priority = priority;
            this.disposeAction = dispose;
            this.data = new Data();
        }

        public static ControlledDevice Empty
        {
            get
            {
                return empty;
            }
        }

        public void Dispose()
        {
            if (this.disposeAction != null)
                this.disposeAction();
        }

        public void PushData(DataElements dataElement, object value)
        {
            this.data[dataElement] = value;
        }

        public IData Data
        {
            get { return this.data; }
        }

        public string Name { get; private set; }

        public int Priority { get; private set; }
    }
}
