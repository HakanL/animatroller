using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication
{
    public interface IClientCommunication
    {
        Task StartAsync();

        Task StopAsync();

        Task<bool> SendData(string messageType, byte[] data);

        string Server { get; }
    }
}
