using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using Animatroller.Framework.PhysicalDevice;
using Acn.Sockets;
using System.Net;
using Acn.Packets.sAcn;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;
using Acn;
using Acn.Helpers;

namespace Animatroller.Framework.Expander
{
    public class AcnOutput : IPort, IDmxOutput
    {
        private StreamingAcnSocket socket;
        private Acn.DmxStreamer dmxStreamer;

        public AcnOutput()
        {
            this.socket = new StreamingAcnSocket(Guid.NewGuid(), "Streaming ACN Snoop");
//            this.socket.NewPacket += new EventHandler<NewPacketEventArgs<Acn.Packets.sAcn.DmxPacket>>(socket_NewPacket);
            var networkCard = GetFirstNetworkCard();
            this.socket.Open(networkCard.IpAddress);

            //foreach (int universe in universes)
            //    socket.JoinDmxUniverse(universe);
            this.dmxStreamer = new DmxStreamer(socket);
        }

        private CardInfo GetFirstNetworkCard()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.SupportsMulticast && adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();

                    for (int n = 0; n < ipProperties.UnicastAddresses.Count; n++)
                    {
                        if (ipProperties.UnicastAddresses[n].Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            CardInfo card = new CardInfo(adapter, n);

                            return card;
                        }
                    }
                }
            }

            throw new ArgumentException("No NIC found");
        }

        public SendStatus SendDimmerValue(int channel, byte value)
        {
            throw new NotImplementedException();
        }

        public SendStatus SendDimmerValues(int firstChannel, params byte[] values)
        {
            throw new NotImplementedException();
        }
    }


    public class CardInfo
    {
        public CardInfo(NetworkInterface info, int addressIndex)
        {
            Interface = info;
            AddressIndex = addressIndex;
        }

        public NetworkInterface Interface { get; set; }

        public int AddressIndex { get; set; }

        public IPAddress IpAddress
        {
            get
            {
                return Interface.GetIPProperties().UnicastAddresses[AddressIndex].Address;
            }
        }

        public override string ToString()
        {
            return string.Format("{1}:  {0}", Interface.Description, IpAddress.ToString());
        }
    }
}
