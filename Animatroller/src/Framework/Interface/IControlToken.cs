using System;
using System.Collections.Generic;
using System.Drawing;

namespace Animatroller.Framework
{
    public interface IControlToken : IDisposable
    {
        IData GetDataForDevice(IOwnedDevice device);

        int Priority { get; }

        void PushData(DataElements dataElement, object value);

        bool IsOwner(IControlToken checkToken);
    }
}
