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
    public class AcnStream : IPort, IRunnable
    {
        protected class AcnUniverse : IDmxOutput, IDisposable
        {
            private int universe;
            private Acn.DmxStreamer streamer;
            private DmxUniverse dmxUniverse;

            public AcnUniverse(Acn.DmxStreamer streamer, int universe)
            {
                this.streamer = streamer;
                this.universe = universe;

                this.dmxUniverse = new DmxUniverse(universe);
                this.streamer.AddUniverse(this.dmxUniverse);
            }

            public void Dispose()
            {
                this.streamer.RemoveUniverse(this.universe);
            }

            public SendStatus SendDimmerValue(int channel, byte value)
            {
                this.dmxUniverse.SetDmx(channel, value);

                return SendStatus.NotSet;
            }

            public SendStatus SendDimmerValues(int firstChannel, params byte[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    int chn = firstChannel + i;
                    if (chn >= 1 && chn <= 512)
                        this.dmxUniverse.DmxData[chn - 1] = values[i];
                }

                return SendStatus.NotSet;
            }
        }

        private object lockObject = new object();
        private StreamingAcnSocket socket;
        private Acn.DmxStreamer dmxStreamer;
        private Dictionary<int, AcnUniverse> sendingUniverses;

        public AcnStream(IPAddress bindIpAddress)
        {
            if (bindIpAddress == null)
                bindIpAddress = GetFirstBindAddress();

            this.socket = new StreamingAcnSocket(Guid.NewGuid(), "Animatroller");
            this.socket.NewPacket += socket_NewPacket;
            this.socket.Open(bindIpAddress);

            this.dmxStreamer = new DmxStreamer(socket);
            this.sendingUniverses = new Dictionary<int, AcnUniverse>();

            Executor.Current.Register(this);
        }

        public AcnStream()
            : this(null)
        {
        }

        public AcnStream JoinDmxUniverse(params int[] universes)
        {
            foreach (int universe in universes)
                this.socket.JoinDmxUniverse(universe);

            return this;
        }

        private void socket_NewPacket(object sender, NewPacketEventArgs<DmxPacket> e)
        {
            Console.WriteLine("Received DMX packet on ACN stream");
        }

        protected AcnUniverse GetSendingUniverse(int universe)
        {
            AcnUniverse acnUniverse;
            lock (this.lockObject)
            {
                if (!this.sendingUniverses.TryGetValue(universe, out acnUniverse))
                {
                    acnUniverse = new AcnUniverse(this.dmxStreamer, universe);

                    this.sendingUniverses.Add(universe, acnUniverse);
                }
            }

            return acnUniverse;
        }

        private IPAddress GetFirstBindAddress()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.SupportsMulticast && adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();

                    foreach (var ipAddress in ipProperties.UnicastAddresses)
                    {
                        if (ipAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            return ipAddress.Address;
                    }
                }
            }

            throw new ArgumentException("No suitable NIC found");
        }

        public AcnStream Connect(PhysicalDevice.INeedsDmxOutput device, int universe)
        {
            device.DmxOutputPort = GetSendingUniverse(universe);

            return this;
        }

        public void Start()
        {
            this.dmxStreamer.Start();
        }

        public void Stop()
        {
            lock(this.lockObject)
            {
                foreach (var sendingUniverse in this.sendingUniverses.Values)
                    sendingUniverse.Dispose();
                this.sendingUniverses.Clear();
            }

            this.dmxStreamer.Stop();
        }
    }

}
