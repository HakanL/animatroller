namespace Animatroller.Framework
{
    public abstract class DiagData
    {
        public string Name { get; set; }

        public abstract string Display { get; }
    }

    public class DiagDataPortStatus : DiagData
    {
        public int Port { get; set; }

        public bool Value { get; set; }

        public override string Display => $"Port {Port} set to {Value}";
    }

    public class DiagDataAudioPlayback : DiagData
    {
        public string Type { get; set; }

        public string Value { get; set; }

        public override string Display => $"Playing {Value} as {Type}";
    }
}
