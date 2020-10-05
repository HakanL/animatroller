using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common
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

        public override void Output(DmxData dmxData)
        {
            switch (dmxData.DataType)
            {
                case DmxData.DataTypes.FullFrame:
                    this.streamWriter.Write((byte)0x01);
                    this.streamWriter.Write((uint)dmxData.TimestampMS);
                    this.streamWriter.Write((ushort)dmxData.UniverseId);
                    this.streamWriter.Write((ushort)dmxData.Data.Length);
                    this.streamWriter.Write(dmxData.Data);
                    this.streamWriter.Write((byte)0x04);
                    break;

                case DmxData.DataTypes.NoChange:
                    this.streamWriter.Write((byte)0x02);
                    this.streamWriter.Write((uint)dmxData.TimestampMS);
                    this.streamWriter.Write((ushort)dmxData.UniverseId);
                    this.streamWriter.Write((byte)0x04);
                    break;
            }
        }
    }
}
