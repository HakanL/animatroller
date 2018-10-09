using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Animatroller.Framework.Import
{
    public class FseqFileReader : BaseFileReader, IFileReader3
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

        public struct NetworkData
        {
            public int MaxChannels { get; set; }

            public int NumUniverses { get; set; }

            public int UniverseId { get; set; }

            public bool Skip { get; set; }
        }

        private BinaryReader binRead;
        private Header header;
        private int currentFrame;
        private byte[] frameBuffer;
        private FileFormat.Networks config;
        private int currentNetwork;
        private int currentReadPosition;
        private Dictionary<int, NetworkData> networks;
        private int triggerUniverseId;
        private int maxNetwork;

        public int TriggerUniverseId => this.triggerUniverseId;

        public int FrameSize => this.header.StepSize;

        public FseqFileReader(string fileName, string configFileName = null)
            : base(fileName)
        {
            this.binRead = new System.IO.BinaryReader(this.fileStream);
            this.header = null;

            if (string.IsNullOrEmpty(configFileName))
                configFileName = Path.Combine(Path.GetDirectoryName(fileName), "xlights_networks.xml");
            else
            {
                if (!Path.IsPathRooted(configFileName))
                    configFileName = Path.Combine(Path.GetDirectoryName(fileName), configFileName);
            }

            var serializer = new XmlSerializer(typeof(FileFormat.Networks));
            using (var fs = File.OpenRead(configFileName))
            {
                this.config = (FileFormat.Networks)serializer.Deserialize(fs);
            }

            if (this.config.Network.Length == 0)
                throw new ArgumentOutOfRangeException("Need at least a single network in the networks/config file");

            this.networks = new Dictionary<int, NetworkData>();

            for (int i = 0; i < this.config.Network.Length; i++)
            {
                var fseqNetwork = this.config.Network[i];
                if (fseqNetwork.MaxChannels <= 0)
                    continue;

                var networkData = new NetworkData
                {
                    MaxChannels = fseqNetwork.MaxChannels,
                    UniverseId = int.Parse(fseqNetwork.BaudRate),
                    Skip = fseqNetwork.NetworkType != "E131",
                    NumUniverses = string.IsNullOrEmpty(fseqNetwork.NumUniverses) ? 1 : int.Parse(fseqNetwork.NumUniverses)
                };
                this.networks[i] = networkData;

                this.maxNetwork = i + 1;
                this.triggerUniverseId = networkData.UniverseId;
            }

            ReadHeader();
        }

        public override void Dispose()
        {
            this.binRead?.Dispose();
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

        public override void Rewind()
        {
            Position = this.header.ChannelDataOffset;
            this.currentFrame = -1;
            this.currentNetwork = 0;
        }

        public byte[] ReadFullFrame(out long timestampMS)
        {
            int readBytes = this.binRead.Read(this.frameBuffer, 0, this.header.StepSize);

            if (readBytes == 0)
            {
                timestampMS = -1;
                return null;
            }

            this.currentFrame++;

            timestampMS = this.currentFrame * this.header.StepTimeMS;

            return this.frameBuffer;
        }

        public (int UniverseId, int FSeqChannel)[] GetFrameLayout()
        {
            var layout = new(int, int)[this.header.StepSize];

            int writePos = -1;
            foreach (var network in this.networks.Values)
            {
                for (int u = 0; u < network.NumUniverses; u++)
                {
                    for (int i = 0; i < network.MaxChannels; i++)
                    {
                        layout[++writePos] = (network.Skip ? -1 : network.UniverseId + u, i);
                    }
                }
            }

            return layout;
        }

        public override DmxData ReadFrame()
        {
            if (this.currentNetwork == 0)
            {
                // Read a full frame
                int readBytes = this.binRead.Read(this.frameBuffer, 0, this.header.StepSize);

                if (readBytes == 0)
                    return null;

                this.currentFrame++;

                this.currentReadPosition = 0;
            }

            NetworkData network;
            while (true)
            {
                network = this.networks[this.currentNetwork];

                this.currentNetwork++;
                if (this.currentNetwork >= this.maxNetwork)
                {
                    this.currentNetwork = 0;
                    break;
                }

                break;
            }

            var dmxData = new DmxData
            {
                Data = new byte[network.MaxChannels],
                TimestampMS = (ulong)(this.currentFrame * this.header.StepTimeMS),
                Sequence = this.currentFrame,
                Universe = network.UniverseId
            };

            Buffer.BlockCopy(this.frameBuffer, this.currentReadPosition, dmxData.Data, 0, dmxData.Data.Length);
            this.currentReadPosition += dmxData.Data.Length;

            if (network.Skip)
                dmxData.DataType = network.Skip ? DmxData.DataTypes.Nop : DmxData.DataTypes.FullFrame;

            return dmxData;
        }
    }
}
