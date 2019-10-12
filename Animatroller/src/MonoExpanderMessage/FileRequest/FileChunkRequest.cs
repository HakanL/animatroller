using System;
using System.Collections.Generic;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class FileChunkRequest
    {
        public string DownloadId { get; set; }

        public FileTypes Type { get; set; }

        public string FileName { get; set; }

        public long ChunkStart { get; set; }

        public int ChunkSize { get; set; }
    }
}
