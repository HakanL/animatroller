using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.PostProcessor.Command
{
    public class TrimBlack : ICommand
    {
        private readonly Common.IFileReader fileReader;
        private readonly Common.IFileWriter fileWriter;

        public TrimBlack(Common.IFileReader fileReader, Common.IFileWriter fileWriter)
        {
            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
        }

        public void Execute()
        {
            double? timestampOffset = null;

            var positions = new List<long>();
            var blackPositions = new HashSet<long>();

            int inputFrameCount = 0;
            int outputFrameCount = 0;

            long currentPos = 0;

            // Read until we have all starting points
            while (this.fileReader.DataAvailable)
            {
                long pos = currentPos++;

                var data = this.fileReader.ReadFrame();
                inputFrameCount++;

                if (data.DataType == Common.DmxData.DataTypes.FullFrame)
                {
                    positions.Add(pos);
                    if (data.Data.All(x => x == 0))
                        blackPositions.Add(pos);
                    else
                    {
                        if (!timestampOffset.HasValue)
                            timestampOffset = data.TimestampMS;
                    }
                }
            }

            Console.WriteLine("{0} frames in input file", inputFrameCount);

            long firstPos = positions
                .SkipWhile(x => blackPositions.Contains(x))
                .FirstOrDefault();

            long? lastPos = positions
                .Reverse<long>()
                .SkipWhile(x => blackPositions.Contains(x))
                .FirstOrDefault();
            if (lastPos == 0)
                lastPos = null;

            this.fileReader.Rewind();
            // Skip the black data at the beginning of the file
            currentPos = 0;
            while (this.fileReader.DataAvailable)
            {
                long pos = currentPos++;

                if (pos >= firstPos)
                    break;

                this.fileReader.ReadFrame();
            }

            while (this.fileReader.DataAvailable)
            {
                long pos = currentPos++;
                if (lastPos.HasValue && pos > lastPos.Value)
                    break;

                var data = this.fileReader.ReadFrame();

                data.TimestampMS -= timestampOffset.Value;

                this.fileWriter.Output(data);
                outputFrameCount++;
            }

            Console.WriteLine("{0} frames written to output file", outputFrameCount);
        }
    }
}
