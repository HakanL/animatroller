using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class AudioFinished : AudioBase
    {
        public string Id { get; set; }

        public AudioTypes Type { get; set; }
    }
}
