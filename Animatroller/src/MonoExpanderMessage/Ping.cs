using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class Ping
    {
        public string HostName { get; set; }

        public string Version { get; set; }

        public int Inputs { get; set; }

        public int Outputs { get; set; }
    }
}
