using Animatroller.AdminMessage;

namespace Animatroller.SceneRunner.SendControls
{
    public interface ISendControl
    {
        string ComponentId { get; }

        ComponentType ComponentType { get; }

        object GetMessageToSend();
    }
}
