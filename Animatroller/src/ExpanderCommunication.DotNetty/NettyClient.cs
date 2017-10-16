using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;
using System.Net.Sockets;
using Serilog;
using DotNetty.Buffers;

namespace Animatroller.ExpanderCommunication
{
    public class NettyClient : IClientCommunication
    {
        protected ILogger log;

        private string host;
        private int port;
        private IPAddress firstAddress;
        private string instanceId;
        private MultithreadEventLoopGroup group;
        private Bootstrap bootstrap;
        private IChannel clientChannel;
        private Action<string, byte[]> dataReceivedAction;
        private Action connectedAction;
        private CancellationTokenSource cts;
        private Task connectionTask;

        public NettyClient(ILogger logger, string host, int port, string instanceId, Action<string, byte[]> dataReceivedAction, Action connectedAction)
        {
            this.log = logger;
            this.host = host;
            this.port = port;
            this.instanceId = instanceId;
            this.dataReceivedAction = dataReceivedAction;
            this.connectedAction = connectedAction;
            this.cts = new CancellationTokenSource();

            var hostEntry = Task.Run(async () => await Dns.GetHostEntryAsync(host)).Result;
            this.firstAddress = hostEntry.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);

            this.group = new MultithreadEventLoopGroup();

            this.bootstrap = new Bootstrap();
            this.bootstrap
                .Group(group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    //TODO: Send InstanceId in the pipeline instead of part of the data buffer
                    pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                    pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(32 * 1024, 0, 2, 0, 2));

                    pipeline.AddLast("main", new NettyClientHandler(this.log, this));
                }));
        }

        public string Server
        {
            get { return $"{this.host}:{this.port}"; }
        }

        internal static void WriteStringToBuffer(IByteBuffer buffer, string input)
        {
            int typeBytes = Encoding.UTF8.GetByteCount(input);

            buffer.WriteByte(typeBytes);
            buffer.WriteBytes(Encoding.UTF8.GetBytes(input));
        }

        public async Task<bool> SendData(string messageType, byte[] data)
        {
            if (this.clientChannel == null || !this.clientChannel.Active)
                return false;

            var buffer = Unpooled.Buffer(512 + data.Length);

            WriteStringToBuffer(buffer, this.instanceId);
            WriteStringToBuffer(buffer, messageType);
            buffer.WriteBytes(data);

            await this.clientChannel?.WriteAndFlushAsync(buffer);

            return true;
        }

        public Task StartAsync()
        {
            this.connectionTask = Task.Run(async () =>
            {
                while (!this.cts.IsCancellationRequested)
                {
                    try
                    {
                        if (this.clientChannel == null || !this.clientChannel.Active)
                            this.clientChannel = await this.bootstrap.ConnectAsync(new IPEndPoint(this.firstAddress, this.port));
                    }
                    catch (Exception ex)
                    {
                        if (ex is AggregateException)
                            ex = ex.InnerException;
                        this.log.Warning("Exception in ConnectionTask: " + ex.Message);
                    }

                    this.cts.Token.WaitHandle.WaitOne(2000);
                }
            });

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            try
            {
                this.cts.Cancel();

                await this.clientChannel?.CloseAsync();
                await this.group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
            catch
            {
            }
        }

        internal void DataReceived(string messageType, byte[] data)
        {
            Task.Run(() =>
            {
                this.dataReceivedAction?.Invoke(messageType, data);
            });
        }

        internal void Connected()
        {
            Task.Run(() =>
            {
                this.connectedAction?.Invoke();
            });
        }
    }
}
