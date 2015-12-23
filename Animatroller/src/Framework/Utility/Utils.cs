using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public class Utils
    {
        public static Tuple<DataElements, object> AdditionalData(DataElements dataElement, Color color)
        {
            return Tuple.Create(dataElement, (object)color);
        }

        public static Tuple<DataElements, object> AdditionalData(Color color)
        {
            return Tuple.Create(DataElements.Color, (object)color);
        }
    }
}
