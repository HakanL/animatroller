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

        public long Timestamp { get; set; }

        public long Sequence { get; set; }

        public int Universe { get; set; }

        private RawDmxData()
        {
        }

        public static RawDmxData Create(long millisecond, long sequence, int universe, byte[] data)
        {
            return new RawDmxData
            {
                Data = data,
                Sequence = sequence,
                Timestamp = millisecond,
                Universe = universe
            };
        }
    }
}
