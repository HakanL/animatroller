using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor
{
    public interface ICommandInput : ICommand
    {
        void Execute(ProcessorContext context, Common.IInputReader inputReader);
    }
}
