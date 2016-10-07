using System;
using System.Collections.Generic;
using System.Linq;
//using System.Reactive.Subjects;
using NLog;
using Newtonsoft.Json;
using Owin;
//using Microsoft.Owin.Hosting;
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
        private Action<string, Type, object> messageReceivedAction;

        public SignalRServer(int listenPort)
        {
            this.listenPort = listenPort;

            GlobalHost.DependencyResolver.Register(typeof(SignalRHub), () =>
                new SignalRHub(this, log));
        }

        public Task<bool> SendToClientAsync(string connectionId, byte[] data)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<SignalRHub>();

            IClientProxy client = context.Clients.Client(connectionId);

            if (client == null)
                return Task.FromResult(false);

            client.Invoke("HandleMessage", data);

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
            //FIXME
            Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            this.signalrServer?.Dispose();
            this.signalrServer = null;
        }

        internal void UpdateInstance(string instanceId, string connectionId)
        {
            //FIXME
        }

        public void SetMessageReceivedCallback(Action<string, Type, object> messageReceived)
        {
            this.messageReceivedAction = messageReceived;
        }

        public void SetKnownInstanceId(string instanceId, string connectionId)
        {
            //FIXME
            //lock (this.lockObject)
            //{
            //    this.connectionIdByInstanceId[instanceId] = connectionId;
            //    this.instanceIdByConnectionId[connectionId] = instanceId;
            //}
        }

        public void HandleMessage(string connectionId, Type messageType, object message)
        {
            this.messageReceivedAction?.Invoke(connectionId, messageType, message);
        }
    }
}
