using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.PhysicalDevice
{
    public interface IOutputDevice : IRunningDevice
    {
    }


    public interface INeedsDmxOutput
    {
        IDmxOutput DmxOutputPort { set; }
    }

    public interface INeedsPixelOutput
    {
        IPixelOutput PixelOutputPort { set; }
    }

    public interface INeedsPixel2DOutput
    {
        IPixelOutput PixelOutputPort { set; }
    }

    //    public interface INeedsDigitalOutput
    //    {
    ////        IDigitalOutput DigitalOutputPort { set; }
    //    }

    //public interface INeedsMotorFeedbackOutput
    //{
    //    IMotorFeedbackOutput MotorFeedbackOutput { set; }
    //}

    public interface INeedsDigitalInput
    {
    }
}
