﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using Akka.Routing;
using NLog;
using Newtonsoft.Json;

namespace Animatroller.Framework.Expander
{
    public class MonoExpanderServer : IDisposable, IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private string name;
        private ActorSystem system;
        private IActorRef serverActor;
        private int listenPort;
        private Dictionary<string, IMonoExpanderInstance> clientInstances;

        public MonoExpanderServer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(
                name: name,
                listenPort: Executor.Current.GetSetKey(this, name + ".listenPort", 8081));
        }

        public void AddInstance(string instanceId, MonoExpanderInstance expanderLocal)
        {
            this.clientInstances.Add(instanceId, expanderLocal);
        }

        public MonoExpanderServer(int listenPort, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(name, listenPort);
        }

        internal void UpdateClientActors(string instanceId, IActorRef clientActor, IActorRef serverActor)
        {
            IMonoExpanderInstance expander;
            if (this.clientInstances.TryGetValue(instanceId, out expander))
            {
                expander.SetActor(clientActor, serverActor);
            }
        }

        internal IMonoExpanderInstance GetClientInstance(string instanceId)
        {
            IMonoExpanderInstance instance;
            this.clientInstances.TryGetValue(instanceId, out instance);

            return instance;
        }

        private void Initialize(string name, int listenPort)
        {
            this.name = name;
            this.listenPort = listenPort;
            this.clientInstances = new Dictionary<string, IMonoExpanderInstance>();

            // Default
            var akkaConfig = ConfigurationFactory.ParseString(@"
                akka {
                    #loglevel = DEBUG
                    #log-config-on-start = on
                    actor {
                        provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                        #debug {  
                            #receive = on
                            #autoreceive = on
                            #lifecycle = on
                            #event-stream = on
                            #unhandled = on
                        #}
                    }
                    remote {
                        helios.tcp {
                            #port = 8081
                            hostname = 0.0.0.0,
                            #public-hostname = localhost
                        }
                    }
                    cluster {
                        #seed-nodes = []
                        roles = [animatroller]
                        auto-down-unreachable-after = 120s
                    }
                }
            ");

            // Reference for fallback configuration
            // https://github.com/petabridge/lighthouse/blob/master/src/Lighthouse/LighthouseHostFactory.cs

            var finalConfig = ConfigurationFactory.ParseString(
                    string.Format(@"akka.remote.helios.tcp.public-hostname = {0} 
                        akka.remote.helios.tcp.port = {1}", "localhost", listenPort))
                .WithFallback(akkaConfig);

            // Create Actor System
            this.system = ActorSystem.Create("Animatroller", finalConfig);

            Executor.Current.Register(this);
        }

        public void Start()
        {
            // Create actor
            this.serverActor = this.system.ActorOf(Props.Create<MonoExpanderServerActor>(this), "ExpanderServer");

            var seeds = new HashSet<Address>();

            try
            {
                string seedList = Executor.Current.GetKey(this, "CurrentUpMembers", null);

                if (!string.IsNullOrEmpty(seedList))
                {
                    foreach (var upMember in Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(seedList))
                        seeds.Add(Address.Parse(upMember));
                }
            }
            catch
            {
                // Ignore
            }

            // Always add us first
            var ourAddress = Address.Parse(string.Format("akka.tcp://Animatroller@localhost:{0}", this.listenPort));
            seeds.Remove(ourAddress);

            var joinSeeds = System.Collections.Immutable.ImmutableList<Address>.Empty;
            joinSeeds = joinSeeds.Add(ourAddress);

            // Build seed list from rest of seeds
            foreach (var addr in seeds)
                joinSeeds = joinSeeds.Add(addr);

            // Join the cluster
            Cluster.Get(this.system).JoinSeedNodes(joinSeeds);
        }

        public string ExpanderSharedFiles { get; set; }

        public void Stop()
        {
            if (this.serverActor != null)
            {
                this.serverActor.GracefulStop(TimeSpan.FromSeconds(5));
                this.serverActor = null;
            }
        }

        public void Dispose()
        {
            if (this.system != null)
            {
                this.system.Dispose();
                this.system = null;
            }
        }
    }
}
