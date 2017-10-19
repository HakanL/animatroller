using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.PostProcessor.Command
{
    public class FileConvert : ICommand
    {
        private Common.IFileReader fileReader;
        private Common.IFileWriter fileWriter;
        private HashSet<int> universes;

        public FileConvert(Common.IFileReader fileReader, Common.IFileWriter fileWriter)
        {
            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
            this.universes = new HashSet<int>();
        }

        public void Execute()
        {
            ulong? timestampOffset = null;

            while (this.fileReader.DataAvailable)
            {
                var data = this.fileReader.ReadFrame();

                if (data.DataType == Common.DmxData.DataTypes.Nop)
                    // Skip/null data
                    continue;

                if (!this.universes.Contains(data.Universe))
                {
                    // Write header
                    this.fileWriter.Header(data.Universe);
                    this.universes.Add(data.Universe);
                }

                if (!timestampOffset.HasValue)
                    timestampOffset = data.TimestampMS;

                data.TimestampMS -= timestampOffset.Value;

                this.fileWriter.Output(data);
            }

            // Write footers
            foreach (int universe in this.universes)
                this.fileWriter.Footer(universe);
        }
    }
}
