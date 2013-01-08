using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Framework.Expander
{
    public abstract class PacketManager
    {
        private MemoryStream buffer;
        private readonly byte[] headerPattern;
        private readonly byte[] trailerPattern;
        private bool foundStart;
        private int? contentSize;
        private int? contentEnd;
        private int? messageEnd;

        public class PacketReceivedEventArgs : EventArgs
        {
            public byte[] Buffer { get; private set; }
            public int ContentSize { get; private set; }
            public PacketReceivedEventArgs(byte[] buffer, int contentSize)
            {
                this.Buffer = buffer;
                this.ContentSize = contentSize;
            }
        }

        public event EventHandler<PacketReceivedEventArgs> PacketReceived;

        public PacketManager(byte[] headerPattern, byte[] trailerPattern)
        {
            this.buffer = new MemoryStream();
            this.headerPattern = headerPattern ?? new byte[0];
            this.trailerPattern = trailerPattern ?? new byte[0];
        }

        protected abstract int? GetContentSize(byte[] buf, int size);

        private void KillBufferStart(int bytesToKill)
        {
            if (bytesToKill == 0)
                return;

            this.buffer.Position = bytesToKill;
            var tempBuffer = new MemoryStream((int)(this.buffer.Length - bytesToKill));
            this.buffer.CopyTo(tempBuffer);
            this.buffer.Close();
            this.buffer.Dispose();
            this.buffer = tempBuffer;
        }

        public void WriteNewData(byte[] buf)
        {
            if (buf == null || buf.Length == 0)
                return;

            this.buffer.Write(buf, 0, buf.Length);

            if (!foundStart)
            {
                // Look for start
                if (this.headerPattern.Length == 0)
                    this.foundStart = true;
                else
                {
                    // Look for header pattern
                    int startPattern = this.buffer.GetBuffer().Locate(this.headerPattern, (int)this.buffer.Length, 0);
                    if (startPattern > -1)
                    {
                        this.foundStart = true;
                        KillBufferStart(startPattern + this.headerPattern.Length);
                    }
                }
            }

            if (foundStart && !this.contentSize.HasValue && this.buffer.Length > 0)
            {
                // Check for size
                this.contentSize = GetContentSize(this.buffer.GetBuffer(), (int)this.buffer.Length);
            }

            if (this.contentSize.HasValue && !this.contentEnd.HasValue && this.buffer.Length >= this.contentSize.Value)
            {
                this.contentEnd = this.contentSize.Value;
            }

            if (this.contentEnd.HasValue)
            {
                if (this.trailerPattern.Length == 0)
                {
                    this.messageEnd = (int)this.buffer.Length;
                }
                else
                {
                    var endPattern = this.buffer.GetBuffer().Locate(this.trailerPattern, (int)this.buffer.Length, this.contentEnd.Value);
                    if (endPattern > -1)
                    {
                        this.messageEnd = endPattern + this.trailerPattern.Length;
                    }
                }
            }

            if (this.messageEnd.HasValue)
            {
                RaisePacketReceived(this.buffer.GetBuffer(), this.contentSize.Value);

                KillBufferStart(this.messageEnd.Value);

                Reset();
            }
        }

        protected virtual void Reset()
        {
            this.foundStart = false;
            this.contentSize = null;
            this.contentEnd = null;
            this.messageEnd = null;
        }

        protected void RaisePacketReceived(byte[] packetData, int size)
        {
            var handler = PacketReceived;
            if (handler != null)
                handler(this, new PacketReceivedEventArgs(packetData, size));
        }
    }


    internal static class ByteArrayRocks
    {

        public static int Locate(this byte[] self, byte[] candidate, int offset = 0)
        {
            return Locate(self, candidate, self.Length, offset);
        }

        public static int Locate(this byte[] self, byte[] candidate, int size, int offset = 0)
        {
            if (size > self.Length)
                throw new ArgumentOutOfRangeException("Size is larger than self.Length");

            if (IsEmptyLocate(self, candidate, offset, size))
                return -1;

            for (int i = offset; i < size; i++)
            {
                if (!IsMatch(self, i, candidate, size))
                    continue;

                return i;
            }

            return -1;
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate, int size)
        {
            if (candidate.Length > (size - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate, int offset, int size)
        {
            return array == null
                || candidate == null
                || size == 0
                || candidate.Length == 0
                || candidate.Length > (size + offset);
        }
    }
}
