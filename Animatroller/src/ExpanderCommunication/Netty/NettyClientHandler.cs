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
    internal class NettyClientHandler : ChannelHandlerAdapter
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private NettyClient parent;

        public NettyClientHandler(NettyClient parent)
        {
            this.parent = parent;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
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

                this.parent.DataReceived(messageType, buffer.ToArray());
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            log.Warn($"Exception in NettyClientHandler: {exception.Message}");

            context.CloseAsync();
        }
    }
}
