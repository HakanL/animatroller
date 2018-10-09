using System;

namespace Animatroller.Framework
{
    public interface IOwnedDevice : IDevice
    {
        IControlToken TakeControl(IChannel channel, int priority, string name = "");

        bool HasControl(IControlToken checkOwner);

        bool IsOwned { get; }

        IData GetFrameBuffer(IChannel channel, IControlToken token, IReceivesData device);
    }
}
