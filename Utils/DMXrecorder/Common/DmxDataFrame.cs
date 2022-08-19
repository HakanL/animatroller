using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class DmxDataFrame : BaseDmxFrame
    {
        public byte[] Data { get; set; }

        public int UniverseId { get; set; }

        public DmxDataFrame()
        {
        }

        public DmxDataFrame(DmxDataFrame source)
        {
            Data = source.Data;
            UniverseId = source.UniverseId;
            SyncAddress = source.SyncAddress;
            Destination = source.Destination;
        }

        public static DmxDataFrame CreateFrame(int universe, int syncAddress, byte[] data, System.Net.IPAddress destination)
        {
            return new DmxDataFrame
            {
                UniverseId = universe,
                SyncAddress = syncAddress,
                Data = data,
                Destination = destination
            };
        }

        public bool IsAllBlack()
        {
            return Data.All(d => d == 0);
        }
    }
}
