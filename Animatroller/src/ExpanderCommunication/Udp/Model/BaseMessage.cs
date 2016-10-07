using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication.Model
{
    internal abstract class BaseMessage
    {
        public enum MessageTypes
        {
            Connect,
            Alive,
            Payload
        }

        public Guid MessageId { get; private set; }

        public DateTime Timestamp { get; private set; }

        public MessageTypes Type { get; private set; }

        protected BaseMessage(MessageTypes type)
        {
            Type = type;
            MessageId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
}
