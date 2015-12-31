using System;
using System.Collections.Generic;
using PowerArgs;

namespace Animatroller.PostProcessor
{
    public class Arguments
    {
        public enum Commands
        {
            TrimBlack,
            FindLoop,
            TrimEnd
        }

        public enum FileFormats
        {
            Binary
        }

        [ArgShortcut("i")]
        [ArgDescription("Input file")]
        [ArgRequired()]
        [ArgExistingFile()]
        public string Inputfile { get; set; }

        [ArgShortcut("o")]
        [ArgDescription("Output file")]
        public string OutputFile { get; set; }

        [ArgShortcut("c")]
        [ArgDescription("Command")]
        [ArgRequired()]
        public Commands Command { get; set; }

        [ArgShortcut("f")]
        [ArgDescription("File format")]
        [ArgDefaultValue(FileFormats.Binary)]
        public FileFormats FileFormat { get; set; }

        [ArgShortcut("tp")]
        [ArgDescription("Trim position")]
        public long TrimPos { get; set; }
    }
}
