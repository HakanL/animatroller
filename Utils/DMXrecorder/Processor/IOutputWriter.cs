using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface IOutputWriter
    {
        void Output(ProcessorContext context, Common.OutputFrame outputFrame, Action<Common.BaseDmxFrame> action = null);

        void Simulate(ProcessorContext context, Common.DmxDataPacket dmxData, Action<Common.BaseDmxFrame> action);
    }
}
