using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using NLog;
using Newtonsoft.Json;
using Owin;
using Microsoft.Owin.Hosting;
using System.Threading.Tasks;
using System.IO;

namespace Animatroller.Framework.Expander
{
    public interface IMonoExpanderServerRepository
    {
        void SetKnownInstanceId(string instanceId, string connectionId);

        //        void HandleMessage(string connectionId, object message);

        string ExpanderSharedFiles { get; }

    }

    public class MonoExpanderServer : IDisposable, IPort, IRunnable, IMonoExpanderServerRepository
    {
        public enum CommunicationTypes
        {
            Unknown = 0,
            SignalR = 1,
            Netty = 2
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        private string name;
        private Dictionary<string, MonoExpanderInstance> clientInstances;
        private Dictionary<string, string> connectionIdByInstanceId;
        private Dictionary<string, string> instanceIdByConnectionId;
        private IDisposable signalrServer;
        private object lockObject = new object();
        private ExpanderCommunication.IServerCommunication serverCommunication;

        public MonoExpanderServer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(
                name: name,
                listenPort: Executor.Current.GetSetKey(this, name + ".listenPort", 8081),
                communicationType: CommunicationTypes.SignalR);
        }

        public MonoExpanderServer(int listenPort, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(
                name: name,
                listenPort: listenPort,
                communicationType: CommunicationTypes.SignalR);
        }

        private void Initialize(string name, int listenPort, CommunicationTypes communicationType)
        {
            this.name = name;
            this.clientInstances = new Dictionary<string, MonoExpanderInstance>();
            this.connectionIdByInstanceId = new Dictionary<string, string>();
            this.instanceIdByConnectionId = new Dictionary<string, string>();

            switch (communicationType)
            {
                case CommunicationTypes.SignalR:
                    this.serverCommunication = new ExpanderCommunication.SignalRServer(listenPort);
                    break;

                case CommunicationTypes.Netty:
                    this.serverCommunication = new ExpanderCommunication.NettyServer(listenPort);
                    break;

                default:
                    throw new ArgumentException("Communication Type");
            }

            this.serverCommunication.SetMessageReceivedCallback((connectionId, messageType, message) =>
                {
                    HandleMessage(connectionId, messageType, message);
                });

            Executor.Current.Register(this);
        }

        private static void Serialize(object value, Stream s)
        {
            using (var writer = new StreamWriter(s))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                var ser = new JsonSerializer();
                ser.Serialize(jsonWriter, value);
                jsonWriter.Flush();
            }
        }

        private static object DeserializeFromStream(Stream stream)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize(jsonTextReader);
            }
        }

        public void AddInstance(string instanceId, MonoExpanderInstance expanderLocal)
        {
            this.clientInstances.Add(instanceId, expanderLocal);

            expanderLocal.SetSendAction(async msg =>
            {
                string connectionId;

                lock (this.lockObject)
                {
                    if (!this.connectionIdByInstanceId.TryGetValue(instanceId, out connectionId))
                    {
                        log.Trace("InstanceId {0} not connected yet", instanceId);

                        return;
                    }
                }

                using (var ms = new MemoryStream())
                {
                    Serialize(msg, ms);

                    await this.serverCommunication.SendToClientAsync(connectionId, ms.ToArray());
                }
            });
        }

        public void Start()
        {
            Task.Run(async () => await this.serverCommunication.StartAsync()).Wait();
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
            this.signalrServer?.Dispose();
            this.signalrServer = null;
        }

        public string ExpanderSharedFiles { get; set; }

        public void SetKnownInstanceId(string instanceId, string connectionId)
        {
            lock (this.lockObject)
            {
                this.connectionIdByInstanceId[instanceId] = connectionId;
                this.instanceIdByConnectionId[connectionId] = instanceId;
            }
        }

        private void HandleMessage(string connectionId, Type messageType, object message)
        {
            string instanceId;
            lock (this.lockObject)
            {
                // Find instance id
                if (!this.instanceIdByConnectionId.TryGetValue(connectionId, out instanceId))
                    return;
            }

            if (!string.IsNullOrEmpty(instanceId))
            {
                // Find instance
                MonoExpanderInstance instance;
                if (!this.clientInstances.TryGetValue(instanceId, out instance))
                    return;

                instance.HandleMessage(messageType, message);
            }
        }
    }
}
