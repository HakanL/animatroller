using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication
{
    public interface IServerCommunication
    {
        Task StartAsync();

        Task StopAsync();

        void SetMessageReceivedCallback(Action<string, Type, object> messageReceived);

        Task<bool> SendToClientAsync(string connectionId, byte[] data);
    }
}
