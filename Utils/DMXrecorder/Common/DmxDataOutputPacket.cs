using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class DmxDataOutputPacket : DmxDataPacket
    {
        public long Sequence { get; set; }

        public DmxDataOutputPacket()
        {
        }

        public static DmxDataOutputPacket CreateFullFrame(double millisecond, long sequence, int universe, byte[] data, int syncAddress)
        {
            return new DmxDataOutputPacket
            {
                Content = DmxDataFrame.CreateFrame(universe, syncAddress, data),
                Sequence = sequence,
                TimestampMS = millisecond
            };
        }

        public static DmxDataOutputPacket CreateSync(double millisecond, long sequence, int syncAddress)
        {
            return new DmxDataOutputPacket
            {
                Content = SyncFrame.CreateFrame(syncAddress),
                Sequence = sequence,
                TimestampMS = millisecond
            };
        }
    }
}
