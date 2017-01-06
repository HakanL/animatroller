using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common
{
    public abstract class BaseFileWriter : IFileWriter, IDisposable
    {
        protected FileStream fileStream;

        public BaseFileWriter(string fileName)
        {
            this.fileStream = File.Create(fileName);
        }

        public abstract void Dispose();

        public virtual void Header(int universeId)
        {
        }

        public abstract void Output(DmxData dmxData);

        public virtual void Footer(int universeId)
        {
        }
    }
}
