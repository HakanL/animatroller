using System;
using NLog;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Hosting;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication
{
    public class SignalRServer : IDisposable, IServerCommunication
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private int listenPort;
        private IDisposable signalrServer;
        private object lockObject = new object();
        private Action<string, string, byte[]> dataReceivedAction;
        private Action<string, string> connectionIdUpdatedAction;

        public SignalRServer(
            int listenPort,
            Action<string, string> connectionIdUpdatedAction,
            Action<string, string, byte[]> dataReceivedAction)
        {
            this.listenPort = listenPort;
            this.connectionIdUpdatedAction = connectionIdUpdatedAction;
            this.dataReceivedAction = dataReceivedAction;

            GlobalHost.DependencyResolver.Register(typeof(SignalRHub), () =>
                new SignalRHub(this, log));
        }

        public Task<bool> SendToClientAsync(string connectionId, string messageType, byte[] data)
        {
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
            this.connectionIdUpdatedAction(instanceId, connectionId);
        }

        internal void DataReceived(string connectionId, string messageType, byte[] data)
        {
            this.dataReceivedAction?.Invoke(connectionId, messageType, data);
        }
    }
}
