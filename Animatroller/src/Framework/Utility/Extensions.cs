using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Reactive.Linq;

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

        public static double GetDouble(this byte b)
        {
            return ((double)b) / 255.0;
        }

        public static double LimitAndScale(this double d, double start, double length, double min = 0.0, double max = 1.0)
        {
            return ((d.Limit(start, start + length) - start) / length).ScaleToMinMax(min, max);
        }

        public static IObservable<T> Controls<T>(this IObservable<T> input, IObserver<T> control)
        {
            input.Subscribe(control);

            return input;
        }

        public static IObservable<DoubleZeroToOne> Controls(this IObservable<DoubleZeroToOne> input, IObserver<double> control)
        {
            input.Subscribe(x =>
                {
                    control.OnNext(x.Value);
                });

            return input;
        }

        public static void Log(this IObservable<bool> input, string propertyName)
        {
            input.Subscribe(x =>
                {
                    Executor.Current.LogDebug(string.Format("Property [{0}]   Value: {1}", propertyName, x));
                });
        }

        public static void Log(this IObservable<DoubleZeroToOne> input, string propertyName)
        {
            input.Subscribe(x =>
            {
                Executor.Current.LogDebug(string.Format("Property [{0}]   Value: {1:N8}", propertyName, x.Value));
            });
        }

        public static void Log(this IObservable<double> input, string propertyName)
        {
            input.Subscribe(x =>
            {
                Executor.Current.LogDebug(string.Format("Property [{0}]   Value: {1:N8}", propertyName, x));
            });
        }

        public static T GetLatestValue<T>(this ReplaySubject<T> subject, T defaultValue = default(T))
        {
            return subject.MostRecent(defaultValue).First();
        }
    }
}
