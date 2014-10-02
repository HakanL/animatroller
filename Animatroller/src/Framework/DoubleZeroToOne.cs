using System;
using System.Collections.Generic;

namespace Animatroller.Framework
{
    public class DoubleZeroToOne : IControlData
    {
        public static readonly DoubleZeroToOne Zero = new DoubleZeroToOne();
        public static readonly DoubleZeroToOne Full = new DoubleZeroToOne(1.0);

        public DoubleZeroToOne()
        {
        }

        public DoubleZeroToOne(double initialValue)
        {
            Value = initialValue;
        }

        public double Value { get; set; }

        public bool IsValid()
        {
            return Value >= 0.0 && Value <= 1.0;
        }
    }
}
