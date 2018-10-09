using System;

namespace Animatroller.Framework
{
    public class Channel : IChannel
    {
        public int ChannelId { get; private set; }

        public static Channel Main { get; } = new Channel
        {
            ChannelId = 0
        };

        public static Channel FromId(int channelId)
        {
            return new Channel
            {
                ChannelId = channelId
            };
        }

        public bool Equals(IChannel other)
        {
            return other != null && other.ChannelId == ChannelId;
        }

        public override int GetHashCode()
        {
            return ChannelId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IChannel);
        }
    }
}
