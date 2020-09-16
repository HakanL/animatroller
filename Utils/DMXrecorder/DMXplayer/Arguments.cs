using System;
using System.Collections.Generic;
using PowerArgs;

namespace Animatroller.DMXplayer
{
    public class Arguments
    {
        public enum OutputTypes
        {
            sACN
        }

        public enum FileFormats
        {
            Binary,
            PCapAcn,
            PCapArtNet
        }

        [ArgShortcut("t")]
        [ArgDescription("Output type")]
        [ArgDefaultValue(OutputTypes.sACN)]
        public OutputTypes OutputType { get; set; }

        [ArgShortcut("i")]
        [ArgDescription("Input file")]
        [ArgRequired()]
        public string InputFile { get; set; }

        [ArgShortcut("l")]
        [ArgDescription("Loop # of times")]
        public int Loop { get; set; }

        [ArgShortcut("f")]
        [ArgDescription("File format")]
        [ArgDefaultValue(FileFormats.Binary)]
        public FileFormats FileFormat { get; set; }

        [ArgShortcut("n")]
        [ArgDescription("Network Adapter")]
        public string NetworkAdapter { get; set; }

        [ArgShortcut("bo")]
        [ArgDescription("Black Out at end")]
        public bool BlackOutAtEnd { get; set; }
    }
}
