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
using DotNetty.Transport.Channels.Groups;
using DotNetty.Common.Concurrency;
using DotNetty.Buffers;
using Serilog;

namespace Animatroller.ExpanderCommunication
{
    public class NettyServer : IServerCommunication
    {
        private ILogger log;
        private MultithreadEventLoopGroup bossGroup;
        private MultithreadEventLoopGroup workerGroup;
        private ServerBootstrap bootstrap;
        private IChannel boundChannel;
        private int listenPort;
        private Dictionary<string, IChannel> channels;

        public NettyServer(
            ILogger logger,
            int listenPort,
            Action<string, string, string, byte[]> dataReceivedAction,
            Action<string, string, System.Net.EndPoint> clientConnectedAction)
        {
            this.log = logger;
            this.listenPort = listenPort;

            this.channels = new Dictionary<string, IChannel>();
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

                   //TODO: Receive InstanceId in the pipeline instead of part of the data buffer
                   pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                   pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(32 * 1024, 0, 2, 0, 2));

                   pipeline.AddLast("main", new NettyServerHandler(this.log, this, dataReceivedAction, clientConnectedAction));
               }));
        }

        public async Task StartAsync()
        {
            this.boundChannel = await this.bootstrap.BindAsync(this.listenPort).ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            if (this.boundChannel != null)
            {
                await this.boundChannel.CloseAsync().ConfigureAwait(false);
                this.boundChannel = null;
            }

            await Task.WhenAll(
                this.bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                this.workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))).ConfigureAwait(false);
        }

        internal void SetInstanceIdChannel(string instanceId, IChannel channel)
        {
            lock (this)
            {
                this.channels[instanceId] = channel;
            }
        }

        public async Task<bool> SendToClientAsync(string instanceId, string messageType, byte[] data)
        {
            IChannel channel;
            lock (this)
            {
                if (!this.channels.TryGetValue(instanceId, out channel))
                    return false;
            }

            var buffer = Unpooled.Buffer(512 + data.Length);

            NettyClient.WriteStringToBuffer(buffer, messageType);
            buffer.WriteBytes(data);

            await channel.WriteAndFlushAsync(buffer);

            return true;
        }
    }
}
