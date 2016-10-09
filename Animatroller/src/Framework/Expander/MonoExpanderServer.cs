using System;
using System.Collections.Generic;
using NLog;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;

namespace Animatroller.Framework.Expander
{
    public class MonoExpanderServer : IDisposable, IPort, IRunnable
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
        private object lockObject = new object();
        private ExpanderCommunication.IServerCommunication serverCommunication;
        private Dictionary<string, Type> typeCache;

        public MonoExpanderServer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(
                name: name,
                listenPort: Executor.Current.GetSetKey(this, name + ".listenPort", 8081),
                communicationType: CommunicationTypes.Netty);
        }

        public MonoExpanderServer(int listenPort, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(
                name: name,
                listenPort: listenPort,
                communicationType: CommunicationTypes.Netty);
        }

        private void Initialize(string name, int listenPort, CommunicationTypes communicationType)
        {
            this.name = name;
            this.clientInstances = new Dictionary<string, MonoExpanderInstance>();
            this.typeCache = new Dictionary<string, Type>();

            switch (communicationType)
            {
                case CommunicationTypes.SignalR:
                    this.serverCommunication = new ExpanderCommunication.SignalRServer(
                        listenPort: listenPort,
                        dataReceivedAction: DataReceived);
                    break;

                case CommunicationTypes.Netty:
                    this.serverCommunication = new ExpanderCommunication.NettyServer(
                        listenPort: listenPort,
                        dataReceivedAction: DataReceived);
                    break;

                default:
                    throw new ArgumentException("Communication Type");
            }

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

        private static object DeserializeFromStream(Stream stream, Type messageType)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize(jsonTextReader, messageType);
            }
        }

        private async Task SendData(string instanceId, object data)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    Serialize(data, ms);

                    await this.serverCommunication.SendToClientAsync(instanceId, data.GetType().FullName, ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                if (ex is AggregateException)
                    ex = ex.InnerException;

                log.Warn(ex, "Failed to SendData");
            }
        }

        public void AddInstance(string instanceId, MonoExpanderInstance expanderLocal)
        {
            this.clientInstances.Add(instanceId, expanderLocal);

            expanderLocal.Initialize(
                expanderSharedFiles: ExpanderSharedFiles,
                sendAction: async msg => await SendData(instanceId, msg));
        }

        public void Start()
        {
            Task.Run(async () => await this.serverCommunication.StartAsync()).Wait();
        }

        public void Stop()
        {
            Task.Run(async () => await this.serverCommunication.StopAsync()).Wait();
        }

        public void Dispose()
        {
            Stop();
        }

        public string ExpanderSharedFiles { get; set; }

        private void DataReceived(string instanceId, string connectionId, string messageType, byte[] data)
        {
            // Find instance
            MonoExpanderInstance instance;
            if (!this.clientInstances.TryGetValue(instanceId, out instance))
                return;

            object messageObject;
            Type type;

            using (var ms = new MemoryStream(data))
            {
                lock (this.typeCache)
                {
                    if (!this.typeCache.TryGetValue(messageType, out type))
                    {
                        type = typeof(Animatroller.Framework.MonoExpanderMessages.Ping).Assembly.GetType(messageType, true);
                        this.typeCache.Add(messageType, type);
                    }
                }

                messageObject = DeserializeFromStream(ms, type);
            }

            instance.HandleMessage(connectionId, type, messageObject);
        }
    }
}
