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

        public static DmxDataOutputPacket CreateFullFrame(double millisecond, long sequence, int universe, System.Net.IPAddress destination, byte[] data, int syncAddress)
        {
            return new DmxDataOutputPacket
            {
                Content = DmxDataFrame.CreateFrame(universe, syncAddress, data, destination),
                Sequence = sequence,
                TimestampMS = millisecond
            };
        }

        public static DmxDataOutputPacket CreateSync(double millisecond, long sequence, int syncAddress, System.Net.IPAddress destination)
        {
            return new DmxDataOutputPacket
            {
                Content = SyncFrame.CreateFrame(syncAddress, destination),
                Sequence = sequence,
                TimestampMS = millisecond
            };
        }
    }
}
