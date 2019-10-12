using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public class FileResponse
    {
        public string DownloadId { get; set; }

        public long Size { get; set; }

        public byte[] SignatureSha1 { get; set; }
    }
}
