using Microsoft.AspNet.SignalR.Client;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication
{
    public class SignalRClient : IClientCommunication
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private HubConnection connection;
        private IHubProxy hub;
        private bool attemptHubReconnect;
        private Action<string, byte[]> dataReceivedAction;

        public SignalRClient(string host, int port, string instanceId, Action<string, byte[]> dataReceivedAction)
        {
            this.dataReceivedAction = dataReceivedAction;
            this.attemptHubReconnect = true;

            this.connection = new HubConnection(
                string.Format("http://{0}:{1}/", host, port),
                string.Format("InstanceId={0}", instanceId));

#if DEBUG
            connection.DeadlockErrorTimeout = TimeSpan.FromMinutes(15);
#endif
            connection.TransportConnectTimeout = TimeSpan.FromSeconds(15);

            connection.Closed += () =>
            {
                if (this.attemptHubReconnect)
                {
                    log.Warn("Connection to {0}:{1} disconnected, trying reconnect", host, port);

                    Task.Delay(2000).ContinueWith(t => connection.Start());
                }
            };

            //connection.TraceLevel = TraceLevels.All;
            //connection.TraceWriter = Console.Out;

            connection.Error += error =>
            {
                log.Warn("SignalR error {0} for host {1}:{2}", error.Message, host, port);
            };

            connection.StateChanged += state =>
            {
                log.Trace("Connection to {0}:{1} changed state to {2}", host, port, state.NewState);
            };

            this.hub = connection.CreateHubProxy("ExpanderCommunicationHub");

            // Wire up messages
            this.hub.On<string, byte[]>("HandleMessage", DataReceived);
        }

        public string Server
        {
            get { return this.connection.Url; }
        }

        public Task<bool> SendData(string messageType, byte[] data)
        {
            if (this.connection.State == ConnectionState.Connected)
            {
                this.hub.Invoke("HandleMessage", messageType, data);

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task StartAsync()
        {
            // Start, ignore result here (caught by the event handlers)
            this.connection.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            this.attemptHubReconnect = false;
            this.connection?.Stop();
            this.connection?.Dispose();
            this.connection = null;

            return Task.CompletedTask;
        }

        internal void DataReceived(string messageType, byte[] data)
        {
            this.dataReceivedAction?.Invoke(messageType, data);
        }
    }
}
