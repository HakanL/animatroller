using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    public class TrimFrame : ICommandInputOutput
    {
        private readonly long trimStart;
        private readonly long? trimEnd;
        private readonly long? trimDuration;

        public TrimFrame(long? trimStart, long? trimEnd, long? trimDuration)
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
                if (data.Position < this.trimStart)
                    continue;

                if (this.trimEnd.HasValue && data.Position >= this.trimEnd.Value)
                    break;

                long duration = data.Position - this.trimStart;
                if (this.trimDuration.HasValue && duration >= this.trimDuration.Value)
                    break;

                outputWriter.Output(context, data);
            }
        }
    }
}
