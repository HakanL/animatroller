using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Akka.Actor;
using Animatroller.Framework.MonoExpanderMessages;
using Akka.Cluster;

namespace Animatroller.MonoExpander
{
    public interface IMonoExpanderClientActor :
        IHandle<SetOutputRequest>,
        IHandle<AudioEffectCue>,
        IHandle<AudioEffectPlay>,
        IHandle<AudioEffectPause>,
        IHandle<AudioEffectResume>,
        IHandle<AudioEffectSetVolume>,
        IHandle<AudioBackgroundSetVolume>,
        IHandle<AudioBackgroundResume>,
        IHandle<AudioBackgroundPause>,
        IHandle<AudioBackgroundNext>,
        IHandle<AudioTrackPlay>,
        IHandle<AudioTrackCue>,
        IHandle<AudioTrackResume>,
        IHandle<AudioTrackPause>,
        IHandle<VideoPlay>
    {
    }

    public class MonoExpanderClientActor : TypedActor,
        ILogReceive,
        IHandle<ClusterEvent.IMemberEvent>,
        IHandle<WhoAreYouRequest>,
        IHandle<ActorIdentity>,
        IMonoExpanderClientActor
    {
        protected Logger log = LogManager.GetCurrentClassLogger();
        private Main main;

        public MonoExpanderClientActor(Main main)
        {
            this.main = main;

            // Subscribe to cluster events
            Cluster.Get(Context.System).Subscribe(Self, new[] { typeof(ClusterEvent.IMemberEvent) });
        }

        public void Handle(SetOutputRequest message)
        {
            this.main.Handle(message);
        }

        public void Handle(ClusterEvent.IMemberEvent message)
        {
            if (message.Member.Status == MemberStatus.Up && message.Member.Roles.Contains("animatroller"))
            {
                // Found a master, ping it
                var sel = GetServerActorSelection(message.Member.Address);
                sel.Tell(new Identify("animatroller"), Self);
            }
            else
                // Not valid
                this.main.RemoveServer(message.Member.Address);
        }

        private ActorSelection GetServerActorSelection(Address address)
        {
            return Context.ActorSelection(string.Format("{0}/user/ExpanderServer", address));
        }

        public void Handle(WhoAreYouRequest message)
        {
            Sender.Tell(new WhoAreYouResponse
            {
                InstanceId = this.main.InstanceId
            }, Self);
        }

        public void Handle(ActorIdentity message)
        {
            if (message.MessageId is string && (string)message.MessageId == "animatroller")
            {
                // We expected this
                this.main.AddServer(Sender.Path.Address, Sender);
            }
        }

        public void Handle(AudioEffectCue message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioEffectPlay message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioEffectPause message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioEffectResume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioEffectSetVolume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioBackgroundSetVolume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioBackgroundResume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioBackgroundPause message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioBackgroundNext message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioTrackPlay message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioTrackCue message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioTrackResume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioTrackPause message)
        {
            this.main.Handle(message);
        }

        public void Handle(VideoPlay message)
        {
            this.main.Handle(message);
        }
    }
}
