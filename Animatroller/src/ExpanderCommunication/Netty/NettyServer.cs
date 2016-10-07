using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Animatroller.ExpanderCommunication
{
    public class NettyServer : IServerCommunication
    {
        private MultithreadEventLoopGroup bossGroup;
        private MultithreadEventLoopGroup workerGroup;
        private ServerBootstrap bootstrap;
        private IChannel boundChannel;
        private int listenPort;
        private Action<string, string, byte[]> dataReceivedAction;

        public NettyServer(int listenPort)
        {
            this.listenPort = listenPort;

            this.bossGroup = new MultithreadEventLoopGroup(1);
            this.workerGroup = new MultithreadEventLoopGroup();

            this.bootstrap = new ServerBootstrap();
            bootstrap
               .Group(bossGroup, workerGroup)
               .Channel<TcpServerSocketChannel>()
               .Option(ChannelOption.SoBacklog, 100)
               .Handler(new LoggingHandler("SRV-LSTN"))
               .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
               {
                   IChannelPipeline pipeline = channel.Pipeline;
                   //pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                   pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                   pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(10240, 0, 2, 0, 2));

                   pipeline.AddLast("main", new NettyServerHandler());
               }));
        }

        public async Task StartAsync()
        {
            this.boundChannel = await this.bootstrap.BindAsync(this.listenPort);
        }

        public async Task StopAsync()
        {
            await this.boundChannel?.CloseAsync();

            await Task.WhenAll(
                this.bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                this.workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
        }

        public void SetDataReceivedCallback(Action<string, string, byte[]> dataReceived)
        {
            this.dataReceivedAction = dataReceived;
        }

        public Task<bool> SendToClientAsync(string connectionId, string messageType, byte[] data)
        {
            return Task.FromResult(false);
        }
    }
}
