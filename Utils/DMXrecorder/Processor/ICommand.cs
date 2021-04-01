using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor
{
    public interface ICommand
    {
        void Execute(TransformContext context);
    }
}
