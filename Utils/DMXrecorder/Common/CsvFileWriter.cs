﻿using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common
{
    public class CsvFileWriter : BaseFileWriter
    {
        private StreamWriter streamWriter;

        public CsvFileWriter(string fileName)
            : base(fileName)
        {
            this.streamWriter = new StreamWriter(this.fileStream);
        }

        public override void Dispose()
        {
            this.streamWriter.Flush();
            this.streamWriter.Dispose();
        }

        public override void Output(DmxDataPacket dmxData)
        {
            switch (dmxData.DataType)
            {
                case DmxDataFrame.DataTypes.NoChange:
                    this.streamWriter.WriteLine("{0},{1},{2},NoChange", dmxData.Sequence, dmxData.TimestampMS, dmxData.UniverseId);
                    break;

                case DmxDataFrame.DataTypes.FullFrame:
                    this.streamWriter.WriteLine("{0},{1},{2},Full,{3}",
                        dmxData.Sequence, dmxData.TimestampMS, dmxData.UniverseId, string.Join(",", dmxData.Data.Select(x => x.ToString())));
                    break;
            }
        }
    }
}
