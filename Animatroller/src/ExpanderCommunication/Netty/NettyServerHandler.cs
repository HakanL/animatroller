using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using NLog;

namespace Animatroller.ExpanderCommunication
{
    internal class NettyServerHandler : ChannelHandlerAdapter
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private Action<string, string, string, byte[]> dataReceivedAction;
        private Action<string, string> clientConnectedAction;
        private NettyServer parent;
        private bool clientConnectedInvoked;

        public NettyServerHandler(
            NettyServer parent,
            Action<string, string, string, byte[]> dataReceivedAction,
            Action<string, string> clientConnectedAction)
        {
            this.dataReceivedAction = dataReceivedAction;
            this.clientConnectedAction = clientConnectedAction;
            this.parent = parent;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            log.Info($"Channel {context.Channel.Id.AsShortText()} connected");

            this.clientConnectedInvoked = false;

            base.ChannelActive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
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
                    this.clientConnectedAction?.Invoke(instanceId, context.Channel.Id.AsShortText());
                    this.clientConnectedInvoked = true;
                }

                Task.Run(() =>
                {
                    this.dataReceivedAction(instanceId, context.Channel.Id.AsShortText(), messageType, buffer.ToArray());
                });
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (!(exception is ObjectDisposedException))
                log.Warn($"Exception in NettyServerHandler {context.Channel.Id.AsShortText()}: {exception.Message}");

            context.CloseAsync();
        }
    }
}
