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
            TrimBlackFindLoop,
            TrimFrame,
            TrimTime,
            Convert,
            GenerateStatic,
            GenerateRamp,
            GenerateSaw,
            GenerateRainbow,
            Duplicate
        }

        [ArgShortcut("i")]
        [ArgDescription("Input file")]
        [ArgExistingFile()]
        public string InputFilename { get; set; }

        [ArgShortcut("ic")]
        [ArgDescription("Input config file")]
        [ArgExistingFile()]
        public string InputConfigFile { get; set; }

        [ArgShortcut("o")]
        [ArgDescription("Output file")]
        public string OutputFilename { get; set; }

        [ArgShortcut("c")]
        [ArgDescription("Command")]
        [ArgRequired()]
        public Commands Command { get; set; }

        [ArgShortcut("if")]
        [ArgDescription("Input File format")]
        public Common.FileFormats? InputFileFormat { get; set; }

        [ArgDefaultValue(true)]
        [ArgDescription("Set to True if TrimBlack should leave the first frame black")]
        public bool FirstFrameBlack { get; set; }

        [ArgShortcut("ts")]
        [ArgDescription("Trim start position")]
        public double? TrimStart { get; set; }

        [ArgShortcut("te")]
        [ArgDescription("Trim end position")]
        public double? TrimEnd { get; set; }

        [ArgShortcut("td")]
        [ArgDescription("Trim duration/count")]
        public double? TrimDuration { get; set; }

        [ArgShortcut("fb")]
        [ArgDescription("Fill byte")]
        public byte FillByte { get; set; }

        [ArgShortcut("of")]
        [ArgDescription("Output File format")]
        public Common.FileFormats? OutputFileFormat { get; set; }

        [ArgShortcut("m")]
        [ArgDescription("Universe Mapping (input=output,input2=output2 - example 1=10,2=11,6=20)")]
        public string UniverseMapping { get; set; }

        [ArgShortcut("u")]
        [ArgDescription("Universes")]
        public string Universes { get; set; }

        [ArgShortcut("hz")]
        [ArgDefaultValue(40)]
        [ArgDescription("Frequency")]
        public double Frequency { get; set; }

        [ArgShortcut("e")]
        [ArgDescription("Enhancers, example -e BrightnessFixer,TimestampFixer")]
        public string Enhancers { get; set; }

        [ArgShortcut("l")]
        [ArgDescription("Loop")]
        public int? Loop { get; set; }
    }
}
