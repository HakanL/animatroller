using Animatroller.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor
{
    public interface ICommandOutput : ICommand
    {
        void Execute(ProcessorContext context, IOutputWriter outputWriter);
    }
}
