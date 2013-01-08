using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public interface ICanExecute
    {
        void Execute(System.Threading.CancellationToken cancelToken);
        bool IsMultiInstance { get; }
        string Name { get; }
    }
}
