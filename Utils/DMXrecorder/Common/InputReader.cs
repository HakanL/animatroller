using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Common
{
    public class InputReader : IInputReader
    {
        private readonly IO.IFileReader reader;
        private readonly List<DmxDataOutputPacket> readPackets = new List<DmxDataOutputPacket>();
        private int readPosition = 0;

        public InputReader(IO.IFileReader reader)
        {
            this.reader = reader;
        }

        public DmxDataOutputPacket ReadFrame()
        {
            if (this.readPosition < this.readPackets.Count)
                return this.readPackets[this.readPosition++];

            do
            {
                if (!this.reader.DataAvailable)
                    break;

                var frame = this.reader.ReadFrame();
                if (frame == null)
                    break;
                if (frame.Content == null)
                    continue;

                this.readPackets.Add(frame);
                this.readPosition++;
                return frame;
            } while (this.reader.DataAvailable);

            return null;
        }

        public void Rewind()
        {
            this.readPosition = 0;
        }

        public int FramesRead => this.readPackets.Count;

        public bool DataAvailable => throw new NotImplementedException();
    }
}
