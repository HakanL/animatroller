using System;
using System.Collections.Generic;
using PowerArgs;

namespace Animatroller.sACNrecorder
{
    public class Arguments
    {
        [ArgShortcut("o")]
        [ArgDescription("Output file")]
        [ArgRequired()]
        public string OutputFile { get; set; }

        [ArgShortcut("u")]
        [ArgDescription("Universes (comma-separated)")]
        [ArgDefaultValue(1)]
        public int[] Universes { get; set; }

        [ArgShortcut("f")]
        [ArgDescription("File format")]
        [ArgDefaultValue(DataWriter.FileFormats.Csv)]
        public DataWriter.FileFormats FileFormat { get; set; }
    }
}
