#define DEBUG_OSC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using NLog;
using System.Net;

namespace Animatroller.Framework.Expander
{
    public class OscServer : IPort, IRunnable
    {
        public class Message
        {
            public string Address { get; private set; }
            public Rug.Osc.OscMessage RawMessage { get; private set; }
            public IEnumerable<object> Data { get; private set; }

            public Message(Rug.Osc.OscMessage message)
            {
                this.Address = message.Address;
                this.RawMessage = message;
                this.Data = message;
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        private Rug.Osc.OscReceiver receiver;
        private Task receiverTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private Dictionary<string, Action<Message>> dispatch;
        private Dictionary<string, Action<Message>> dispatchPartial;
        private Dictionary<IPEndPoint, OscClient> clients;

        public OscServer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : this(Executor.Current.GetSetKey<int>(name, 9999))
        {
        }

        public OscServer(int listenPort)
        {
            this.receiver = new Rug.Osc.OscReceiver(listenPort);
            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.dispatch = new Dictionary<string, Action<Message>>();
            this.dispatchPartial = new Dictionary<string, Action<Message>>();
            this.clients = new Dictionary<IPEndPoint, OscClient>();

            this.receiverTask = new Task(x =>
            {
                try
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        while (this.receiver.State != Rug.Osc.OscSocketState.Closed)
                        {
                            if (this.receiver.State == Rug.Osc.OscSocketState.Connected)
                            {
                                var packet = this.receiver.Receive();

                                lock (this.clients)
                                {
                                    if (!this.clients.ContainsKey(packet.Origin))
                                    {
                                        this.clients.Add(packet.Origin, new OscClient(packet.Origin.Address, packet.Origin.Port));
                                    }
                                }

                                if (packet is Rug.Osc.OscBundle)
                                {
                                    var bundles = (Rug.Osc.OscBundle)packet;
                                    if (bundles.Any())
                                    {
                                        foreach (var bundle in bundles)
                                        {
                                            var oscMessage = bundle as Rug.Osc.OscMessage;
                                            if (oscMessage != null)
                                            {
#if DEBUG_OSC
                                                if (oscMessage.Address != "/ping")
                                                    log.Debug("Received OSC message: {0}", oscMessage);
#endif

                                                Invoke(oscMessage);
                                            }
                                        }
                                    }
                                }

                                if (packet is Rug.Osc.OscMessage)
                                {
                                    var msg = (Rug.Osc.OscMessage)packet;

#if DEBUG_OSC
                                    if (msg.Address != "/ping")
                                        log.Debug("Received OSC message: {0}", msg);
#endif

                                    Invoke(msg);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            Executor.Current.Register(this);
        }

        public void SendAllClients(string address, params object[] data)
        {
            foreach (var client in this.clients.Values)
            {
                client.Send(address, true, data);
            }
        }

        private void Invoke(Rug.Osc.OscMessage oscMessage)
        {
            var message = new Message(oscMessage);

            Action<Message> action;
            if (this.dispatch.TryGetValue(oscMessage.Address, out action))
            {
                try
                {
                    action.Invoke(message);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Error while dispatching OSC message");
                }
            }

            foreach (var kvp in this.dispatchPartial)
            {
                if (kvp.Key == string.Empty || message.Address.StartsWith(kvp.Key))
                {
                    try
                    {
                        kvp.Value.Invoke(message);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Error while dispatching OSC message");
                    }
                }
            }
        }

        public void Start()
        {
            this.receiverTask.Start();
            this.receiver.Connect();
        }

        public void Stop()
        {
            this.cancelSource.Cancel();
            this.receiver.Close();
        }

        public OscServer RegisterAction(string address, Action<Message> action)
        {
            if (address.EndsWith("*"))
                this.dispatchPartial[address.Substring(0, address.Length - 1)] = action;
            else
                this.dispatch[address] = action;

            return this;
        }

        public OscServer RegisterAction<T>(string address, Func<IEnumerable<T>, bool> predicate, Action<Message, IEnumerable<T>> action)
        {
            return RegisterAction<T>(address, (m, d) =>
            {
                if (predicate(d))
                    action(m, d);
            });
        }

        public OscServer RegisterAction<T>(string address, Action<Message, IEnumerable<T>> action)
        {
            Action<Message> invokeAction = msg =>
            {
                try
                {
                    var list = msg.Data.ToList().ConvertAll<T>(y => (T)Convert.ChangeType(y, typeof(T)));
                    action(msg, list);
                }
                catch
                {

                }
            };

            return RegisterAction(address, invokeAction);
        }

        public OscServer RegisterAction<T1, T2>(string address, Action<Message, T1, IEnumerable<T2>> action)
        {
            Action<Message> invokeAction = msg =>
            {
                try
                {
                    T1 value1 = (T1)Convert.ChangeType(msg.Data.First(), typeof(T1));

                    var list = msg.Data
                        .Skip(1).ToList()
                        .ConvertAll<T2>(y => (T2)Convert.ChangeType(y, typeof(T2)));
                    action(msg, value1, list);
                }
                catch
                {

                }
            };

            return RegisterAction(address, invokeAction);
        }

        public OscServer RegisterActionSimple<T>(string address, Action<Message, T> action)
        {
            Action<Message> invokeAction = msg =>
            {
                var list = msg.Data.ToList().ConvertAll<T>(y => (T)Convert.ChangeType(y, typeof(T)));
                if (list.Count == 1)
                    action(msg, list.First());
            };

            if (address.EndsWith("*"))
                this.dispatchPartial[address.Substring(0, address.Length - 1)] = invokeAction;
            else
                this.dispatch[address] = invokeAction;

            return this;
        }
    }
}
