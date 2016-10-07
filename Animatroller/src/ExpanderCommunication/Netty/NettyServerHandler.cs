using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Animatroller.ExpanderCommunication
{
    internal class NettyServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                Console.WriteLine("Received from client: " + buffer.ToString(Encoding.UTF8));
            }
            context.WriteAsync(message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }
}
