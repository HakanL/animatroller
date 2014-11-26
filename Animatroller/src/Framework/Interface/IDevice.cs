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
    public interface IReceivesBrightness : IOwnedDevice
    {
        Animatroller.Framework.LogicalDevice.ControlledObserver<double> GetBrightnessObserver(IControlToken controlToken);
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
        bool HasControl { get; }

        int Priority { get; }
    }

    public interface IOwnedDevice : IDevice
    {
        IControlToken TakeControl(int priority, string name = "");

        bool HasControl(IControlToken checkOwner);
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
        void StartDevice();
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
