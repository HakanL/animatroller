using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using NLog;

namespace Animatroller.Framework.Expander
{
    public class MonoExpanderServer : IDisposable, IPort, IRunnable
    {
        private ActorSystem system;
        private IActorRef serverActor;

        public MonoExpanderServer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(
                listenPort: Executor.Current.GetSetKey(this, name + ".listenPort", 8081));
        }

        public MonoExpanderServer(int listenPort)
        {
            Initialize(listenPort);
        }

        private void Initialize(int listenPort)
        {
            // Default
            var config = ConfigurationFactory.ParseString(@"
                akka {
                    loglevel = DEBUG
                    log-config-on-start = on
                    actor {
                        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                        debug {  
                            receive = on
                            autoreceive = on
                            lifecycle = on
                            event-stream = on
                            unhandled = on
                        }
                    }
                    remote {
                        helios.tcp {
                            transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                            applied-adapters = []
                            transport-protocol = tcp
                            port = 8081
                            hostname = localhost
                        }
                    }
                }
            ");

            // Update with the listen port
            config.GetValue("akka.remote.helios.tcp.port").NewValue(new HoconLiteral
            {
                Value = listenPort.ToString()
            });

            // Create Actor System
            this.system = ActorSystem.Create("Animatroller", config);

            Executor.Current.Register(this);
        }

        public void Start()
        {
            // Create actor
            this.serverActor = this.system.ActorOf<MonoExpanderServerActor>("ExpanderServer");
        }

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
