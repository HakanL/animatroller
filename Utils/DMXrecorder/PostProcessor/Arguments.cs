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
            TrimEnd,
            FileConvert
        }

        public enum FileFormats
        {
            Binary,
            PCapAcn,
            FSeq
        }

        [ArgShortcut("i")]
        [ArgDescription("Input file")]
        [ArgRequired()]
        [ArgExistingFile()]
        public string InputFile { get; set; }

        [ArgShortcut("ic")]
        [ArgDescription("Input config file")]
        [ArgExistingFile()]
        public string InputConfigFile { get; set; }

        [ArgShortcut("o")]
        [ArgDescription("Output file")]
        public string OutputFile { get; set; }

        [ArgShortcut("c")]
        [ArgDescription("Command")]
        [ArgRequired()]
        public Commands Command { get; set; }

        [ArgShortcut("if")]
        [ArgDescription("Input File format")]
        [ArgDefaultValue(FileFormats.Binary)]
        public FileFormats InputFileFormat { get; set; }

        [ArgShortcut("tp")]
        [ArgDescription("Trim position")]
        public long TrimPos { get; set; }

        [ArgShortcut("of")]
        [ArgDescription("Output File format")]
        [ArgDefaultValue(FileFormats.Binary)]
        public FileFormats OutputFileFormat { get; set; }
    }
}
