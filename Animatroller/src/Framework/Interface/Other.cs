using System;
using System.Collections.Generic;
using System.Drawing;

namespace Animatroller.Framework
{
    public interface IReceivesData : IOwnedDevice
    {
        LogicalDevice.ControlledObserverData GetDataObserver(IControlToken token);

        //        void PushData(IControlToken token, IData data);

        void PushOutput(IControlToken token);

        void BuildDefaultData(IData data);
    }

    public interface IReceivesBrightness : IReceivesData
    {
        double Brightness { get; }

        void SetBrightness(double brightness, IControlToken token);
    }

    public interface IReceivesStrobeSpeed : IReceivesData
    {
        double StrobeSpeed { get; }

        void SetStrobeSpeed(double strobeSpeed, IControlToken token);
    }

    public interface IReceivesColor : IReceivesData
    {
        Color Color { get; }

        void SetColor(Color color, double? brightness, IControlToken token);
    }

    public interface IReceivesPanTilt : IReceivesData
    {
        double Pan { get; }

        double Tilt { get; }

        void SetPanTilt(double pan, double tilt, IControlToken token);
    }

    public interface ISendsData : ILogicalDevice
    {
        IObservable<IData> OutputData { get; }

        IObservable<IData> OutputChanged { get; }

        IData CurrentData { get; }
    }

    public interface IData : IDictionary<DataElements, object>
    {
        IControlToken CurrentToken { get; set; }
    }

    [Obsolete]
    public interface IOwner
    {
        string Name { get; }

        [Obsolete]
        int Priority { get; }
    }

    public interface IHasAnalogInput
    {

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
