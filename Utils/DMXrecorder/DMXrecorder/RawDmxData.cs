using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.DMXrecorder
{
    public class RawDmxData
    {
        public byte[] Data { get; set; }

        public double TimestampMS { get; set; }

        public long Sequence { get; set; }

        public int Universe { get; set; }

        public int SyncAddress { get; set; }

        private RawDmxData()
        {
        }

        public static RawDmxData Create(double millisecond, long sequence, int universe, byte[] data, int syncAddress)
        {
            return new RawDmxData
            {
                Data = data,
                Sequence = sequence,
                TimestampMS = millisecond,
                Universe = universe,
                SyncAddress = syncAddress
            };
        }
    }
}
