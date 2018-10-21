//#define DEBUG_OSC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace Animatroller.Framework.Expander
{
    public class OscServer : IPort, IRunnable, ISupportsPersistence, IDisposable
    {
        protected class ConnectedClient
        {
            protected static readonly TimeSpan Timeout = TimeSpan.FromHours(2);

            private DateTime lastAccess;

            public OscClient Client { get; private set; }

            public ConnectedClient(IPAddress address, int port)
            {
                Client = new OscClient(address, port);
                this.lastAccess = DateTime.Now;
            }

            public ConnectedClient(IPEndPoint ipe)
                : this(ipe.Address, ipe.Port)
            {
            }

            public bool Expired
            {
                get { return (DateTime.Now - this.lastAccess) > Timeout; }
            }

            public void Touch()
            {
                this.lastAccess = DateTime.Now;
            }
        }

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

        protected ILogger log;
        private Rug.Osc.OscReceiver receiver;
        private readonly Task receiverTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private readonly Dictionary<string, Action<Message>> dispatch;
        private readonly Dictionary<string, Action<Message>> dispatchPartial;
        private Dictionary<IPEndPoint, ConnectedClient> clients;
        private readonly int forcedClientPort;
        private EventLoopScheduler scheduler;
        private readonly bool registerAutoHandlers;
        private readonly IObserver<(string, object[])> sender;
        private readonly Dictionary<string, object[]> queuedData;
        private readonly Dictionary<string, List<int>> portDiagStatus = new Dictionary<string, List<int>>();
        private readonly Dictionary<string, List<bool>> portCommandStatus = new Dictionary<string, List<bool>>();

        public OscServer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : this(Executor.Current.GetSetKey<int>(name, 8000))
        {
        }

        public OscServer(int listenPort, int forcedClientPort = 0, bool registerAutoHandlers = false)
        {
            this.forcedClientPort = forcedClientPort;
            this.registerAutoHandlers = registerAutoHandlers;
            this.log = Log.Logger;
            this.receiver = new Rug.Osc.OscReceiver(listenPort);
            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.dispatch = new Dictionary<string, Action<Message>>();
            this.dispatchPartial = new Dictionary<string, Action<Message>>();
            this.clients = new Dictionary<IPEndPoint, ConnectedClient>();
            this.scheduler = new EventLoopScheduler();
            this.queuedData = new Dictionary<string, object[]>();

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
                                    ConnectedClient connectedClient;

                                    var ipe = new IPEndPoint(packet.Origin.Address, forcedClientPort == 0 ? packet.Origin.Port : forcedClientPort);
                                    if (!this.clients.TryGetValue(ipe, out connectedClient))
                                    {
                                        connectedClient = new ConnectedClient(ipe);

                                        this.clients.Add(ipe, connectedClient);

                                        // Send all current data
                                        SendQueuedData();
                                    }

                                    connectedClient.Touch();
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
                                                    log.Debug("Received OSC message at {Address}: {Value}", oscMessage.Address, oscMessage);
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
                                        log.Debug("Received OSC message at {Address}: {Value}", msg.Address, msg);
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

            this.sender = Observer.NotifyOn(Observer.Create<(string Address, object[] Data)>(x =>
            {
                lock (this.clients)
                {
                    this.queuedData[x.Address] = x.Data;

                    foreach (var kvp in this.clients.ToList())
                    {
                        if (kvp.Value.Expired)
                        {
                            this.clients.Remove(kvp.Key);
                            continue;
                        }

                        kvp.Value.Client.Send(x.Address, true, x.Data);
                    }
                }
            }), this.scheduler);

            Executor.Current.Register(this);
        }

        private void SendQueuedData()
        {
            foreach (var kvp in this.queuedData.ToList())
            {
                this.sender.OnNext((kvp.Key, kvp.Value));
            }
        }

        public void SendAllClients(string address, params object[] data)
        {
            try
            {
                this.sender.OnNext((address, data));
            }
            catch
            {
                // Ignore
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

            if (this.registerAutoHandlers)
                RegisterAutoHandlers();

            Executor.Current.Diagnostics.Subscribe(x =>
            {
                switch (x)
                {
                    case DiagDataPortStatus portStatus:
                        string id = $"{x.Name}-{portStatus.Direction}";
                        List<int> status;
                        lock (portDiagStatus)
                        {
                            if (!portDiagStatus.TryGetValue(id, out status))
                            {
                                status = new List<int>();
                                portDiagStatus.Add(id, status);
                            }
                            while (portStatus.Port + 1 > status.Count)
                                status.Add(0);
                            status[portStatus.Port] = portStatus.Value ? 1 : 0;
                        }

                        SendAllClients($"/{id}/x", status.OfType<object>().ToArray());
                        break;

                    case DiagDataAudioPlayback audioPlayback:
                        SendAllClients($"/{x.Name}-{audioPlayback.Type}/Text", audioPlayback.Value);
                        break;
                }
            });

            foreach (string autoName in Executor.Current.AutoNames)
            {
                // Register OSC links for the auto names (expanders, etc)
                RegisterAction<bool>($"/{autoName}-Input/x", (msg, data) =>
                {
                    string id = $"{autoName}-Input";
                    List<bool> status;
                    lock (portCommandStatus)
                    {
                        if (!portCommandStatus.TryGetValue(id, out status))
                        {
                            status = new List<bool>();
                            portCommandStatus.Add(id, status);
                        }
                        while (data.Count > status.Count)
                            status.Add(false);
                    }
                    for (int index = 0; index < data.Count; index++)
                    {
                        if (status[index] != data[index])
                        {
                            Executor.Current.SetupCommands.OnNext(new SetupDataPort
                            {
                                Name = autoName,
                                Direction = Direction.Input,
                                Port = index,
                                Value = data[index]
                            });
                            status[index] = data[index];
                        }
                    }
                });

                RegisterAction<bool>($"/{autoName}-Output/x", (msg, data) =>
                {
                    string id = $"{autoName}-Output";
                    List<bool> status;
                    lock (portCommandStatus)
                    {
                        if (!portCommandStatus.TryGetValue(id, out status))
                        {
                            status = new List<bool>();
                            portCommandStatus.Add(id, status);
                        }
                        while (data.Count > status.Count)
                            status.Add(false);
                    }
                    for (int index = 0; index < data.Count; index++)
                    {
                        if (status[index] != data[index])
                        {
                            Executor.Current.SetupCommands.OnNext(new SetupDataPort
                            {
                                Name = autoName,
                                Direction = Direction.Output,
                                Port = index,
                                Value = data[index]
                            });
                            status[index] = data[index];
                        }
                    }
                });
            }
        }

        public void Stop()
        {
            this.cancelSource.Cancel();
            this.receiver.Close();
        }

        public void RegisterAutoHandlers()
        {
            var allInputs = Executor.Current.AllDevicesOfType<LogicalDevice.DigitalInput2>();

            foreach (var inputDevice in allInputs)
            {
                RegisterAction<int>($"/auto/{inputDevice.Name}", (msg, data) =>
                {
                    inputDevice.Control.OnNext(data.First() != 0);
                }, 1);
            }

            Executor.Current.MasterStatus.Subscribe(x =>
            {
                object data = null;

                if (x.Value is bool)
                    data = (bool)x.Value ? 1 : 0;

                if (data != null)
                    SendAllClients($"/auto/{x.Name}", data);
            });
        }

        public OscServer RegisterAction(string address, Action<Message> action)
        {
            if (address.EndsWith("*"))
                this.dispatchPartial[address.Substring(0, address.Length - 1)] = action;
            else
                this.dispatch[address] = action;

            return this;
        }

        public OscServer RegisterAction<T>(string address, Func<IReadOnlyList<T>, bool> predicate, Action<Message, IReadOnlyList<T>> action)
        {
            return RegisterAction<T>(address, (m, d) =>
            {
                if (predicate(d))
                    action(m, d);
            });
        }

        public OscServer RegisterAction<T>(string address, Action<Message, IReadOnlyList<T>> action, int? expectedArraySize = null)
        {
            Action<Message> invokeAction = msg =>
            {
                try
                {
                    var list = msg.Data.ToList().ConvertAll<T>(y => (T)Convert.ChangeType(y, typeof(T)));

                    if (expectedArraySize.HasValue && expectedArraySize.Value != list.Count)
                        // Ignore so we don't have to check in the action method
                        return;

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

        public bool PersistState
        {
            get { return true; }
        }

        public void SetValueFromPersistence(Func<string, string, string> getKeyFunc)
        {
            lock (this.clients)
            {
                var clientsIPList = JsonConvert.DeserializeObject<List<string>>(getKeyFunc("clients", "[]"));

                foreach (var clientEP in clientsIPList)
                {
                    string[] parts = clientEP.Split(':');
                    if (parts.Length != 2)
                        continue;

                    var ipe = new IPEndPoint(IPAddress.Parse(parts[0]), this.forcedClientPort == 0 ? int.Parse(parts[1]) : this.forcedClientPort);

                    this.clients[ipe] = new ConnectedClient(ipe);
                }
            }

            SendQueuedData();
        }

        public void SaveValueToPersistence(Action<string, string> setKeyFunc)
        {
            lock (this.clients)
            {
                string clientsIPList = JsonConvert.SerializeObject(this.clients.Select(x => $"{x.Key.Address}:{x.Key.Port}").ToList());
                setKeyFunc("clients", clientsIPList);
            }
        }

        public void Dispose()
        {
            this.scheduler?.Dispose();
            this.scheduler = null;
        }
    }
}
