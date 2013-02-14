using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public interface IOwner
    {
        string Name { get; }
        int Priority { get; }
    }

    public interface IDevice
    {
        void StartDevice();
    }

    public interface ILogicalDevice : IDevice
    {
        string Name { get; }
    }

    public interface IPhysicalDevice : IDevice
    {
    }

    public interface IControlledDevice : ILogicalDevice
    {
        void Suspend();
        void Resume();
    }
}
