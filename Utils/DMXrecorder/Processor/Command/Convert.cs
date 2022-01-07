using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    public class Convert : ICommandInputOutput
    {
        public Convert()
        {
        }

        public void Execute(ProcessorContext context, Common.IInputReader inputReader, IOutputWriter outputWriter)
        {
            Common.InputFrame data;
            while ((data = inputReader.ReadFrame()) != null)
            {
                outputWriter.Output(context, data);
            }
        }
    }
}
