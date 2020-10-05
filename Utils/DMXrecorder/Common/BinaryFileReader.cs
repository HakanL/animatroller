using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class BinaryFileReader : BaseFileReader
    {
        private BinaryReader binRead;

        public BinaryFileReader(string fileName)
            : base(fileName)
        {
            this.binRead = new System.IO.BinaryReader(this.fileStream);
        }

        public override void Dispose()
        {
            this.binRead.Dispose();
            base.Dispose();
        }

        public override DmxData ReadFrame()
        {
            var target = new DmxData();
            byte start = this.binRead.ReadByte();
            target.TimestampMS = (uint)this.binRead.ReadInt32();
            target.UniverseId = this.binRead.ReadUInt16();
            switch (start)
            {
                case 1:
                    target.DataType = DmxData.DataTypes.FullFrame;
                    ushort len = (ushort)this.binRead.ReadUInt16();
                    target.Data = this.binRead.ReadBytes(len);
                    break;

                case 2:
                    target.DataType = DmxData.DataTypes.NoChange;
                    break;

                default:
                    throw new ArgumentException("Invalid data");
            }
            byte end = this.binRead.ReadByte();

            if (end != 4)
                throw new ArgumentException("Invalid data");

            return target;
        }
    }
}
