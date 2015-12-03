using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Akka.Actor;
using Akka.Cluster;
using Newtonsoft.Json;
using Animatroller.Framework.MonoExpanderMessages;

namespace Animatroller.Framework.Expander
{
    public interface IMonoExpanderServerActor :
            IHandle<InputChanged>,
            IHandle<AudioPositionChanged>
    {
    }

    public interface IMonoExpanderInstance : IMonoExpanderServerActor
    {
        void SetActor(IActorRef clientActorRef, IActorRef serverActorRef);
    }

    public class MonoExpanderServerActor : TypedActor,
        IMonoExpanderServerActor,
        IHandle<ClusterEvent.IMemberEvent>,
        ILogReceive
    {
        protected Logger log = LogManager.GetCurrentClassLogger();
        private MonoExpanderServer parent;
        private Dictionary<Address, string> knownClients;

        public MonoExpanderServerActor(MonoExpanderServer parent)
        {
            this.parent = parent;

            try
            {
                string knownClientsValue = Executor.Current.GetKey(this.parent, "KnownClients", null);

                if (!string.IsNullOrEmpty(knownClientsValue))
                {
                    var knownList = JsonConvert.DeserializeObject<Dictionary<string, string>>(knownClientsValue);
                    this.knownClients = knownList.ToDictionary(x => Address.Parse(x.Key), x => x.Value);

                    foreach (var kvp in knownList)
                        this.log.Debug("Known client - InstanceId {0} at {1}", kvp.Value, kvp.Key);
                }
                else
                    this.knownClients = new Dictionary<Address, string>();
            }
            catch
            {
                this.knownClients = new Dictionary<Address, string>();
            }

            // Subscribe to cluster events
            Cluster.Get(Context.System).Subscribe(Self, new[] { typeof(ClusterEvent.IMemberEvent) });
        }

        private IMonoExpanderServerActor GetClientInstance()
        {
            string instanceId;
            if (!this.knownClients.TryGetValue(Sender.Path.Address, out instanceId))
                return null;

            return this.parent.GetClientInstance(instanceId);
        }

        public void Handle(WhoAreYouResponse message)
        {
            this.log.Debug("Response from instance {0} at {1}", message.InstanceId, Sender.Path);

            lock (this.knownClients)
            {
                string currentInstanceId;
                if (!this.knownClients.TryGetValue(Sender.Path.Address, out currentInstanceId) || currentInstanceId != message.InstanceId)
                {
                    // Update
                    this.knownClients[Sender.Path.Address] = message.InstanceId;

                    SaveKnownClients();
                }
            }

            this.parent.UpdateClientActors(message.InstanceId, Sender, Self);
        }

        public void Handle(ClusterEvent.IMemberEvent message)
        {
            var currentUpMembers = Cluster.Get(Context.System).ReadView.Members
                .Where(x => x.Status == MemberStatus.Up)
                .ToDictionary(x => x.Address);

            string seedList = Newtonsoft.Json.JsonConvert.SerializeObject(currentUpMembers.Select(x => x.Value.Address.ToString()));
            Executor.Current.SetKey(this.parent, "CurrentUpMembers", seedList);

            // See if the connected node is known
            if (message.Member.Status == MemberStatus.Up && message.Member.Roles.Contains("expander"))
            {
                // It's an expander, we should ping to get an actor ref
                lock (this.knownClients)
                {
                    // Tell it to send us its InstanceId
                    var sel = GetClientActorSelection(message.Member.Address);
                    sel.Tell(new WhoAreYouRequest(), Self);
                }
            }

            // Clean up known clients list, remove any client who isn't currently up
            lock (this.knownClients)
            {
                bool dirty = false;
                foreach (var clientAddr in this.knownClients.Keys.ToList())
                {
                    if (!currentUpMembers.ContainsKey(clientAddr))
                    {
                        this.knownClients.Remove(clientAddr);
                        dirty = true;
                    }
                }

                if (dirty)
                    SaveKnownClients();
            }
        }

        private void SaveKnownClients()
        {
            lock (this.knownClients)
            {
                string knownClientsValue = JsonConvert.SerializeObject(this.knownClients);
                Executor.Current.SetKey(this.parent, "KnownClients", knownClientsValue);
            }
        }

        private ActorSelection GetClientActorSelection(Address address)
        {
            return Context.ActorSelection(string.Format("{0}/user/Expander", address));
        }

        public void Handle(AudioPositionChanged message)
        {
            GetClientInstance()?.Handle(message);
        }

        public void Handle(InputChanged message)
        {
            GetClientInstance()?.Handle(message);
        }
    }
}
