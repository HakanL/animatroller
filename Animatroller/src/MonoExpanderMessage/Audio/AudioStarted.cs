using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class AudioStarted : AudioBase
    {
        public string Id { get; set; }

        public AudioTypes Type { get; set; }
    }
}
