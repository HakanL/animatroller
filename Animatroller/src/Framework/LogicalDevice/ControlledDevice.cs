using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledDevice : IControlToken
    {
        private static ControlledDevice empty = new ControlledDevice(string.Empty, false, -1, null);
        private Action dispose;

        public ControlledDevice(string name, bool hasControl, int priority, Action dispose)
        {
            this.Name = name;
            this.HasControl = hasControl;
            this.Priority = priority;
            this.dispose = dispose;
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
            if (this.dispose != null)
                this.dispose();
        }

        public string Name { get; private set; }

        public int Priority { get; private set; }

        public bool HasControl { get; private set; }
    }
}
