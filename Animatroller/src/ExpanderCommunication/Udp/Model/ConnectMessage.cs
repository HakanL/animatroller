using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication.Model
{
    internal class ConnectMessage : BaseMessage
    {
        public string HostId { get; set; }

        public ConnectMessage()
            : base(MessageTypes.Connect)
        {
        }
    }
}
