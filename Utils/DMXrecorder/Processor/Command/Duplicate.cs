using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    public class Duplicate : ICommandInputOutput
    {
        private readonly int extraCopies;

        public Duplicate(int extraCopies)
        {
            this.extraCopies = extraCopies;
        }

        public void Execute(ProcessorContext context, Common.IInputReader inputReader, IOutputWriter outputWriter)
        {
            Common.InputFrame data;

            for (int copy = 0; copy <= this.extraCopies; copy++)
            {
                while ((data = inputReader.ReadFrame()) != null)
                {
                    outputWriter.Output(context, data);
                }

                inputReader.Rewind();
            }
        }
    }
}
