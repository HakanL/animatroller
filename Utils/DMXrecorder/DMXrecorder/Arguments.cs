using System;
using System.Collections.Generic;
using PowerArgs;

namespace Animatroller.DMXrecorder
{
    public class Arguments
    {
        public enum InputTypes
        {
            sACN,
            ArtNet
        }

        public enum FileFormats
        {
            Csv,
            Binary,
            PCapAcn
        }

        [ArgShortcut("t")]
        [ArgDescription("Input type")]
        [ArgRequired()]
        public InputTypes InputType { get; set; }

        [ArgShortcut("o")]
        [ArgDescription("Output file")]
        [ArgRequired()]
        public string OutputFile { get; set; }

        [ArgShortcut("n")]
        [ArgDescription("Network adapter index")]
        public int NetworkAdapterIndex { get; set; }

        [ArgShortcut("u")]
        [ArgDescription("Universes (comma-separated)")]
        [ArgDefaultValue(1)]
        public ushort[] Universes { get; set; }

        [ArgShortcut("f")]
        [ArgDescription("File format")]
        [ArgDefaultValue(FileFormats.Binary)]
        public FileFormats FileFormat { get; set; }
    }
}
