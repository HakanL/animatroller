using System;
using System.Collections.Generic;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public enum AudioTypes
    {
        Background,
        Track,
        Effect
    }

    public class WhoAreYouRequest
    {
    }

    public class WhoAreYouResponse
    {
        public string InstanceId { get; set; }
    }

    public class InputChanged
    {
        public string Input { get; set; }

        public double Value { get; set; }
    }

    public class SetOutputRequest
    {
        public string Output { get; set; }

        public double Value { get; set; }

    }

    public class VideoPositionChanged
    {
        public string Id { get; set; }

        public double Position { get; set; }
    }

    public class AudioPositionChanged
    {
        public string Id { get; set; }

        public double Position { get; set; }

        public AudioTypes Type { get; set; }
    }

    public class VideoStarted
    {
        public string Id { get; set; }
    }

    public class VideoFinished
    {
        public string Id { get; set; }
    }

    public class AudioStarted
    {
        public string Id { get; set; }

        public AudioTypes Type { get; set; }
    }

    public class AudioFinished
    {
        public string Id { get; set; }

        public AudioTypes Type { get; set; }
    }
}
