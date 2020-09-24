using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    public class FileConvert : ICommand
    {
        private readonly Common.IFileReader fileReader;
        private readonly Common.IFileWriter fileWriter;
        private readonly HashSet<int> universes;
        private readonly ITransformer transformer;

        public FileConvert(Common.IFileReader fileReader, Common.IFileWriter fileWriter, ITransformer transformer)
        {
            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
            this.universes = new HashSet<int>();
            this.transformer = transformer;
        }

        public void Execute()
        {
            double? timestampOffset = null;

            while (this.fileReader.DataAvailable)
            {
                var data = this.fileReader.ReadFrame();

                if (data.DataType == Common.DmxData.DataTypes.Nop)
                    // Skip/null data
                    continue;

                if (!timestampOffset.HasValue)
                    timestampOffset = data.TimestampMS;

                this.transformer.Transform(data.Universe, data.Data, (universeId, dmxData) =>
                {
                    if (!this.universes.Contains(universeId))
                    {
                        // Write header
                        this.fileWriter.Header(universeId);
                        this.universes.Add(universeId);
                    }

                    this.fileWriter.Output(new Common.DmxData
                    {
                        Data = dmxData,
                        DataType = data.DataType,
                        Sequence = data.Sequence,
                        TimestampMS = data.TimestampMS - timestampOffset.Value,
                        Universe = universeId
                    });
                });
            }

            // Write footers
            foreach (int universe in this.universes)
                this.fileWriter.Footer(universe);
        }
    }
}
