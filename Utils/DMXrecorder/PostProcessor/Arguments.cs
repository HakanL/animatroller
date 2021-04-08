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
            Trim,
            FileConvert
        }

        public enum FileFormats
        {
            Binary,
            PCapAcn,
            FSeq,
            PCapArtNet
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

        [ArgShortcut("ts")]
        [ArgDescription("Trim start position")]
        public long? TrimStart { get; set; }

        [ArgShortcut("te")]
        [ArgDescription("Trim end position")]
        public long? TrimEnd { get; set; }

        [ArgShortcut("tc")]
        [ArgDescription("Trim count")]
        public long? TrimCount { get; set; }

        [ArgShortcut("of")]
        [ArgDescription("Output File format")]
        [ArgDefaultValue(FileFormats.Binary)]
        public FileFormats OutputFileFormat { get; set; }

        [ArgShortcut("m")]
        [ArgDescription("Universe Mapping (input=output,input2=output2 - example 1=10,2=11,6=20)")]
        public string UniverseMapping { get; set; }

        [ArgShortcut("e")]
        [ArgDescription("Enhancers, example -e BrightnessFixer,TimestampFixer")]
        public string Enhancers { get; set; }

        [ArgShortcut("l")]
        [ArgDescription("Loop")]
        public int? Loop { get; set; }
    }
}
