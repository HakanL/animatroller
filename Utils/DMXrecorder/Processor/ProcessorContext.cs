using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public class ProcessorContext
    {
        public string InputFilename { get; set; }

        public double FirstSyncTimestampMS { get; set; }

        //public int FullFramesBeforeFirstSync { get; set; }

        public bool HasSyncFrames { get; set; }
        
        public int TotalFrames { get; set; }
    }
}
