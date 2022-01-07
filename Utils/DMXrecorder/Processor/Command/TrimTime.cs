using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    public class TrimTime : ICommandInputOutput
    {
        private readonly double trimStart;
        private readonly double? trimEnd;
        private readonly double? trimDuration;

        public TrimTime(double? trimStart, double? trimEnd, double? trimDuration)
        {
            if (trimStart <= 0)
                throw new ArgumentOutOfRangeException("TrimPos has to be a positive number (> 0)");

            this.trimStart = trimStart ?? 0;
            this.trimEnd = trimEnd;
            this.trimDuration = trimDuration;
        }

        public void Execute(ProcessorContext context, Common.IInputReader inputReader, IOutputWriter outputWriter)
        {
            Common.InputFrame data;
            while ((data = inputReader.ReadFrame()) != null)
            {
                if (data.TimestampMS < this.trimStart)
                    continue;

                if (this.trimEnd.HasValue && data.TimestampMS >= this.trimEnd.Value)
                    break;

                double duration = data.TimestampMS - this.trimStart;
                if (this.trimDuration.HasValue && duration > this.trimDuration.Value)
                    break;

                outputWriter.Output(context, data);
            }
        }
    }
}
