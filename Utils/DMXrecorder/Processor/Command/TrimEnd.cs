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
        private readonly Common.IFileReader fileReader;
        private readonly Common.IFileWriter fileWriter;
        private readonly long trimPos;
        private readonly ITransformer transformer;

        public TrimEnd(Common.IFileReader fileReader, Common.IFileWriter fileWriter, long trimPos, ITransformer transformer)
        {
            if (trimPos <= 0)
                throw new ArgumentOutOfRangeException("TrimPos has to be a positive number (> 0)");

            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
            this.trimPos = trimPos;
            this.transformer = transformer;
        }

        public void Execute()
        {
            long pos = 0;
            while (this.fileReader.DataAvailable)
            {
                if (pos >= this.trimPos)
                    break;

                var data = this.fileReader.ReadFrame();

                this.transformer.Transform(data.UniverseId, data.Data, (universeId, dmxData, sequence) =>
                {
                    this.fileWriter.Output(new Common.DmxData(universeId, dmxData, sequence, data.TimestampMS));
                });

                pos++;
            }
        }
    }
}
