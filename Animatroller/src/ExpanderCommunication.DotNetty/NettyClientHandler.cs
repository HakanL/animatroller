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
    internal class NettyClientHandler : ChannelHandlerAdapter
    {
        protected ILogger log;
        private NettyClient parent;

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
