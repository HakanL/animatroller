using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class SendSerialRequest
    {
        public int Port { get; set; }

        public byte[] Data { get; set; }
    }
}
