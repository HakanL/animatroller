using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public interface IPort
    {
    }

    public interface IDmxOutput : IPort
    {
        SendStatus SendDimmerValue(int channel, byte value);
        SendStatus SendDimmerValues(int firstChannel, params byte[] values);
    }

    public interface IPixelOutput : IPort
    {
        SendStatus SendPixelValue(int channel, byte r, byte g, byte b);
        SendStatus SendPixelsValue(int channel, byte[] rgb);
    }

    public interface IDigitalInput : IPort
    {
        Action<bool> Trigger();
    }

    public interface IMotorOutput : IPort
    {
        SendStatus SendMotorSpeed(int channel, double speed);
    }
}
