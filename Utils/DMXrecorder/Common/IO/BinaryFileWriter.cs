using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common.IO
{
    public class BinaryFileWriter : BaseFileWriter
    {
        private BinaryWriter streamWriter;

        public BinaryFileWriter(string fileName)
            : base(fileName)
        {
            this.streamWriter = new BinaryWriter(this.fileStream);
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
                case DmxDataFrame dmxDataFrame:
                    this.streamWriter.Write((byte)0x01);
                    this.streamWriter.Write((uint)dmxData.TimestampMS);
                    this.streamWriter.Write((ushort)dmxDataFrame.UniverseId);
                    this.streamWriter.Write((ushort)dmxDataFrame.Data.Length);
                    this.streamWriter.Write(dmxDataFrame.Data);
                    this.streamWriter.Write((byte)0x04);
                    break;

                case SyncFrame syncFrame:
                    this.streamWriter.Write((byte)0x02);
                    this.streamWriter.Write((uint)dmxData.TimestampMS);
                    this.streamWriter.Write((ushort)syncFrame.SyncAddress);
                    this.streamWriter.Write((byte)0x04);
                    break;
            }
        }
    }
}
