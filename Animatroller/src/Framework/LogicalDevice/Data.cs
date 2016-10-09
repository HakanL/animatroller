using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class Data : Dictionary<DataElements, object>, IData
    {
        private static int globalCreationId;
        private int creationId;

        public Data()
        {
            creationId = ++globalCreationId;
        }

        public Data(DataElements dataElement, object value)
        {
            this[dataElement] = value;
            creationId = ++globalCreationId;
        }

        public IControlToken CurrentToken { get; set; }

        public string CreationId
        {
            get { return this.creationId.ToString(); }
        }
    }
}
