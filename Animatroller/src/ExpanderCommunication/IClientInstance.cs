using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication
{
    public interface IClientInstance
    {
        void SetSendAction(Action<object> sendAction);

//        void HandleMessage(string connectionId, Type messageType, object message);

        void UpdateInstance(string instanceId, string connectionId);
    }
}
