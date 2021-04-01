using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class BaseDmxData
    {
        public enum DataTypes
        {
            NoChange,
            FullFrame,
            Nop,
            Sync
        }

        public DataTypes DataType { get; set; }

        public byte[] Data { get; set; }

        public int? UniverseId { get; set; }

        public int SyncAddress { get; set; }

        public BaseDmxData()
        {
        }

        public BaseDmxData(BaseDmxData source)
        {
            DataType = source.DataType;
            Data = source.Data;
            UniverseId = source.UniverseId;
            SyncAddress = source.SyncAddress;
        }

        public static BaseDmxData CreateFullFrame(int universe, int syncAddress, byte[] data)
        {
            return new BaseDmxData
            {
                DataType = DataTypes.FullFrame,
                UniverseId = universe,
                SyncAddress = syncAddress,
                Data = data
            };
        }
    }
}
