using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledDevice : IControlToken
    {
        private static ControlledDevice empty = new ControlledDevice(string.Empty, -1, null, null);
        private Action<Dictionary<string, object>> disposeAction;
        private Dictionary<string, object> state;

        public ControlledDevice(string name, int priority, Dictionary<string, object> state, Action<Dictionary<string, object>> dispose)
        {
            this.Name = name;
            this.Priority = priority;
            this.state = state;
            this.disposeAction = dispose;
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
                this.disposeAction(this.state);
        }

        public string Name { get; private set; }

        public int Priority { get; private set; }
    }
}
