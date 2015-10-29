using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Threading.Tasks;
using System.Reactive.Subjects;

namespace Animatroller.Framework
{
    public interface IDisposableObserver<T> : IObserver<T>, IDisposable
    {
    }

    public interface IReceivesBrightness : IReceivesData
    {
        //        LogicalDevice.ControlledObserver<double> GetBrightnessObserver(IControlToken token = null);

        double Brightness { get; set; }
    }

    public interface IReceivesStrobeSpeed : IReceivesData
    {
        double StrobeSpeed { get; set; }
    }

    public interface IReceivesData : IOwnedDevice
    {
        LogicalDevice.ControlledObserverData GetDataObserver(IControlToken token = null);
    }

    public interface IReceivesColor : IReceivesData
    {
        //        LogicalDevice.ControlledObserver<Color> GetColorObserver(IControlToken token = null);

        //        LogicalDevice.ControlledObserverRGB GetRgbObserver(IControlToken token = null);

        Color Color { get; set; }
    }

    public interface IReceivesPanTilt : IOwnedDevice
    {
        LogicalDevice.ControlledObserver<double> GetPanObserver(IControlToken token = null);

        LogicalDevice.ControlledObserver<double> GetTiltObserver(IControlToken token = null);

        double Pan { get; set; }

        double Tilt { get; set; }
    }

    public interface ISendsData
    {
        IObservable<IData> OutputData { get; }

        IData CurrentData { get; }
    }

    //    public interface ISendsBrightness : ISendsData
    //    {
    ////        IObservable<double> OutputBrightness { get; }
    //    }

    //    public interface ISendsColor : ISendsData
    //    {
    ////        IObservable<Color> OutputColor { get; }
    //    }

    //    public interface ISendsStrobeSpeed : ISendsData
    //    {
    //        //        IObservable<double> OutputStrobeSpeed { get; }
    //    }

    public enum DataElements
    {
        Unknown = 0,
        Brightness,
        Color,
        StrobeSpeed
    }

    public interface IData : IDictionary<DataElements, object>
    {
    }

    //public interface IDataBrightness : IData
    //{
    //    double Brightness { get; }
    //}

    //public interface IDataColor : IData
    //{
    //    Color Color { get; }
    //}

    //public interface IDataStrobeSpeed : IData
    //{
    //    double StrobeSpeed { get; }
    //}

    public interface IControlToken : IDisposable
    {
        int Priority { get; }

        void PushData(DataElements dataElement, object value);

        IData Data { get; }
    }

    public interface IOwnedDevice : IDevice
    {
        IControlToken TakeControl(int priority, string name = "");

        bool HasControl(IControlToken checkOwner);

        bool IsOwned { get; }
    }

    public interface IOwner
    {
        string Name { get; }

        [Obsolete]
        int Priority { get; }
    }

    public interface IHasAnalogInput
    {

    }

    public interface IDevice
    {
        string Name { get; }
    }

    public interface IRunningDevice : IDevice
    {
        void SetInitialState();
    }

    public interface ILogicalDevice : IRunningDevice
    {
    }

    public interface IApiVersion3 : ILogicalDevice
    {
    }

    public interface IPhysicalDevice : IRunningDevice
    {
    }

    public interface IControlledDevice : ILogicalDevice
    {
        void Suspend();
        void Resume();
        void TurnOff();
    }
}
