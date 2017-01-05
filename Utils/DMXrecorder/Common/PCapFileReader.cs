using PcapngUtils.Pcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class PCapFileReader : BaseFileReader
    {
        private PcapReader reader;
        private PcapngUtils.Common.IPacket lastPacket;

        public PCapFileReader(string fileName)
            : base(fileName)
        {
            this.reader = new PcapReader(this.fileStream);
            this.reader.OnReadPacketEvent += Reader_OnReadPacketEvent;
        }

        private void Reader_OnReadPacketEvent(object context, PcapngUtils.Common.IPacket packet)
        {
            this.lastPacket = packet;
        }

        public override void Dispose()
        {
            this.reader.Dispose();

            base.Dispose();
        }

        public override DmxData ReadFrame()
        {
            this.reader.ReadPackets(CancellationToken.None);

            return null;
        }
    }
}
