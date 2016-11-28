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

    public interface IOutputHardware
    {
    }

    public interface IInputHardware
    {
    }

    public interface IDmxOutput : IPort
    {
        SendStatus SendDimmerValue(int channel, byte value);
        SendStatus SendDimmerValues(int firstChannel, byte[] values);
        SendStatus SendDimmerValues(int firstChannel, byte[] values, int offset, int length);
    }

    public interface IPixelOutput : IPort
    {
        SendStatus SendPixelValue(int channel, PhysicalDevice.PixelRGBByte rgb);

        SendStatus SendPixelsValue(int channel, PhysicalDevice.PixelRGBByte[] rgb);

        void SendPixelsValue(int channel, byte[] rgb, int length);
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
