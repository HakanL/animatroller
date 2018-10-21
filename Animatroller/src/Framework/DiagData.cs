namespace Animatroller.Framework
{
    public enum Direction
    {
        Input,
        Output
    }

    public abstract class DiagData
    {
        public string Name { get; set; }

        public abstract string Display { get; }
    }

    public class DiagDataPortStatus : DiagData
    {
        public Direction Direction { get; set; }

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

    public abstract class SetupData
    {
        public string Name { get; set; }
    }

    public class SetupDataPort : SetupData
    {
        public Direction Direction { get; set; }

        public int Port { get; set; }

        public bool Value { get; set; }
    }
}
