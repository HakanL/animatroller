using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class AudioPositionChanged : AudioBase
    {
        public string Id { get; set; }

        public double Position { get; set; }

        public AudioTypes Type { get; set; }
    }
}
