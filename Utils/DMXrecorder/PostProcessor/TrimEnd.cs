using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.PostProcessor
{
    public class TrimEnd
    {
        private Common.BinaryFileReader fileReader;
        private Common.BinaryFileWriter fileWriter;
        private long trimPos;

        public TrimEnd(Common.BinaryFileReader fileReader, Common.BinaryFileWriter fileWriter, long trimPos)
        {
            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
            this.trimPos = trimPos;
        }

        public void Execute()
        {
            while (this.fileReader.DataAvailable)
            {
                if (this.fileReader.Position >= this.trimPos)
                    break;

                var data = this.fileReader.ReadFrame();
                this.fileWriter.Output(data);
            }
        }
    }
}
