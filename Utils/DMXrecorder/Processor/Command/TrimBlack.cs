using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Common;

namespace Animatroller.Processor.Command
{
    public class TrimBlack : ICommand
    {
        private readonly IInputReader inputReader;
        private readonly ITransformer transformer;

        public TrimBlack(IInputReader inputReader, ITransformer transformer)
        {
            this.inputReader = inputReader;
            this.transformer = transformer;
        }

        public void Execute(TransformContext context)
        {
            double? timestampOffset = null;

            var positions = new List<long>();
            var blackPositions = new HashSet<long>();

            int inputFrameCount = 0;
            int outputFrameCount = 0;

            long currentPos = 0;

            // Read until we have all starting points
            while (true)
            {
                long pos = currentPos++;

                var data = this.inputReader.ReadFrame();
                if (data == null)
                    break;

                inputFrameCount++;

                if (data.Content is DmxDataFrame)
                {
                    this.transformer.Transform(context, data, packet =>
                    {
                        if (packet is DmxDataFrame dmxDataFrame)
                        {
                            positions.Add(pos);
                            if (dmxDataFrame.Data.All(x => x == 0))
                            {
                                blackPositions.Add(pos);
                            }
                            else
                            {
                                if (!timestampOffset.HasValue)
                                    timestampOffset = data.TimestampMS;
                            }
                        }
                    });
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

            this.inputReader.Rewind();
            // Skip the black data at the beginning of the file
            currentPos = 0;
            while (true)
            {
                long pos = currentPos;

                if (pos >= firstPos)
                    break;

                currentPos++;
                var data = this.inputReader.ReadFrame();
                if (data == null)
                    break;
            }

            while (true)
            {
                long pos = currentPos++;
                if (lastPos.HasValue && pos > lastPos.Value)
                    break;

                var data = this.inputReader.ReadFrame();
                if (data == null)
                    break;

                data.TimestampMS -= timestampOffset.Value;

                throw new NotImplementedException();
                //this.fileWriter.Output(data);
                outputFrameCount++;
            }

            Console.WriteLine("{0} frames written to output file", outputFrameCount);
        }
    }
}
