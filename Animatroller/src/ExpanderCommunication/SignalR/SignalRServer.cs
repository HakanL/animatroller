using System;
using NLog;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Hosting;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Animatroller.ExpanderCommunication
{
    public class SignalRServer : IDisposable, IServerCommunication
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private int listenPort;
        private IDisposable signalrServer;
        private object lockObject = new object();
        private Action<string, string, string, byte[]> dataReceivedAction;
        private Dictionary<string, string> instanceConnectionId;

        public SignalRServer(
            int listenPort,
            Action<string, string, string, byte[]> dataReceivedAction)
        {
            this.listenPort = listenPort;
            this.dataReceivedAction = dataReceivedAction;
            this.instanceConnectionId = new Dictionary<string, string>();

            GlobalHost.DependencyResolver.Register(typeof(SignalRHub), () =>
                new SignalRHub(this, log));
        }

        public Task<bool> SendToClientAsync(string instanceId, string messageType, byte[] data)
        {
            string connectionId;
            if (!this.instanceConnectionId.TryGetValue(instanceId, out connectionId))
                return Task.FromResult(false);

            var context = GlobalHost.ConnectionManager.GetHubContext<SignalRHub>();

            IClientProxy client = context.Clients.Client(connectionId);

            if (client == null)
                return Task.FromResult(false);

            client.Invoke("HandleMessage", messageType, data);

            return Task.FromResult(true);
        }

        public Task StartAsync()
        {
            var startOptions = new StartOptions(string.Format("http://+:{0}/", this.listenPort))
            {
                ServerFactory = "Microsoft.Owin.Host.HttpListener"
            };
            this.signalrServer = WebApp.Start<SignalRServerStartup>(startOptions);

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            this.signalrServer?.Dispose();
            this.signalrServer = null;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Task.Run(async () => await StopAsync()).Wait();
        }

        internal void UpdateInstance(string instanceId, string connectionId)
        {
            this.instanceConnectionId[instanceId] = connectionId;
        }

        internal void DataReceived(string instanceId, string connectionId, string messageType, byte[] data)
        {
            this.dataReceivedAction?.Invoke(instanceId, connectionId, messageType, data);
        }
    }
}
