using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Animatroller.Common
{
    public class FseqFileReader : BaseFileReader
    {
        public class Header
        {
            public string Tag { get; set; }

            public int NumPeriods { get; set; }

            public int StepSize { get; set; }

            public int StepTimeMS { get; set; }

            public byte MinorVersion { get; set; }

            public byte MajorVersion { get; set; }

            public int FixedHeaderSize { get; set; }

            public int NumUniverses { get; set; }

            public int UniverseSize { get; set; }

            public byte Gamma { get; set; }

            public byte ColorEncoding { get; set; }

            public int ChannelDataOffset { get; set; }

            public string MediaFilename { get; set; }
        }

        private BinaryReader binRead;
        private Header header;
        private int currentFrame;
        private byte[] frameBuffer;
        private FileFormat.Networks config;
        private int currentNetwork;
        private int currentReadPosition;

        public FseqFileReader(string fileName, string configFileName)
            : base(fileName)
        {
            this.binRead = new System.IO.BinaryReader(this.fileStream);
            this.header = null;

            var serializer = new XmlSerializer(typeof(FileFormat.Networks));
            using (var fs = File.OpenRead(configFileName))
            {
                this.config = (FileFormat.Networks)serializer.Deserialize(fs);
            }

            if (this.config.Network.Length == 0)
                throw new ArgumentOutOfRangeException("Need at least a single network in the networks/config file");

            ReadHeader();
        }

        public override void Dispose()
        {
            this.binRead.Dispose();
            base.Dispose();
        }

        private int ReadInt16()
        {
            byte lsb = this.binRead.ReadByte();
            byte msb = this.binRead.ReadByte();

            return msb << 8 | lsb;
        }

        private int ReadInt32()
        {
            byte b1 = this.binRead.ReadByte();
            byte b2 = this.binRead.ReadByte();
            byte b3 = this.binRead.ReadByte();
            byte b4 = this.binRead.ReadByte();

            return b4 << 24 | b3 << 16 | b2 << 8 | b1;
        }

        private void ReadHeader()
        {
            this.header = new Header();

            this.header.Tag = System.Text.Encoding.ASCII.GetString(this.binRead.ReadBytes(4));

            if (this.header.Tag != "PSEQ" && this.header.Tag != "FSEQ")
                throw new ArgumentException($"Invalid/Unknown file type ({this.header.Tag})");

            this.header.ChannelDataOffset = ReadInt16();
            this.header.MinorVersion = this.binRead.ReadByte();
            this.header.MajorVersion = this.binRead.ReadByte();
            this.header.FixedHeaderSize = ReadInt16();
            this.header.StepSize = ReadInt32();
            this.header.NumPeriods = ReadInt32();
            this.header.StepTimeMS = ReadInt16();
            this.header.NumUniverses = ReadInt16();
            this.header.UniverseSize = ReadInt16();
            this.header.Gamma = this.binRead.ReadByte();
            this.header.ColorEncoding = this.binRead.ReadByte();
            int fill = ReadInt16();

            if (this.header.ChannelDataOffset > 28)
            {
                int mediaFilenameLength = ReadInt16();
                if (mediaFilenameLength > 0)
                {
                    // mf
                    ReadInt16();
                    this.header.MediaFilename = Encoding.UTF8.GetString(this.binRead.ReadBytes(mediaFilenameLength));
                }
            }

            this.frameBuffer = new byte[this.header.StepSize];

            int RefreshRateHz = 1000 / this.header.StepTimeMS;
            int DurationS = (int)((float)(Length - this.header.ChannelDataOffset) / ((float)this.header.StepSize * (float)RefreshRateHz));

            // Set starting position
            Position = this.header.ChannelDataOffset;
            this.currentFrame = -1;
        }

        private void ReadFullFrame()
        {
            this.frameBuffer = this.binRead.ReadBytes(this.header.StepSize);

            this.currentFrame++;
        }

        public override DmxData ReadFrame()
        {
            if (this.currentNetwork == 0)
            {
                // Read a full frame
                ReadFullFrame();

                this.currentReadPosition = 0;
            }

            FileFormat.NetworkNode network;
            while (true)
            {
                network = this.config.Network[this.currentNetwork];

                this.currentNetwork++;
                if (this.currentNetwork >= this.config.Network.Length)
                {
                    this.currentNetwork = 0;
                    break;
                }

                if (network.MaxChannels == 0)
                    // Skip this network
                    continue;

                break;
            }

            var dmxData = new DmxData
            {
                Data = new byte[network.MaxChannels],
                TimestampMS = (ulong)(this.currentFrame * this.header.StepTimeMS),
                Sequence = this.currentFrame
            };

            Buffer.BlockCopy(this.frameBuffer, this.currentReadPosition, dmxData.Data, 0, dmxData.Data.Length);
            this.currentReadPosition += dmxData.Data.Length;

            switch (network.NetworkType)
            {
                case "E131":
                    dmxData.DataType = DmxData.DataTypes.FullFrame;
                    dmxData.Universe = int.Parse(network.BaudRate);
                    break;

                default:
                    dmxData.DataType = DmxData.DataTypes.Nop;
                    break;
            }

            return dmxData;
        }
    }
}
