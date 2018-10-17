namespace Animatroller.AdminMessage
{
    public class SceneUpdate
    {
        public SceneUpdateType Type { get; set; }

        public string MessageType { get; set; }

        public byte[] Object { get; set; }
    }
}
