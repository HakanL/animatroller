using System;

namespace Animatroller.Framework.MonoExpanderMessages
{
//
    // File requests
    //

    public class FileRequest
    {
        public string DownloadId { get; set; }

        public FileTypes Type { get; set; }

        public string FileName { get; set; }
    }
}
