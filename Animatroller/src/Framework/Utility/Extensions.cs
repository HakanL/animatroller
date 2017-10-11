using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Drawing;

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

        public static void Controls<T>(this LogicalDevice.ILogicalOutputDevice<T> input, IObserver<T> control)
        {
            input.Output.Subscribe(control);
        }

        public static IObservable<T> Controls<T>(this IObservable<T> input, Action<T> control)
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

        //public static void ConnectTo<T>(this Animatroller.Framework.LogicalDevice.ILogicalOutputDevice<T> device, IObserver<T> component)
        //{
        //    device.Output.Subscribe(component);
        //}

        public static void WhenOutputChanges<T>(this Animatroller.Framework.LogicalDevice.ILogicalOutputDevice<T> device, Action<T> onNext)
        {
            device.Output.Subscribe(onNext);
        }

        public static void Follow(this Animatroller.Framework.LogicalDevice.ILogicalControlDevice<bool> device, Animatroller.Framework.LogicalDevice.OperatingHours2 operatingHours)
        {
            // Initially turned on
            device.Control.OnNext(true);

            operatingHours.Output.Subscribe(x =>
                {
                    device.Control.OnNext(x);
                });
        }

        public static IData GenerateIData(this Tuple<DataElements, object>[] dataElements)
        {
            IData data = null;

            if (dataElements.Any())
            {
                data = new LogicalDevice.Data();
                foreach (var kvp in dataElements)
                    data[kvp.Item1] = kvp.Item2;
            }

            return data;
        }

        public static void SetData(this IReceivesData device, IControlToken token, params Tuple<DataElements, object>[] data)
        {
            device.SetData(token, new LogicalDevice.Data(data));
        }

        public static void SetBrightness(this IReceivesBrightness device, double brightness, IControlToken token = null)
        {
            device.SetData(token, Utils.Data(DataElements.Brightness, brightness));
        }

        public static void SetStrobeSpeed(this IReceivesStrobeSpeed device, double strobeSpeed, IControlToken token = null)
        {
            device.SetData(token, Utils.Data(DataElements.StrobeSpeed, strobeSpeed));
        }

        public static void SetColor(this IReceivesColor device, Color color, double? brightness, IControlToken token = null)
        {
            if (brightness.HasValue)
                device.SetData(token, Utils.Data(color, brightness.Value));
            else
                device.SetData(token, Utils.Data(color));
        }

        public static void SetColor(this IReceivesColor device, Color color, IControlToken token = null)
        {
            device.SetColor(color, null, token);
        }

        public static void SetPanTilt(this IReceivesStrobeSpeed device, double pan, double tilt, IControlToken token = null)
        {
            device.SetData(token, Utils.Data(DataElements.Pan, pan), Utils.Data(DataElements.Tilt, tilt));
        }

        public static double GetCurrentBrightness(this IReceivesBrightness device)
        {
            return (double)device.GetCurrentData(DataElements.Brightness);
        }

        public static Color GetCurrentColor(this IReceivesColor device)
        {
            return (Color)device.GetCurrentData(DataElements.Color);
        }

        public static double GetCurrentStrobeSpeed(this IReceivesStrobeSpeed device)
        {
            return (double)device.GetCurrentData(DataElements.StrobeSpeed);
        }

        public static double GetCurrentPan(this IReceivesPanTilt device)
        {
            return (double)device.GetCurrentData(DataElements.Pan);
        }

        public static double GetCurrentTilt(this IReceivesPanTilt device)
        {
            return (double)device.GetCurrentData(DataElements.Tilt);
        }
    }
}
