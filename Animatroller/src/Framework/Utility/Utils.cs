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
        public static Tuple<DataElements, object> Data(DataElements dataElement, Color color)
        {
            return Tuple.Create(dataElement, (object)color);
        }

        public static Tuple<DataElements, object> Data(DataElements dataElement, double value)
        {
            return Tuple.Create(dataElement, (object)value);
        }

        public static Tuple<DataElements, object> Data(DataElements dataElement, bool value)
        {
            return Tuple.Create(dataElement, (object)value);
        }

        public static Tuple<DataElements, object> Data(Color color)
        {
            return Tuple.Create(DataElements.Color, (object)color);
        }

        public static Tuple<DataElements, object> Data(double brightness)
        {
            return Tuple.Create(DataElements.Brightness, (object)brightness);
        }

        public static IData Data(Color color, double brightness = 1.0)
        {
            return new LogicalDevice.Data(
                Utils.Data(DataElements.Color, color),
                Utils.Data(DataElements.Brightness, brightness));
        }

        public static Utility.ReactiveOr ReactiveOr(params IObservable<bool>[] inputs)
        {
            return new Utility.ReactiveOr(inputs);
        }

        public static Utility.ReactiveOr ReactiveOr(params LogicalDevice.ILogicalOutputDevice<bool>[] inputs)
        {
            return new Utility.ReactiveOr(inputs.Select(x => x.Output).ToArray());
        }
    }
}
