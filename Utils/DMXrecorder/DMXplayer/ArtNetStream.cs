using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Haukcode.ArtNet.Packets;
using Haukcode.ArtNet.Sockets;

namespace Animatroller.DMXplayer
{
    public class ArtNetStream : IOutput
    {
        private readonly ArtNetSocket artNetClient;
        private readonly Dictionary<int, byte> usedUniverses = new Dictionary<int, byte>();

        public ArtNetStream(IPAddress bindIpAddress)
        {
            if (bindIpAddress == null)
                bindIpAddress = Haukcode.sACN.SACNCommon.GetFirstBindAddress();
            if (bindIpAddress == null)
                throw new ArgumentException("No suitable NIC found");

            this.artNetClient = new ArtNetSocket();
            this.artNetClient.EnableBroadcast = true;
            this.artNetClient.Open(bindIpAddress, IPAddress.Broadcast);

            Console.WriteLine("ArtNet binding to {0}", bindIpAddress);
        }

        public void SendDmx(int universe, byte[] data, byte? priority = null, int syncUniverse = 0)
        {
            this.usedUniverses.TryGetValue(universe, out byte seq);
            seq++;

            var packet = new ArtNetDmxPacket
            {
                DmxData = data,
                Universe = (short)(universe - 1),
                Sequence = seq
            };

            this.artNetClient.Send(packet);

            this.usedUniverses[universe] = seq;
        }

        public void Dispose()
        {
            this.artNetClient.Dispose();
        }

        public void SendSync(int syncUniverse)
        {
            // Send ArtNetSync
            throw new NotImplementedException();
        }

        public IList<int> UsedUniverses => this.usedUniverses.Keys.ToList();
    }
}
