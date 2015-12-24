using System;

namespace Animatroller.Framework
{
    public interface IOwnedDevice : IDevice
    {
        IControlToken TakeControl(int priority, string name = "");

        bool HasControl(IControlToken checkOwner);

        bool IsOwned { get; }

        IData GetFrameBuffer(IControlToken token, IReceivesData device);
    }
}
