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
        public readonly Guid dmxPlayerAcnId = new Guid("{D599A13F-8117-4A6E-AE1E-753B7D4DB347}");
        private readonly SACNClient acnClient;
        private readonly byte priority;
        private HashSet<int> usedUniverses = new HashSet<int>();

        public AcnStream(IPAddress bindIpAddress, byte priority)
        {
            if (bindIpAddress == null)
                bindIpAddress = SACNCommon.GetFirstBindAddress();
            if (bindIpAddress == null)
                throw new ArgumentException("No suitable NIC found");

            this.priority = priority;

            this.acnClient = new SACNClient(
                senderId: this.dmxPlayerAcnId,
                senderName: "DmxPlayer",
                localAddress: bindIpAddress);

            Console.WriteLine("ACN binding to {0}", bindIpAddress);
        }

        public AcnStream(byte priority = 100)
            : this(null, priority)
        {
        }

        public void SendDmx(int universe, byte[] data, byte? priority = null)
        {
            this.acnClient.SendMulticast(
                universeId: (ushort)universe,
                startCode: 0,
                data: data,
                priority: priority ?? this.priority);

            this.usedUniverses.Add(universe);
        }

        public void Dispose()
        {
            this.acnClient.Dispose();
        }

        public IList<int> UsedUniverses => this.usedUniverses.ToList();
    }
}
