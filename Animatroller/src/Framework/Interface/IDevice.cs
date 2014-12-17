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

    public interface IReceivesBrightness : IOwnedDevice
    {
        Animatroller.Framework.LogicalDevice.ControlledObserver<double> GetBrightnessObserver();

        double Brightness { get; set; }
    }

    public interface IReceivesColor : IOwnedDevice
    {
        Animatroller.Framework.LogicalDevice.ControlledObserver<Color> GetColorObserver();

        Animatroller.Framework.LogicalDevice.ControlledObserverRGB GetRgbObsserver();

        Color Color { get; set; }
    }

    public interface IReceivesPanTilt : IOwnedDevice
    {
        Animatroller.Framework.LogicalDevice.ControlledObserver<double> GetPanObserver();

        Animatroller.Framework.LogicalDevice.ControlledObserver<double> GetTiltObserver();

        double Pan { get; set; }

        double Tilt { get; set; }
    }

    public interface ISendsBrightness
    {
        IObservable<double> OutputBrightness { get; }
    }

    public interface ISendsColor
    {
        IObservable<Color> OutputColor { get; }
    }

    public interface ISendsStrobeSpeed
    {
        IObservable<double> OutputStrobeSpeed { get; }
    }

    public interface IControlToken : IDisposable
    {
        int Priority { get; }
    }

    public interface IOwnedDevice : IDevice
    {
        IControlToken TakeControl(int priority, bool executeReleaseAction = true, string name = "");

        bool HasControl(IControlToken checkOwner);

        bool IsOwned { get; }
    }

    public interface IOwner
    {
        string Name { get; }

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
