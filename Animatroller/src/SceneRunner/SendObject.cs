using System;

namespace Animatroller.SceneRunner
{
    public class SendObject
    {
        public string ComponentId { get; set; }

        public SendControls.ISendControl SendControl { get; set; }

        public byte[] LastHash { get; set; }

        public DateTime LastSend { get; set; }
    }
}
