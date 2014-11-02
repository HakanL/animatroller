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
        private object lockObject = new object();

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

            this.sender.Connect();

            Executor.Current.Register(this);
        }

        public void Start()
        {
        }

        public void Stop()
        {
            this.sender.Close();
        }

        public OscClient Send(string address, params object[] data)
        {
//            this.sender.WaitForAllMessagesToComplete();

            log.Info("Sending to {0}", address);

            if (data == null || data.Length == 0)
            {
                // Send empty message
                var oscMessage = new OscMessage(address);

                lock (lockObject)
                {
                    this.sender.Send(oscMessage);
                }
            }
            else
            {
                log.Info("   Data {0}", string.Join(" ", data));

                var oscMessage = new OscMessage(address, data);
                var oscPacket = new OscBundle(0, oscMessage);

                lock (lockObject)
                {
                    this.sender.Send(oscPacket);
                }
            }

            return this;
        }
    }
}
