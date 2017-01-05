using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using Acn.Sockets;
using System.Net;
using Acn.Packets.sAcn;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;
using Acn;
using Acn.Helpers;

namespace Animatroller.DMXplayer
{
    public class AcnStream : IOutput
    {
        public readonly Guid dmxPlayerAcnId = new Guid("{D599A13F-8117-4A6E-AE1E-753B7D4DB347}");
        private StreamingAcnSocket socket;
        private byte priority;

        public AcnStream(IPAddress bindIpAddress, byte priority)
        {
            if (bindIpAddress == null)
                bindIpAddress = GetFirstBindAddress();

            this.priority = priority;

            this.socket = new StreamingAcnSocket(dmxPlayerAcnId, "DmxPlayer");
            this.socket.Open(new IPEndPoint(bindIpAddress, 0));
            this.socket.UnhandledException += Socket_UnhandledException;
            Console.WriteLine("ACN binding to {0}", bindIpAddress);
        }

        private void Socket_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Error");
        }

        public AcnStream(byte priority = 100)
            : this(null, priority)
        {
        }

        public void SendDmx(int universe, byte[] data, byte? priority = null)
        {
            this.socket.SendDmx(
                universe: universe,
                startCode: 0,
                dmxData: data,
                priority: priority ?? this.priority);
        }

        private IPAddress GetAddressFromInterfaceType(NetworkInterfaceType interfaceType)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.SupportsMulticast && adapter.NetworkInterfaceType == interfaceType &&
                    adapter.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();

                    foreach (var ipAddress in ipProperties.UnicastAddresses)
                    {
                        if (ipAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            return ipAddress.Address;
                    }
                }
            }

            return null;
        }

        private IPAddress GetFirstBindAddress()
        {
            // Try Ethernet first
            IPAddress ipAddress = GetAddressFromInterfaceType(NetworkInterfaceType.Ethernet);
            if (ipAddress != null)
                return ipAddress;

            ipAddress = GetAddressFromInterfaceType(NetworkInterfaceType.Wireless80211);
            if (ipAddress != null)
                return ipAddress;

            throw new ArgumentException("No suitable NIC found");
        }

        public void Dispose()
        {
            this.socket.Close();
            this.socket.Dispose();
            this.socket = null;
        }
    }
}
