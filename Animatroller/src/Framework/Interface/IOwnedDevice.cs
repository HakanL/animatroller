using System;

namespace Animatroller.Framework
{
    public interface IOwnedDevice : IDevice
    {
        IControlToken TakeControl(int channel, int priority, string name = "");

        bool HasControl(IControlToken checkOwner);

        bool IsOwned { get; }

        IData GetFrameBuffer(int channel, IControlToken token, IReceivesData device);
    }
}
