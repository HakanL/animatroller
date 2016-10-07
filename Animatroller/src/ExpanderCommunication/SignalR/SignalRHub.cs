using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json.Linq;
using NLog;

namespace Animatroller.ExpanderCommunication
{
    [HubName("ExpanderCommunicationHub")]
    public class SignalRHub : Hub
    {
        private SignalRServer parent;
        private Logger log;

        public SignalRHub(SignalRServer parent, Logger log)
        {
            this.parent = parent;
            this.log = log;
        }

        public override Task OnReconnected()
        {
            this.parent.UpdateInstance(Context.QueryString["InstanceId"], Context.ConnectionId);

            return base.OnReconnected();
        }

        public override Task OnConnected()
        {
            this.parent.UpdateInstance(Context.QueryString["InstanceId"], Context.ConnectionId);

            return base.OnConnected();
        }

        public void HandleMessage(string messageType, byte[] data)
        {
            this.parent.DataReceived(Context.ConnectionId, messageType, data);
        }
    }
}
