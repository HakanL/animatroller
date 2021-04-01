using Haukcode.ArtNet.Packets;
using Haukcode.PcapngUtils;
using Haukcode.PcapngUtils.Pcap;
using Haukcode.sACN;
using Haukcode.sACN.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class PCapArtNetFileReader : PCapFileReader, IFileReader
    {
        private double? timestampOffsetMs = null;
        private long sequence;

        public PCapArtNetFileReader(string fileName)
            : base(fileName)
        {
        }

        public DmxDataPacket ReadFrame()
        {
            var data = ReadPacket();

            var artNetData = new Haukcode.ArtNet.IO.ArtNetReceiveData
            {
                buffer = data.Payload,
                DataLength = data.Payload.Length
            };
            var packet = ArtNetPacket.Create(artNetData, null);

            double timestampMs = data.Seconds * 1000 + (data.Microseconds / 1000.0);
            if (!this.timestampOffsetMs.HasValue)
                timestampOffsetMs = timestampMs;

            if (packet is ArtNetDmxPacket dmxPacket)
            {
                return new DmxDataPacket
                {
                    DataType = DmxDataFrame.DataTypes.FullFrame,
                    Sequence = ++this.sequence,
                    TimestampMS = timestampMs - this.timestampOffsetMs.Value,
                    UniverseId = dmxPacket.Universe + 1,
                    Data = dmxPacket.DmxData,
                    SyncAddress = 0
                };
            }
            else
                throw new ArgumentException("Unhandled packet type");
        }
    }
}
