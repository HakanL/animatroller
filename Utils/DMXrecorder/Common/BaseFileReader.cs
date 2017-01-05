using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common
{
    public abstract class BaseFileReader : IDisposable
    {
        protected FileStream fileStream;

        public BaseFileReader(string fileName)
        {
            this.fileStream = File.OpenRead(fileName);
        }

        public virtual void Dispose()
        {
            this.fileStream?.Dispose();
            this.fileStream = null;
        }

        public bool DataAvailable
        {
            get { return this.fileStream.Position < this.fileStream.Length; }
        }

        public long Position
        {
            get { return this.fileStream.Position; }
            set { this.fileStream.Position = value; }
        }

        public long Length
        {
            get { return this.fileStream.Length; }
        }

        public abstract DmxData ReadFrame();
    }
}
