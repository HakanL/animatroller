using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using NLog;
using Newtonsoft.Json;
using Owin;
using Microsoft.Owin.Hosting;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Animatroller.Framework.Expander
{
    public interface IMonoExpanderServerRepository
    {
        void SetKnownInstanceId(string instanceId, string connectionId);

        void HandleMessage(string connectionId, object message);

        string ExpanderSharedFiles { get; }

    }

    public class MonoExpanderServer : IDisposable, IPort, IRunnable, IMonoExpanderServerRepository
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private string name;
        private int listenPort;
        private int? lighthousePort;
        private Dictionary<string, MonoExpanderInstance> clientInstances;
        private Dictionary<string, string> connectionIdByInstanceId;
        private Dictionary<string, string> instanceIdByConnectionId;
        private IDisposable signalrServer;
        private object lockObject = new object();

        public MonoExpanderServer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(
                name: name,
                listenPort: Executor.Current.GetSetKey(this, name + ".listenPort", 8081),
                lighthousePort: Executor.Current.GetSetKey<int?>(this, name + ".lighthousePort", null));
        }

        public void AddInstance(string instanceId, MonoExpanderInstance expanderLocal)
        {
            this.clientInstances.Add(instanceId, expanderLocal);

            expanderLocal.SetSendAction(msg =>
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

                var context = GlobalHost.ConnectionManager.GetHubContext<MonoExpanderHub>();

                IClientProxy client = context.Clients.Client(connectionId);

                client.Invoke("HandleMessage", msg.GetType(), msg);
            });
        }

        public MonoExpanderServer(int listenPort, int? lighthousePort = null, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(name, listenPort, lighthousePort);
        }

        private void Initialize(string name, int listenPort, int? lighthousePort)
        {
            this.name = name;
            this.listenPort = listenPort;
            this.lighthousePort = lighthousePort;
            this.clientInstances = new Dictionary<string, MonoExpanderInstance>();
            this.connectionIdByInstanceId = new Dictionary<string, string>();
            this.instanceIdByConnectionId = new Dictionary<string, string>();

            GlobalHost.DependencyResolver.Register(typeof(MonoExpanderHub), () =>
                new MonoExpanderHub(this, log));

            Executor.Current.Register(this);
        }

        public void Start()
        {
            var startOptions = new StartOptions(@"http://+:8899/")
            {
                ServerFactory = "Microsoft.Owin.Host.HttpListener"
            };
            this.signalrServer = WebApp.Start<MonoExpanderStartup>(startOptions);
        }

        public string ExpanderSharedFiles { get; set; }

        public void Stop()
        {
        }

        public void Dispose()
        {
            this.signalrServer?.Dispose();
            this.signalrServer = null;
        }

        public void SetKnownInstanceId(string instanceId, string connectionId)
        {
            lock (this.lockObject)
            {
                this.connectionIdByInstanceId[instanceId] = connectionId;
                this.instanceIdByConnectionId[connectionId] = instanceId;
            }
        }

        public void HandleMessage(string connectionId, object message)
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

                instance.HandleMessage(message);
            }
        }
    }
}
