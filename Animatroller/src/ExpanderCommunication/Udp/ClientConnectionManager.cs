using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace Animatroller.ExpanderCommunication
{
    public class ClientConnectionManager : BaseConnectionManager, IDisposable
    {
        private CancellationTokenSource cts;
        private UdpClient socket;
        private string connectToHostName;
        private int connectToPort;
        private string hostId;
        private Action<object> payloadReceivedAction;

        public ClientConnectionManager(string serverName, int serverPort, string hostId)
        {
            this.connectToHostName = serverName;
            this.connectToPort = serverPort;
            this.hostId = hostId;

            this.cts = new CancellationTokenSource();

            Task.Run(async () => await ConnectorWorker(this.cts.Token));
        }

        public void Dispose()
        {
            this.cts.Cancel();

            this.writer?.Dispose();
            this.writer = null;

            this.socket?.Dispose();
            this.socket = null;
        }

        private async Task SendToServerAsync(Model.BaseMessage message)
        {
            this.writer.WriteString(SerializeInternalMessage(message));

            await this.writer.StoreAsync();
        }

        private Task SendConnectMessageAsync()
        {
            var message = new Model.ConnectMessage
            {
                HostId = this.hostId
            };

            return SendToServerAsync(message);
        }

        public void SetPayloadReceivedAction(Action<object> payloadReceivedAction)
        {
            this.payloadReceivedAction = payloadReceivedAction;
        }

        public Task SendToServerAsync(object payload)
        {
            var message = new Model.PayloadMessage
            {
                Payload = SerializePayload(payload)
            };

            return SendToServerAsync(message);
        }

        private async Task ConnectorWorker(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    if (this.socket == null)
                    {
                        this.socket = new UdpClient();

                        var addresses = await Dns.GetHostAddressesAsync(this.connectToHostName);

                        this.socket.Connect(addresses.First(), this.connectToPort);

                        this.socket.BeginReceive(Socket_DataReceived, null);
                    }

                    await SendConnectMessageAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in ConnectorWorker " + ex.ToString());
                }

                cancelToken.WaitHandle.WaitOne(5000);
            }
        }

        private void Socket_DataReceived(IAsyncResult result)
        {
            try
            {
                IPEndPoint remoteEP = null;
                byte[] received = this.socket.EndReceive(result, ref remoteEP);
                this.socket.BeginReceive(Socket_DataReceived, null);

                var msg = DeserializeInternalMessage(Encoding.UTF8.GetString(received));

                var aliveMessage = msg as Model.AliveMessage;
                if (aliveMessage != null)
                {
                    Debug.WriteLine("Alive from server");
                }

                var payloadMessage = msg as Model.PayloadMessage;
                if (payloadMessage != null)
                {
                    object payloadObject = DeserializePayload(payloadMessage.Payload);

                    try
                    {
                        this.payloadReceivedAction?.Invoke(payloadObject);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception while invoking callback with payload object " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in Socket_DataReceivied " + ex.ToString());
            }
        }
    }
}
