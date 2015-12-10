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
using System.IO;

namespace Animatroller.Framework.Expander
{
    public interface IMonoExpanderServerActor :
            IHandle<InputChanged>,
            IHandle<VideoPositionChanged>,
            IHandle<VideoStarted>,
            IHandle<VideoFinished>,
            IHandle<AudioStarted>,
            IHandle<AudioFinished>,
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
        IHandle<FileRequest>,
        IHandle<FileChunkRequest>,
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

            // Check UpMembers if they are expanders and known
            foreach (var member in currentUpMembers.Values)
            {
                if (member.Roles.Contains("expander"))
                {
                    // It's an expander, we should ping to get an actor ref
                    lock (this.knownClients)
                    {
                        // Tell it to send us its InstanceId
                        var sel = GetClientActorSelection(message.Member.Address);
                        sel.Tell(new WhoAreYouRequest(), Self);
                    }
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

        public void Handle(VideoPositionChanged message)
        {
            GetClientInstance()?.Handle(message);
        }

        public void Handle(VideoStarted message)
        {
            GetClientInstance()?.Handle(message);
        }

        public void Handle(VideoFinished message)
        {
            GetClientInstance()?.Handle(message);
        }

        public void Handle(AudioStarted message)
        {
            GetClientInstance()?.Handle(message);
        }

        public void Handle(AudioFinished message)
        {
            GetClientInstance()?.Handle(message);
        }

        private byte[] CalculateSignatureSha1(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open))
            using (var bs = new BufferedStream(fs))
            {
                using (var sha1 = new System.Security.Cryptography.SHA1Managed())
                {
                    return sha1.ComputeHash(bs);
                }
            }
        }

        public void Handle(FileRequest message)
        {
            this.log.Info("Requested download file {1} of type {0}", message.Type, message.FileName);

            if (!string.IsNullOrEmpty(Path.GetDirectoryName(message.FileName)))
                throw new ArgumentException("FileName should be without path");

            string fileTypeFolder = Path.Combine(this.parent.ExpanderSharedFiles, message.Type.ToString());
            Directory.CreateDirectory(fileTypeFolder);

            string filePath = Path.Combine(fileTypeFolder, message.FileName);

            if (!File.Exists(filePath))
            {
                this.log.Warn("File {0} of type {1} doesn't exist", message.FileName, message.Type);

                Sender.Tell(new FileResponse
                {
                    DownloadId = message.DownloadId,
                    Size = 0
                }, Self);

                return;
            }

            var fi = new FileInfo(filePath);

            Sender.Tell(new FileResponse
            {
                DownloadId = message.DownloadId,
                Size = fi.Length,
                SignatureSha1 = CalculateSignatureSha1(filePath)
            }, Self);
        }

        public void Handle(FileChunkRequest message)
        {
            string filePath = Path.Combine(this.parent.ExpanderSharedFiles, message.Type.ToString(), message.FileName);

            using (var fs = File.OpenRead(filePath))
            {
                fs.Seek(message.ChunkStart, SeekOrigin.Begin);

                int bytesToRead = Math.Min(message.ChunkSize, (int)(fs.Length - message.ChunkStart));

                byte[] chunk = new byte[bytesToRead];
                fs.Read(chunk, 0, chunk.Length);

                Sender.Tell(new FileChunkResponse
                {
                    DownloadId = message.DownloadId,
                    ChunkStart = message.ChunkStart,
                    Chunk = chunk
                }, Self);
            }
        }
    }
}
