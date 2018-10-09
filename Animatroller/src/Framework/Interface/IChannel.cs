using System;

namespace Animatroller.Framework
{
    public interface IChannel : IEquatable<IChannel>
    {
        int ChannelId { get; }
    }
}
