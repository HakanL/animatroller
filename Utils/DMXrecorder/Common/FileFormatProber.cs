using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Animatroller.Common
{
    public static class FileFormatProber
    {
        public static FileFormats? ProbeFile(string filename)
        {
            // Try to determine the format by probing
            try
            {
                using var testReader = new Common.IO.PCapAcnFileReader(filename);
                testReader.ReadFrame();

                return Common.FileFormats.PCapAcn;
            }
            catch (InvalidDataException)
            {
            }

            // Try to determine the format by attempting to read
            try
            {
                using var testReader = new Common.IO.PCapArtNetFileReader(filename);
                testReader.ReadFrame();

                return Common.FileFormats.PCapArtNet;
            }
            catch (InvalidDataException)
            {
            }

            try
            {
                using var testReader = new Common.IO.FseqFileReader(filename);
                testReader.ReadFrame();

                return Common.FileFormats.FSeq;
            }
            catch (InvalidDataException)
            {
            }

            return null;
        }
    }
}
