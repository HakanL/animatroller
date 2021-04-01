using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class DmxDataPacket : DmxDataFrame
    {
        public long Sequence { get; set; }

        public DmxDataPacket()
        {
        }

        public DmxDataPacket(BaseDmxData dmxData, double timestampMS, long sequence)
            : base(dmxData, timestampMS)
        {
            Sequence = sequence;
        }

        public static DmxDataPacket CreateNoChange(double millisecond, long sequence, int universe)
        {
            return new DmxDataPacket
            {
                DataType = DataTypes.NoChange,
                Data = null,
                Sequence = sequence,
                TimestampMS = millisecond,
                UniverseId = universe
            };
        }

        public static DmxDataPacket CreateFullFrame(double millisecond, long sequence, int universe, byte[] data, int syncAddress)
        {
            return new DmxDataPacket
            {
                DataType = DataTypes.FullFrame,
                Data = data,
                Sequence = sequence,
                TimestampMS = millisecond,
                UniverseId = universe,
                SyncAddress = syncAddress
            };
        }

        public static DmxDataPacket CreateSync(double millisecond, long sequence, int syncAddress)
        {
            return new DmxDataPacket
            {
                DataType = DataTypes.Sync,
                Sequence = sequence,
                TimestampMS = millisecond,
                SyncAddress = syncAddress
            };
        }
    }
}
