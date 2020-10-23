//#define DEBUG_OSC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Net;
using Serilog;
using Haukcode.Osc;
using System.Threading;

namespace Animatroller.Framework.Expander
{
    public class OscClient : IPort, IRunnable
    {
        protected ILogger log;
        private OscSender sender;
        private System.Net.IPAddress destination;
        private int destinationPort;
        private readonly object lockObject = new object();
        private readonly Timer repeatSender;
        private readonly Dictionary<string, OscMessage> sendList = new Dictionary<string, OscMessage>();

        public OscClient(string destination, int destinationPort)
            : this(IPAddress.Parse(destination), destinationPort)
        {
        }

        public OscClient(IPAddress destination, int destinationPort)
        {
            this.log = Log.Logger;
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

            this.repeatSender = new Timer(RepeatSenderCallback, null, 5_000, 5_000);

            Executor.Current.Register(this);
        }

        private void RepeatSenderCallback(object state)
        {
            try
            {
                lock (this.lockObject)
                {
                    foreach (var kvp in this.sendList)
                    {
#if DEBUG_OSC
                        this.log.Verbose("Sending repeat to {0}", kvp.Key);
#endif

                        this.sender.Send(kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.Error(ex, "Error in RepeatSenderCallback: {Message}", ex.Message);
            }
        }

        public void Start()
        {
        }

        public void Stop()
        {
            this.repeatSender.Dispose();
            this.sender.Close();
        }

        public OscClient SendAndRepeat(string address, params object[] data)
        {
            return Send(address, true, true, data);
        }

        public OscClient Send(string address, params object[] data)
        {
            return Send(address, true, false, data);
        }

        public OscClient Send(string address, bool convertDoubleToFloat, bool repeat, params object[] data)
        {
            //            this.sender.WaitForAllMessagesToComplete();

#if DEBUG_OSC
            this.log.Verbose("Sending to {0}", address);
#endif
            if (data == null || data.Length == 0)
            {
                // Send empty message
                var oscMessage = new OscMessage(address);

                lock (this.lockObject)
                {
                    this.sender.Send(oscMessage);

                    if (repeat)
                        this.sendList[address] = oscMessage;
                    else
                        this.sendList.Remove(address);
                }
            }
            else
            {
#if DEBUG_OSC
                this.log.Verbose("   Data {0}", string.Join(" ", data));
#endif

                var sendData = new object[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    if (convertDoubleToFloat && data[i] is double)
                        sendData[i] = (float)((double)data[i]);
                    else
                        sendData[i] = data[i];
                }

                var oscMessage = new OscMessage(address, sendData);
                var oscPacket = new OscBundle(0, oscMessage);

                lock (this.lockObject)
                {
                    this.sender.Send(oscPacket);

                    if (repeat)
                        this.sendList[address] = oscMessage;
                    else
                        this.sendList.Remove(address);
                }
            }

            return this;
        }
    }
}
