using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class AudioEffectPlay : AudioBase
    {
        public string FileName { get; set; }

        public double? VolumeLeft { get; set; }

        public double? VolumeRight { get; set; }

        public bool Simultaneous { get; set; }
    }
}
