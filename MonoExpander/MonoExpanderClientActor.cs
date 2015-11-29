using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Animatroller.Framework.MonoExpanderMessages;

namespace Animatroller.MonoExpander
{
    public class MonoExpanderClientActor : TypedActor,
            IHandle<ConnectRequest>,
            IHandle<ConnectResponse>,
            IHandle<NickRequest>,
            IHandle<NickResponse>,
            IHandle<SayRequest>,
            IHandle<SayResponse>, ILogReceive
    {
        private string _nick = "Roggan";
        private readonly ActorSelection _server = Context.ActorSelection("akka.tcp://Animatroller@localhost:8088/user/ExpanderServer");

        public MonoExpanderClientActor()
        {

        }

        public void Handle(ConnectResponse message)
        {
            Console.WriteLine("Connected!");
            Console.WriteLine(message.Message);
        }

        public void Handle(NickRequest message)
        {
            message.OldUsername = this._nick;
            Console.WriteLine("Changing nick to {0}", message.NewUsername);
            this._nick = message.NewUsername;
            _server.Tell(message);
        }

        public void Handle(NickResponse message)
        {
            Console.WriteLine("{0} is now known as {1}", message.OldUsername, message.NewUsername);
        }

        public void Handle(SayResponse message)
        {
            Console.WriteLine("{0}: {1}", message.Username, message.Text);
        }

        public void Handle(ConnectRequest message)
        {
            Console.WriteLine("Connecting....");
            _server.Tell(message);
        }

        public void Handle(SayRequest message)
        {
            message.Username = this._nick;
            _server.Tell(message);
        }
    }
}
