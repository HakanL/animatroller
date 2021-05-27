using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Common;

namespace Animatroller.Processor.Command
{
    public class FileConvert : ICommand
    {
        private readonly Common.IInputReader fileReader;
        private readonly ITransformer transformer;

        public FileConvert(Common.IInputReader fileReader, ITransformer transformer)
        {
            this.fileReader = fileReader;
            this.transformer = transformer;
        }

        public void Execute(TransformContext context)
        {
            //double? timestampOffset = null;
            //bool firstFrame = true;
            //var readaheadQueue = new List<Common.DmxDataOutputPacket>();

            while (true)
            {
                var data = this.fileReader.ReadFrame2();
                if (data == null)
                    break;

                //if (!timestampOffset.HasValue)
                //    timestampOffset = data.TimestampMS;

                // Apply offset
                //data.TimestampMS -= timestampOffset.Value;

                //readaheadQueue.Add(data);

                //if (firstFrame && context.HasSyncFrames)
                //{
                //    // We need to add to the readahead queue to find the next sync
                //    if (data.Content is SyncFrame)
                //    {
                //        context.FirstSyncTimestampMS = data.TimestampMS;

                //        // Simulate the output so we can count the frames
                //        //context.FullFramesBeforeFirstSync = 0;
                //        foreach (var item in readaheadQueue)
                //        {
                //            if (item.Content is DmxDataFrame)
                //                this.transformer.Simulate(context, item, packet => context.FullFramesBeforeFirstSync++);
                //        }

                //        firstFrame = false;
                //    }
                //}
                //else
                //{
                    //foreach (var outputData in readaheadQueue)
                    //{
                        this.transformer.Transform2(context, data, this.fileReader.PeekFrame2());
                    //}

                    //readaheadQueue.Clear();
                //}
            }
        }
    }
}
