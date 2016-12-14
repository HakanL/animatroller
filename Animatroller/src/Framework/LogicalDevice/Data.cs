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
            creationId = ++globalCreationId;

            this[dataElement] = value;
        }

        public Data(params Tuple<DataElements, object>[] value)
        {
            creationId = ++globalCreationId;

            foreach (var kvp in value)
                this[kvp.Item1] = kvp.Item2;
        }

        public IControlToken CurrentToken { get; set; }

        public string CreationId
        {
            get { return this.creationId.ToString(); }
        }

        public IData Copy()
        {
            return new Data(this.Select(x => Tuple.Create(x.Key, x.Value)).ToArray());
        }

        public T? GetValue<T>(DataElements dataElement, T? defaultValue = null) where T : struct
        {
            object value;

            if (!TryGetValue(dataElement, out value))
                return defaultValue;

            return (T)value;
        }
    }
}
