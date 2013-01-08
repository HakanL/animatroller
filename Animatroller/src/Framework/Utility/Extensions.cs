﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Extensions
{
    public static class Extensions
    {
        public static double Limit(this double d, double min, double max)
        {
            if (d < min)
                return min;

            if (d > max)
                return max;

            return d;
        }

        public static bool WithinLimits(this double d, double min, double max)
        {
            if (d < min)
                return false;

            if (d > max)
                return false;

            return true;
        }

        public static double ScaleToMinMax(this double d, double min, double max)
        {
            return d * (max - min) + min;
        }

        public static byte GetByteScale(this double d)
        {
            return GetByteScale(d, 255);
        }

        public static byte GetByteScale(this double d, int scale)
        {
            return (byte)(d.Limit(0, 1) * scale);
        }
    }
}
