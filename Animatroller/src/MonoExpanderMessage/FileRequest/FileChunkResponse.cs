using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class FileChunkResponse
    {
        public string DownloadId { get; set; }

        public long ChunkStart { get; set; }

        public byte[] Chunk { get; set; }
    }
}
