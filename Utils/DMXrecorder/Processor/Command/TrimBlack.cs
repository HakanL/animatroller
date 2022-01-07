using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    public class TrimBlack : ICommandInputOutput
    {
        private readonly bool firstFrameBlack;

        public TrimBlack(bool firstFrameBlack)
        {
            this.firstFrameBlack = firstFrameBlack;
        }

        public void Execute(ProcessorContext context, Common.IInputReader inputReader, IOutputWriter outputWriter)
        {
            double? startBlackTimestamp = null;
            double? endBlackTimestamp = null;
            double lastTimestamp = 0;

            Common.InputFrame data;
            while ((data = inputReader.ReadFrame()) != null)
            {
                if (data.IsAllBlack())
                {
                    if (startBlackTimestamp.HasValue && !endBlackTimestamp.HasValue)
                        endBlackTimestamp = data.TimestampMS;
                }
                else
                {
                    if (!startBlackTimestamp.HasValue)
                        startBlackTimestamp = lastTimestamp;

                    endBlackTimestamp = null;
                }

                lastTimestamp = data.TimestampMS;
            }

            if (startBlackTimestamp.HasValue)
                Console.WriteLine($"Start trimming at {startBlackTimestamp:N1} mS");
            if (endBlackTimestamp.HasValue)
                Console.WriteLine($"Stop trimming at {endBlackTimestamp:N1} mS");

            inputReader.Rewind();

            while ((data = inputReader.ReadFrame()) != null)
            {
                if (this.firstFrameBlack)
                {
                    if (startBlackTimestamp.HasValue && data.TimestampMS < startBlackTimestamp.Value)
                        continue;
                }
                else
                {
                    if (startBlackTimestamp.HasValue && data.TimestampMS <= startBlackTimestamp.Value)
                        continue;
                }

                if (endBlackTimestamp.HasValue && data.TimestampMS >= endBlackTimestamp.Value)
                    break;

                outputWriter.Output(context, data);
            }
        }
    }
}
