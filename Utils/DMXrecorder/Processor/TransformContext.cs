using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public class TransformContext
    {
        public double FirstSyncTimestampMS { get; set; }

        //public int FullFramesBeforeFirstSync { get; set; }

        public bool HasSyncFrames { get; set; }

    }
}
