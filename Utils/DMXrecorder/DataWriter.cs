using System;
using System.Linq;
using System.IO;

namespace Animatroller.DMXrecorder
{
    public class DataWriter : IDisposable
    {
        public enum FileFormats
        {
            Csv
        }

        private StreamWriter fileStream;
        private FileFormats fileFormat;

        public DataWriter(string fileName, FileFormats fileFormat)
        {
            this.fileStream = File.CreateText(fileName);
            this.fileFormat = fileFormat;
        }

        public void Dispose()
        {
            this.fileStream.Flush();
            this.fileStream.Close();
            this.fileStream.Dispose();

            this.fileStream = null;
        }

        public void Header(int universeId)
        {
            switch (this.fileFormat)
            {
                case FileFormats.Csv:
                    break;

                default:
                    throw new ArgumentException("Unknown file format");
            }
        }

        public void Output(DmxData dmxData)
        {
            switch (this.fileFormat)
            {
                case FileFormats.Csv:
                    OutputCsv(dmxData);
                    break;

                default:
                    throw new ArgumentException("Unknown file format");
            }
        }

        public void Footer(int universeId)
        {
            switch (this.fileFormat)
            {
                case FileFormats.Csv:
                    break;

                default:
                    throw new ArgumentException("Unknown file format");
            }
        }

        private void OutputCsv(DmxData dmxData)
        {
            switch (dmxData.DataType)
            {
                case DmxData.DataTypes.NoChange:
                    this.fileStream.WriteLine("{0},{1},NoChange", dmxData.Sequence, dmxData.Universe);
                    break;

                case DmxData.DataTypes.FullFrame:
                    this.fileStream.WriteLine("{0},{1},Full,{2}", dmxData.Sequence, dmxData.Universe, string.Join(",", dmxData.Data.Select(x => x.ToString())));
                    break;
            }
        }
    }
}
