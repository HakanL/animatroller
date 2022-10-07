using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Haukcode.sACN;

namespace Animatroller.DMXplayer
{
    public class AcnStream : IOutput
    {
        private readonly SACNClient acnClient;
        private readonly byte priority;
        private readonly HashSet<int> usedUniverses = new();

        public AcnStream(IPAddress bindIpAddress, byte priority)
        {
            if (bindIpAddress == null)
                bindIpAddress = SACNCommon.GetFirstBindAddress().IPAddress;
            if (bindIpAddress == null)
                throw new ArgumentException("No suitable NIC found");

            this.priority = priority;

            this.acnClient = new SACNClient(
                senderId: Const.AcnSourceId,
                senderName: Const.AcnSourceName,
                localAddress: bindIpAddress);

            Console.WriteLine("ACN binding to {0}", bindIpAddress);
        }

        public AcnStream(byte priority = 100)
            : this(null, priority)
        {
        }

        public void SendDmx(int universe, byte[] data, byte? priority = null, int syncAddress = 0)
        {
            this.acnClient.SendMulticast(
                universeId: (ushort)universe,
                startCode: 0,
                data: data,
                priority: priority ?? this.priority,
                syncAddress: (ushort)syncAddress);

            this.usedUniverses.Add(universe);
        }

        public void Dispose()
        {
            this.acnClient.Dispose();
        }

        public void SendSync(int syncAddress)
        {
            this.acnClient.SendMulticastSync((ushort)syncAddress);
        }

        public IList<int> UsedUniverses => this.usedUniverses.ToList();
    }
}
