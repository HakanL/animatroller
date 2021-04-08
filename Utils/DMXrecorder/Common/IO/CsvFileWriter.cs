using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common.IO
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

        public override void Output(DmxDataOutputPacket dmxData)
        {
            switch (dmxData.Content)
            {
                case SyncFrame syncFrame:
                    this.streamWriter.WriteLine("{0},{1},{2},Sync", dmxData.Sequence, dmxData.TimestampMS, syncFrame.SyncAddress);
                    break;

                case DmxDataFrame dmxDataFrame:
                    this.streamWriter.WriteLine("{0},{1},{2},Full,{3}",
                        dmxData.Sequence, dmxData.TimestampMS, dmxDataFrame.UniverseId, string.Join(",", dmxDataFrame.Data.Select(x => x.ToString())));
                    break;
            }
        }
    }
}
