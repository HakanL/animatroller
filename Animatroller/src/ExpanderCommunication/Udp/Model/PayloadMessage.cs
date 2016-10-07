using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication.Model
{
    internal class PayloadMessage : BaseMessage
    {
        public byte[] Payload { get; set; }

        public PayloadMessage()
            : base(MessageTypes.Payload)
        {
        }
    }
}
