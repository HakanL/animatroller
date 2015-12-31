using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.PostProcessor
{
    public class TrimBlack
    {
        private Common.BinaryFileReader fileReader;
        private Common.BinaryFileWriter fileWriter;

        public TrimBlack(Common.BinaryFileReader fileReader, Common.BinaryFileWriter fileWriter)
        {
            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
        }

        public void Execute()
        {
            long? timestampOffset = null;

            var positions = new List<long>();
            var blackPositions = new HashSet<long>();

            int inputFrameCount = 0;
            int outputFrameCount = 0;

            // Read until we have all starting points
            while (this.fileReader.DataAvailable)
            {
                long pos = this.fileReader.Position;

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
                            timestampOffset = data.Timestamp;
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

            this.fileReader.Position = firstPos;
            while (this.fileReader.DataAvailable)
            {
                long pos = this.fileReader.Position;
                if (lastPos.HasValue && pos > lastPos.Value)
                    break;

                var data = this.fileReader.ReadFrame();

                data.Timestamp -= timestampOffset.Value;

                this.fileWriter.Output(data);
                outputFrameCount++;
            }

            Console.WriteLine("{0} frames written to output file", outputFrameCount);
        }
    }
}
