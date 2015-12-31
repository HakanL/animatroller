using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class BinaryFileReader : IDisposable
    {
        private FileStream file;
        private BinaryReader binRead;

        public BinaryFileReader(string fileName)
        {
            this.file = System.IO.File.OpenRead(fileName);
            this.binRead = new System.IO.BinaryReader(file);
        }

        public void Dispose()
        {
            if (this.file != null)
            {
                this.file.Dispose();

                this.file = null;
            }
        }

        public bool DataAvailable
        {
            get { return this.file.Position < this.file.Length; }
        }

        public long Position
        {
            get { return this.file.Position; }
            set { this.file.Position = value; }
        }

        public long Length
        {
            get { return this.file.Length; }
        }

        public DmxData ReadFrame()
        {
            var target = new DmxData();
            byte start = this.binRead.ReadByte();
            target.Timestamp = (uint)this.binRead.ReadInt32();
            target.Universe = (ushort)this.binRead.ReadInt16();
            switch (start)
            {
                case 1:
                    target.DataType = DmxData.DataTypes.FullFrame;
                    ushort len = (ushort)this.binRead.ReadInt16();
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
