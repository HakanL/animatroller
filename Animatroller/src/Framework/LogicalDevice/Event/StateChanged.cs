using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class StateChangedEventArgs : EventArgs
    {
        public bool NewState { get; private set; }

        public StateChangedEventArgs(bool newState)
        {
            this.NewState = newState;
        }
    }
}
