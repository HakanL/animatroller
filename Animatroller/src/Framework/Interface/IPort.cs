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
        void SendDmxData(int address, byte value);
        void SendDmxData(int startAddress, byte[] values);
        void SendDmxData(int startAddress, byte[] values, int offset, int length);
    }

    public interface IPixelOutput : IPort
    {
        void SendPixelValue(int pixelPos, PhysicalDevice.PixelRGBByte rgb);

        void SendPixelsValue(int startPixelPos, PhysicalDevice.PixelRGBByte[] rgb);

        void SendPixelDmxData(int startPixelPos, byte[] dmxData, int length);

        void SendMultiUniverseDmxData(byte[][] dmxData);
    }

    public interface IDigitalInput : IPort
    {
        Action<bool> Trigger();
    }

    public interface IMotorOutput : IPort
    {
        void SendMotorSpeed(IChannel channel, double speed);
    }
}
