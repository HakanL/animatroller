using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    internal interface ISupportsPersistence
    {
        void SetValueFromPersistence(Func<string, string, string> getKeyFunc);

        void SaveValueToPersistence(Action<string, string> setKeyFunc);

        bool PersistState { get; }
    }
}
