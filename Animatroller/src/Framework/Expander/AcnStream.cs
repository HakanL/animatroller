﻿using System;
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
using NLog;

namespace Animatroller.Framework.Expander
{
    public class AcnStream : IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        protected class AcnPixelUniverse : IPixelOutput
        {
            private object lockObject = new object();
            private AcnStream acnStream;
            private Dictionary<int, AcnUniverse> acnUniverses;
            private int startUniverse;
            private int startDmxChannel;

            public AcnPixelUniverse(AcnStream acnStream, int startUniverse, int startDmxChannel)
            {
                this.acnStream = acnStream;
                this.startUniverse = startUniverse;
                this.startDmxChannel = startDmxChannel;

                this.acnUniverses = new Dictionary<int, AcnUniverse>();
            }

            protected AcnUniverse GetAcnUniverse(int universe)
            {
                AcnUniverse acnUniverse;
                lock (lockObject)
                {
                    if (!this.acnUniverses.TryGetValue(universe, out acnUniverse))
                    {
                        acnUniverse = this.acnStream.GetSendingUniverse(universe);
                        this.acnUniverses.Add(universe, acnUniverse);
                    }
                }

                return acnUniverse;
            }

            public SendStatus SendPixelValue(int channel, PixelRGBByte rgb)
            {
                var values = new byte[3];
                values[0] = rgb.R;
                values[1] = rgb.G;
                values[2] = rgb.B;

                // Max 510 RGB values per universe
                int universe = (this.startDmxChannel + (channel * 3)) / 510;
                int localStart = (this.startDmxChannel + (channel *3)) % 510;

                var acnUniverse = GetAcnUniverse(this.startUniverse + universe);

                return acnUniverse.SendDimmerValues(localStart, values, 0, 3);
            }

            public SendStatus SendPixelsValue(int channel, PixelRGBByte[] rgb)
            {
                // Max 510 RGB values per universe
                int universe = (this.startDmxChannel + (channel * 3)) / 510;
                int localStart = (this.startDmxChannel + (channel * 3)) % 510;

                var acnUniverse = GetAcnUniverse(this.startUniverse + universe);

                int chn = 0;
                var values = new byte[3 * rgb.Length];
                foreach (var rgbValue in rgb)
                {
                    values[chn++] = rgbValue.R;
                    values[chn++] = rgbValue.G;
                    values[chn++] = rgbValue.B;

                    if (chn + localStart > 510)
                    {
                        acnUniverse.SendDimmerValues(localStart, values, 0, chn);

                        // Get next universe
                        chn = 0;
                        universe++;
                        localStart = 1;
                        acnUniverse = GetAcnUniverse(this.startUniverse + universe);
                    }
                }

                if (chn > 0)
                    acnUniverse.SendDimmerValues(localStart, values, 0, chn);

                return SendStatus.NotSet;
            }
        }

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

            public SendStatus SendDimmerValues(int firstChannel, byte[] values)
            {
                return SendDimmerValues(firstChannel, values, 0, values.Length);
            }

            public SendStatus SendDimmerValues(int firstChannel, byte[] values, int offset, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    int chn = firstChannel + i;
                    if (chn >= 1 && chn <= 512)
                        this.dmxUniverse.DmxData[chn] = values[offset + i];
                }

                // Force a send
                this.dmxUniverse.SetDmx(0, this.dmxUniverse.DmxData[0]);
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
            log.Debug("Received DMX packet on ACN stream");
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

        protected AcnPixelUniverse GetPixelSendingUniverse(int startUniverse, int startDmxChannel)
        {
            return new AcnPixelUniverse(this, startUniverse, startDmxChannel);
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

        public AcnStream Connect(PhysicalDevice.INeedsPixelOutput device, int startUniverse, int startDmxChannel)
        {
            device.PixelOutputPort = GetPixelSendingUniverse(startUniverse, startDmxChannel);

            return this;
        }

        public void Start()
        {
            this.dmxStreamer.Start();
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
        }
    }

}
