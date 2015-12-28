using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.DMXrecorder
{
    public class OutputDmxData
    {
        public enum DataTypes
        {
            NoChange,
            FullFrame
        }

        public DataTypes DataType { get; set; }

        public byte[] Data { get; set; }

        public long Timestamp { get; set; }

        public long Sequence { get; set; }

        public int Universe { get; set; }

        private OutputDmxData()
        {
        }

        public static OutputDmxData CreateNoChange(long millisecond, long sequence, int universe)
        {
            return new OutputDmxData
            {
                DataType = DataTypes.NoChange,
                Data = null,
                Sequence = sequence,
                Timestamp = millisecond,
                Universe = universe
            };
        }

        public static OutputDmxData CreateFullFrame(long millisecond, long sequence, int universe, byte[] data)
        {
            return new OutputDmxData
            {
                DataType = DataTypes.FullFrame,
                Data = data,
                Sequence = sequence,
                Timestamp = millisecond,
                Universe = universe
            };
        }
    }
}
