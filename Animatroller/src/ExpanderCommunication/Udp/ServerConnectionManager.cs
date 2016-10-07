using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.ExpanderCommunication
{
    public class ServerConnectionManager : BaseConnectionManager, IDisposable
    {
        internal class Connection
        {
            //            private DataWriter writer;

            public DateTime LastAccess { get; private set; }

            public string HostId { get; set; }

            public Connection()
            {
            }

            public void Touch()
            {
                LastAccess = DateTime.UtcNow;
            }

            public TimeSpan Age
            {
                get { return DateTime.UtcNow - LastAccess; }
            }

            public void Close()
            {
                //                this.writer?.Dispose();
            }
        }

        private UdpClient listenSocket;
        private Dictionary<Tuple<string, string>, Connection> connectionCache;
        private Dictionary<string, Tuple<string, string>> hostIdLookup;
        private Timer cleanupTimer;
        private Action<string, object> payloadReceivedAction;

        public ServerConnectionManager(int listenPort)
        {
            this.connectionCache = new Dictionary<Tuple<string, string>, Connection>();
            this.hostIdLookup = new Dictionary<string, Tuple<string, string>>();

            this.listenSocket = new UdpClient(listenPort);

            this.listenSocket.BeginReceive(ReceiveCallback, null);

            this.cleanupTimer = new System.Threading.Timer(_ =>
            {
                lock (this.connectionCache)
                {
                    foreach (var kvp in this.connectionCache.ToList())
                    {
                        if (kvp.Value.Age.TotalMinutes > 10)
                        {
                            // Cleanup
                            this.connectionCache.Remove(kvp.Key);

                            try
                            {
                                kvp.Value.Close();
                            }
                            catch
                            {
                            }

                            lock (this.hostIdLookup)
                            {
                                foreach (var kvpHost in this.hostIdLookup.ToList())
                                {
                                    if (kvpHost.Value.Equals(kvp.Key))
                                    {
                                        this.hostIdLookup.Remove(kvpHost.Key);
                                    }
                                }
                            }
                        }
                    }
                }
            }, null, 30000, 60000);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                IPEndPoint remoteEP = null;
                byte[] buffer = this.listenSocket.EndReceive(result, ref remoteEP);

                this.listenSocket.BeginReceive(ReceiveCallback, null);
            }
            catch
            {
            }
        }

        public void SetPayloadReceivedAction(Action<string, object> payloadReceivedAction)
        {
            this.payloadReceivedAction = payloadReceivedAction;
        }

        public Task SendToClientAsync(string hostId, object payload)
        {
            var message = new Model.PayloadMessage
            {
                Payload = SerializePayload(payload)
            };

            return SendToClientAsync(hostId, message);
        }

        public void Dispose()
        {
            this.cleanupTimer.Dispose();

            foreach (var connection in this.connectionCache.Values)
            {
                try
                {
                    // Close
                    connection.Close();
                }
                catch
                {
                }
            }

            this.listenSocket?.Dispose();
            this.listenSocket = null;
        }

        private async Task SendToClientAsync(string hostId, Model.BaseMessage message)
        {
            Tuple<string, string> connectionKey;
            lock (this.hostIdLookup)
            {
                if (!this.hostIdLookup.TryGetValue(hostId, out connectionKey))
                    throw new ArgumentException("Unknown host id");
            }

            Connection connection;
            lock (this.connectionCache)
            {
                if (!this.connectionCache.TryGetValue(connectionKey, out connection))
                    throw new ArgumentException("Host is not connected");
            }

            var writer = await connection.GetWriterAsync(this.listenSocket,
                () => Tuple.Create(new HostName(connectionKey.Item1), connectionKey.Item2));

            writer.WriteString(SerializeInternalMessage(message));

            await writer.StoreAsync();
        }

        private Task SendAliveAsync(string hostId)
        {
            var message = new Model.AliveMessage();

            return SendToClientAsync(hostId, message);
        }

        private Tuple<string, string> GetConnectionKey(string remoteAddress, string remotePort)
        {
            return Tuple.Create(remoteAddress, remotePort);
        }

        private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                var connectionKey = GetConnectionKey(args.RemoteAddress, args.RemotePort);

                Connection connection;
                bool newConnection = false;
                lock (this.connectionCache)
                {
                    if (!this.connectionCache.TryGetValue(connectionKey, out connection))
                    {
                        connection = new Connection();
                        this.connectionCache.Add(connectionKey, connection);

                        newConnection = true;
                    }

                    connection.Touch();
                }

                var reader = args.GetDataReader();
                string received = reader.ReadString(reader.UnconsumedBufferLength);

                var msg = DeserializeInternalMessage(received);

                var connectMessage = msg as Model.ConnectMessage;
                if (connectMessage != null)
                {
                    if (newConnection)
                        Debug.WriteLine("New connection {0}:{1} from host {2}", args.RemoteAddress, args.RemotePort, connectMessage.HostId);

                    Debug.WriteLine("Connect from " + connectMessage.HostId);

                    lock (this.hostIdLookup)
                    {
                        this.hostIdLookup[connectMessage.HostId] = connectionKey;
                    }

                    connection.HostId = connectMessage.HostId;

                    // Send alive message
                    Task.Run(async () => await SendAliveAsync(connectMessage.HostId));
                }

                var payloadMessage = msg as Model.PayloadMessage;
                if (payloadMessage != null)
                {
                    object payloadObject = DeserializePayload(payloadMessage.Payload);

                    try
                    {
                        this.payloadReceivedAction?.Invoke(connection.HostId, payloadObject);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception while invoking callback with payload object " + ex.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                SocketErrorStatus socketError = SocketError.GetStatus(exception.HResult);
                if (socketError == SocketErrorStatus.ConnectionResetByPeer)
                {
                    // This error would indicate that a previous send operation resulted in an 
                    // ICMP "Port Unreachable" message.
                    //NotifyUserFromAsyncThread(
                    //    "Peer does not listen on the specific port. Please make sure that you run step 1 first " +
                    //    "or you have a server properly working on a remote server.",
                    //    NotifyType.ErrorMessage);
                }
                else if (socketError != SocketErrorStatus.Unknown)
                {
                    //NotifyUserFromAsyncThread(
                    //    "Error happened when receiving a datagram: " + socketError.ToString(),
                    //    NotifyType.ErrorMessage);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
