﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Framework.Import
{
    public class DmxData
    {
        public enum DataTypes
        {
            NoChange,
            FullFrame,
            Nop
        }

        public DataTypes DataType { get; set; }

        public byte[] Data { get; set; }

        public ulong TimestampMS { get; set; }

        public long Sequence { get; set; }

        public int Universe { get; set; }

        public DmxData()
        {
        }

        public static DmxData CreateNoChange(ulong millisecond, long sequence, int universe)
        {
            return new DmxData
            {
                DataType = DataTypes.NoChange,
                Data = null,
                Sequence = sequence,
                TimestampMS = millisecond,
                Universe = universe
            };
        }

        public static DmxData CreateFullFrame(ulong millisecond, long sequence, int universe, byte[] data)
        {
            return new DmxData
            {
                DataType = DataTypes.FullFrame,
                Data = data,
                Sequence = sequence,
                TimestampMS = millisecond,
                Universe = universe
            };
        }
    }
}
