using System;
using System.Linq;
using System.IO;

namespace Animatroller.DMXrecorder
{
    public class BinaryFileWriter : FileWriter
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

        public override void Output(OutputDmxData dmxData)
        {
            switch (dmxData.DataType)
            {
                case OutputDmxData.DataTypes.FullFrame:
                    this.streamWriter.Write((byte)0x01);
                    this.streamWriter.Write((uint)dmxData.Timestamp);
                    this.streamWriter.Write((ushort)dmxData.Universe);
                    this.streamWriter.Write((ushort)dmxData.Data.Length);
                    this.streamWriter.Write(dmxData.Data);
                    this.streamWriter.Write((byte)0x04);
                    break;
            }
        }
    }
}
