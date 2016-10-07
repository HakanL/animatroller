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

        Task<bool> SendToClientAsync(string connectionId, string messageType, byte[] data);
    }
}
