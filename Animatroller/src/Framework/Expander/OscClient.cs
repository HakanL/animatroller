using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Net;
using NLog;
using Rug.Osc;

namespace Animatroller.Framework.Expander
{
    public class OscClient : IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private OscSender sender;
        private System.Net.IPAddress destination;
        private int destinationPort;

        public OscClient(IPAddress destination, int destinationPort)
        {
            this.destination = destination;
            this.destinationPort = destinationPort;

            this.sender = new OscSender(
                IPAddress.Any,
                0,
                destination,
                destinationPort,
                OscSocket.DefaultMulticastTimeToLive,
                OscSender.DefaultMessageBufferSize,
                OscSocket.DefaultPacketSize);

            Executor.Current.Register(this);
        }

        public void Start()
        {
            this.sender.Connect();
        }

        public void Stop()
        {
            this.sender.Close();
        }

        public OscClient Send(string address, params object[] data)
        {
            var oscMessage = new OscMessage(address, data);
            var oscPacket = new OscBundle(DateTime.Now, oscMessage);

            this.sender.Send(oscPacket);

            return this;
        }
    }
}
