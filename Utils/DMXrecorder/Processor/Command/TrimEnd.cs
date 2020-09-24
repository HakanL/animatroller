using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    //FIXME: Make it work with multiple universes

    public class TrimEnd : ICommand
    {
        private Common.IFileReader fileReader;
        private Common.IFileWriter fileWriter;
        private long trimPos;

        public TrimEnd(Common.IFileReader fileReader, Common.IFileWriter fileWriter, long trimPos)
        {
            if (trimPos <= 0)
                throw new ArgumentOutOfRangeException("TrimPos has to be a positive number (> 0)");

            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
            this.trimPos = trimPos;
        }

        public void Execute()
        {
            long pos = 0;
            while (this.fileReader.DataAvailable)
            {
                if (pos >= this.trimPos)
                    break;

                var data = this.fileReader.ReadFrame();
                this.fileWriter.Output(data);

                pos++;
            }
        }
    }
}
