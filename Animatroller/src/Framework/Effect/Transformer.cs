using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Effect.Transformer
{
    public class Sinus : ITransformer
    {
        /// <summary>
        /// Transform the input to sinus
        /// </summary>
        /// <param name="input">Between 0 and 1</param>
        /// <returns>Sin for input range</returns>
        public double Transform(double input)
        {
            return Math.Sin(input * Math.PI);
        }
    }

    public class EaseIn : ITransformer
    {
        /// <summary>
        /// Ease-In
        /// </summary>
        /// <param name="input">Between 0 and 1</param>
        /// <returns>Result for input range</returns>
        public double Transform(double input)
        {
            return input * input;
        }
    }

    public class EaseOut : ITransformer
    {
        /// <summary>
        /// Ease-Out
        /// </summary>
        /// <param name="input">Between 0 and 1</param>
        /// <returns>Result for input range</returns>
        public double Transform(double input)
        {
            return -1.0 * input * (input - 2);
        }
    }

    public class EaseInOut : ITransformer
    {
        /// <summary>
        /// Ease-In/Out
        /// </summary>
        /// <param name="input">Between 0 and 1</param>
        /// <returns>Result for input range</returns>
        public double Transform(double input)
        {
            double t = input * 2;
            if (t < 1)
                return 1.0 / 2.0 * t * t;
            t--;
            return -1.0 / 2.0 * (t * (t - 2) - 1);
        }
    }
}
