using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Serilog;

namespace Animatroller.ExpanderCommunication
{
    internal class NettyServerHandler : ChannelHandlerAdapter
    {
        protected ILogger log;
        private Action<string, string, string, byte[]> dataReceivedAction;
        private Action<string, string, System.Net.EndPoint> clientConnectedAction;
        private NettyServer parent;
        private bool clientConnectedInvoked;

        public NettyServerHandler(
            ILogger logger,
            NettyServer parent,
            Action<string, string, string, byte[]> dataReceivedAction,
            Action<string, string, System.Net.EndPoint> clientConnectedAction)
        {
            this.log = logger;
            this.dataReceivedAction = dataReceivedAction;
            this.clientConnectedAction = clientConnectedAction;
            this.parent = parent;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            this.log.Verbose("Channel {ChannelId} connected from {RemoteAddress}", context.Channel.Id.AsShortText(), context.Channel.RemoteAddress);

            this.clientConnectedInvoked = false;

            base.ChannelActive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            string channelId = context.Channel.Id.AsShortText();

            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                int stringLength = buffer.ReadByte();
                var b = new byte[stringLength];
                buffer.ReadBytes(b, 0, b.Length);
                string instanceId = Encoding.UTF8.GetString(b);

                stringLength = buffer.ReadByte();
                b = new byte[stringLength];
                buffer.ReadBytes(b, 0, b.Length);
                string messageType = Encoding.UTF8.GetString(b);

                this.parent.SetInstanceIdChannel(instanceId, context.Channel);

                if (!this.clientConnectedInvoked)
                {
                    this.clientConnectedAction?.Invoke(instanceId, channelId, context.Channel.RemoteAddress);
                    this.clientConnectedInvoked = true;
                }

                var data = new byte[buffer.ReadableBytes];
                buffer.GetBytes(buffer.ReaderIndex, data);

                Task.Run(() =>
                {
                    this.dataReceivedAction(instanceId, channelId, messageType, data);
                });
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (!(exception is ObjectDisposedException))
                this.log.Warning($"Exception in NettyServerHandler {context.Channel.Id.AsShortText()}: {exception.Message}");

            context.CloseAsync();
        }
    }
}
