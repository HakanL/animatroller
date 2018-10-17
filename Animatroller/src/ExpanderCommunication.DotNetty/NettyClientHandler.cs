using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Serilog;
using System;
using System.Text;

namespace Animatroller.ExpanderCommunication
{
    internal class NettyClientHandler : ChannelHandlerAdapter
    {
        protected ILogger log;
        private readonly NettyClient parent;

        public NettyClientHandler(ILogger logger, NettyClient parent)
        {
            this.log = logger;
            this.parent = parent;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            this.parent.Connected();

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
                string messageType = Encoding.UTF8.GetString(b);

                var data = new byte[buffer.ReadableBytes];
                buffer.GetBytes(buffer.ReaderIndex, data);

                buffer.Release();

                this.parent.DataReceived(messageType, data);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            this.log.Warning($"Exception in NettyClientHandler: {exception.Message}");

            context.CloseAsync();
        }
    }
}
