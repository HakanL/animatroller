using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class Data : Dictionary<DataElements, object>, IData
    {
        public Data()
        {
        }

        public Data(DataElements dataElement, object value)
        {
            this[dataElement] = value;
        }
    }
}
