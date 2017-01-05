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
    public class AcnStream
    {
        public readonly Guid dmxPlayerAcnId = new Guid("{D599A13F-8117-4A6E-AE1E-753B7D4DB347}");
        private bool isRunning;

        public class AcnUniverse : IDisposable
        {
            private int universe;
            private Acn.DmxStreamer streamer;
            private DmxUniverse dmxUniverse;
            private AcnStream parent;

            public AcnUniverse(Acn.DmxStreamer streamer, int universe, AcnStream parent)
            {
                this.streamer = streamer;
                this.universe = universe;
                this.parent = parent;

                this.dmxUniverse = new DmxUniverse(universe);
                bool isStreamerRunning = this.streamer.Streaming;
                if (isStreamerRunning)
                    this.streamer.Stop();
                this.streamer.AddUniverse(this.dmxUniverse);
                if (isStreamerRunning)
                    this.streamer.Start();
            }

            public void Dispose()
            {
                this.streamer.RemoveUniverse(this.universe);
            }

            public void SetDmx(byte[] data)
            {
                this.dmxUniverse.SetDmx(data);
            }
        }

        private object lockObject = new object();
        private StreamingAcnSocket socket;
        private Acn.DmxStreamer dmxStreamer;
        private Dictionary<int, AcnUniverse> sendingUniverses;
        private byte priority;

        public AcnStream(IPAddress bindIpAddress, byte priority)
        {
            if (bindIpAddress == null)
                bindIpAddress = GetFirstBindAddress();

            this.priority = priority;

            this.socket = new StreamingAcnSocket(dmxPlayerAcnId, "DmxPlayer");
            this.socket.NewPacket += socket_NewPacket;
            this.socket.Open(new IPEndPoint(bindIpAddress, 0));
            this.socket.UnhandledException += Socket_UnhandledException;
            Console.WriteLine("ACN binding to {0}", bindIpAddress);

            this.dmxStreamer = new DmxStreamer(this.socket);
            this.dmxStreamer.Priority = priority;
            this.sendingUniverses = new Dictionary<int, AcnUniverse>();
        }

        private void Socket_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Error");
        }

        public AcnStream(byte priority = 100)
            : this(null, priority)
        {
        }

        public AcnStream JoinDmxUniverse(params int[] universes)
        {
            foreach (int universe in universes)
                this.socket.JoinDmxUniverse(universe);

            return this;
        }

        private void socket_NewPacket(object sender, NewPacketEventArgs<StreamingAcnDmxPacket> e)
        {
            // Received DMX packet on ACN stream
        }

        public void SendDmx(int universe, byte[] data, byte? priority = null)
        {
            this.socket.SendDmx(
                universe: universe,
                startCode: 0,
                dmxData: data,
                priority: priority ?? this.priority);
        }

        public AcnUniverse GetSendingUniverse(int universe)
        {
            AcnUniverse acnUniverse;
            lock (this.lockObject)
            {
                if (!this.sendingUniverses.TryGetValue(universe, out acnUniverse))
                {
                    acnUniverse = new AcnUniverse(this.dmxStreamer, universe, this);

                    this.sendingUniverses.Add(universe, acnUniverse);

                    if (this.isRunning && !this.dmxStreamer.Streaming)
                        this.dmxStreamer.Start();
                }
            }

            return acnUniverse;
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

        public void Start()
        {
            if (this.sendingUniverses.Any())
                this.dmxStreamer.Start();

            this.isRunning = true;
        }

        public void Stop()
        {
            lock (this.lockObject)
            {
                foreach (var sendingUniverse in this.sendingUniverses.Values)
                    sendingUniverse.Dispose();
                this.sendingUniverses.Clear();
            }

            this.dmxStreamer.Stop();

            this.isRunning = false;
        }
    }
}
