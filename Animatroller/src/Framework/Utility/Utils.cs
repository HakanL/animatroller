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

        public static Tuple<DataElements, object> AdditionalData(DataElements dataElement, double value)
        {
            return Tuple.Create(dataElement, (object)value);
        }

        public static Tuple<DataElements, object> AdditionalData(DataElements dataElement, bool value)
        {
            return Tuple.Create(dataElement, (object)value);
        }

        public static Tuple<DataElements, object> AdditionalData(Color color)
        {
            return Tuple.Create(DataElements.Color, (object)color);
        }

        public static IData Data(Color color, double brightness = 1.0)
        {
            return new LogicalDevice.Data(
                Utils.AdditionalData(DataElements.Color, color),
                Utils.AdditionalData(DataElements.Brightness, brightness));
        }
    }
}
