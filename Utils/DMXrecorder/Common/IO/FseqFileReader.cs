using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Haukcode.sACN.Model;
using static Animatroller.Common.IO.FseqFileReader;

namespace Animatroller.Common.IO
{
    public class FseqFileReader : BaseFileReader
    {
        public abstract class BaseHeader
        {
            public string Tag { get; set; }

            public byte MinorVersion { get; set; }

            public byte MajorVersion { get; set; }

            public int FixedHeaderSize { get; set; }

            public int ChannelDataOffset { get; set; }

            public int StepTimeMS { get; set; }
        }

        public class Header1 : BaseHeader
        {
            public int NumPeriods { get; set; }

            public int StepSize { get; set; }

            public int NumUniverses { get; set; }

            public int UniverseSize { get; set; }

            public byte Gamma { get; set; }

            public byte ColorEncoding { get; set; }

            public string MediaFilename { get; set; }
        }

        public class Header2 : BaseHeader
        {
            public int ChannelCount { get; set; }

            public int FrameCount { get; set; }

            public byte Flags { get; set; }

            public byte CompressionType { get; set; }

            public int CompressionBlockCount { get; set; }

            public byte SparseRangeCount { get; set; }

            public byte Reserved { get; set; }

            public long UniqueId { get; set; }

            public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

            public List<(int FirstFrameNumber, int Length)> CompressionBlocks = new List<(int FirstFrameNumber, int Length)>();

            public List<(int StartChannel, int EndChannelOffset)> SparseRanges = new List<(int StartChannel, int EndChannelOffset)>();
        }

        private BinaryReader binRead;
        private BaseHeader header;
        private int currentFrame;
        private byte[] frameBuffer;
        private FileFormat.Networks config;
        private FileFormat.NetworkNode[] networks;
        private int currentNetwork;
        private int currentReadPosition;
        private bool emittedSync;
        private byte syncSequenceId;
        private string decompressedFilename;

        public FseqFileReader(string fileName, string configFileName = null)
            : base(fileName)
        {
            this.binRead = new System.IO.BinaryReader(this.fileStream);
            this.header = null;

            if (string.IsNullOrEmpty(configFileName))
                configFileName = Path.Combine(Path.GetDirectoryName(fileName), "xlights_networks.xml");

            var serializer = new XmlSerializer(typeof(FileFormat.Networks));
            using (var fs = File.OpenRead(configFileName))
            {
                this.config = (FileFormat.Networks)serializer.Deserialize(fs);
            }

            if (this.config.Network != null)
            {
                this.networks = this.config.Network;
            }
            else if (this.config.Controller != null)
            {
                var list = new List<FileFormat.NetworkNode>();
                foreach (var controller in this.config.Controller)
                {
                    list.AddRange(controller.Network);
                }
                this.networks = list.ToArray();
            }

            if (this.networks == null || this.networks.Length == 0)
                throw new ArgumentOutOfRangeException("Need at least a single network in the networks/config file");

            ReadHeader();
        }

        public override void Dispose()
        {
            this.binRead.Dispose();
            base.Dispose();

            if (this.decompressedFilename != null)
                File.Delete(this.decompressedFilename);
        }

        private int ReadInt16()
        {
            byte lsb = this.binRead.ReadByte();
            byte msb = this.binRead.ReadByte();

            return msb << 8 | lsb;
        }

        private int ReadInt24()
        {
            byte b1 = this.binRead.ReadByte();
            byte b2 = this.binRead.ReadByte();
            byte b3 = this.binRead.ReadByte();

            return b3 << 16 | b2 << 8 | b1;
        }

        private int ReadInt32()
        {
            byte b1 = this.binRead.ReadByte();
            byte b2 = this.binRead.ReadByte();
            byte b3 = this.binRead.ReadByte();
            byte b4 = this.binRead.ReadByte();

            return b4 << 24 | b3 << 16 | b2 << 8 | b1;
        }

        private long ReadInt64()
        {
            byte b1 = this.binRead.ReadByte();
            byte b2 = this.binRead.ReadByte();
            byte b3 = this.binRead.ReadByte();
            byte b4 = this.binRead.ReadByte();
            byte b5 = this.binRead.ReadByte();
            byte b6 = this.binRead.ReadByte();
            byte b7 = this.binRead.ReadByte();
            byte b8 = this.binRead.ReadByte();

            return b8 << 56 | b7 << 48 | b6 << 40 | b5 << 32 | b4 << 24 | b3 << 16 | b2 << 8 | b1;
        }

        private void ReadHeader()
        {
            string headerTag = System.Text.Encoding.ASCII.GetString(this.binRead.ReadBytes(4));

            if (headerTag != "PSEQ" && headerTag != "FSEQ")
                throw new ArgumentException($"Invalid/Unknown file type ({this.header.Tag})");

            int channelDataOffset = ReadInt16();
            byte minorVersion = this.binRead.ReadByte();
            byte majorVersion = this.binRead.ReadByte();
            int fixedHeaderSize = ReadInt16();

            if (majorVersion == 2)
            {
                // Read as v2
                var header = new Header2
                {
                    ChannelDataOffset = channelDataOffset,
                    Tag = headerTag,
                    MajorVersion = majorVersion,
                    MinorVersion = minorVersion,
                    FixedHeaderSize = fixedHeaderSize
                };

                header.ChannelCount = ReadInt32();
                header.FrameCount = ReadInt32();
                header.StepTimeMS = this.binRead.ReadByte();
                header.Flags = this.binRead.ReadByte();
                byte compressionFlags = this.binRead.ReadByte();
                header.CompressionType = (byte)(compressionFlags & 0x0F);
                header.CompressionBlockCount = ((compressionFlags & 0xF0) << 4) + this.binRead.ReadByte();
                header.SparseRangeCount = this.binRead.ReadByte();
                header.Reserved = this.binRead.ReadByte();
                header.UniqueId = ReadInt64();

                this.header = header;

                for (int i = 0; i < header.CompressionBlockCount; i++)
                {
                    int firstFrameNumber = ReadInt32();
                    int length = ReadInt32();

                    if (firstFrameNumber == 0 && length == 0)
                        continue;

                    header.CompressionBlocks.Add((firstFrameNumber, length));
                }

                for (int i = 0; i < header.SparseRangeCount; i++)
                {
                    int startChannel = ReadInt24();
                    int endChannelOffset = ReadInt24();

                    header.SparseRanges.Add((startChannel, endChannelOffset));

                    // Sparse Range is not implemented
                    throw new NotImplementedException();
                }

                // Read variables
                while ((Position + 4) < header.ChannelDataOffset)
                {
                    int length = ReadInt16();
                    string code = $"{(char)this.binRead.ReadByte()}{(char)this.binRead.ReadByte()}";
                    byte[] data;
                    string dataString;
                    if (length > 4)
                    {
                        data = this.binRead.ReadBytes(length - 4);
                        dataString = Encoding.UTF8.GetString(data).Trim('\0');
                    }
                    else
                    {
                        dataString = null;
                    }

                    header.Variables.Add(code, dataString);
                }

                if (header.CompressionType == 0)
                {
                    // Uncompressed

                    // Set starting position
                    Position = this.header.ChannelDataOffset;
                }
                else if (header.CompressionType == 1)
                {
                    // Decompress the whole file into a temporary file
                    Position = this.header.ChannelDataOffset;

                    this.decompressedFilename = Path.GetTempFileName();
                    var decompressedFile = File.Create(this.decompressedFilename);

                    var decompressor = new ZstdSharp.Decompressor();
                    foreach (var block in header.CompressionBlocks)
                    {
                        byte[] bytes = this.binRead.ReadBytes(block.Length);
                        var output = decompressor.Unwrap(bytes);
                        decompressedFile.Write(output);
                    }

                    decompressedFile.Flush();
                    decompressedFile.Dispose();

                    this.fileStream = File.OpenRead(this.decompressedFilename);
                }
                else
                    throw new NotImplementedException();

                this.frameBuffer = new byte[header.ChannelCount];
            }
            else
            {
                // Read as v1
                var header = new Header1
                {
                    ChannelDataOffset = channelDataOffset,
                    Tag = headerTag,
                    MajorVersion = majorVersion,
                    MinorVersion = minorVersion,
                    FixedHeaderSize = fixedHeaderSize
                };

                header.StepSize = ReadInt32();
                header.NumPeriods = ReadInt32();
                header.StepTimeMS = ReadInt16();
                header.NumUniverses = ReadInt16();
                header.UniverseSize = ReadInt16();
                header.Gamma = this.binRead.ReadByte();
                header.ColorEncoding = this.binRead.ReadByte();
                int fill = ReadInt16();

                if (header.ChannelDataOffset > 28)
                {
                    int mediaFilenameLength = ReadInt16();
                    if (mediaFilenameLength > 0)
                    {
                        // mf
                        ReadInt16();
                        header.MediaFilename = Encoding.UTF8.GetString(this.binRead.ReadBytes(mediaFilenameLength));
                    }
                }

                int RefreshRateHz = 1000 / header.StepTimeMS;
                int DurationS = (int)((float)(Length - header.ChannelDataOffset) / ((float)header.StepSize * (float)RefreshRateHz));

                this.header = header;
                this.frameBuffer = new byte[header.StepSize];

                // Set starting position
                Position = this.header.ChannelDataOffset;
            }

            this.currentFrame = -1;
        }

        private void ReadFullFrame()
        {
            int readBytes = this.fileStream.Read(this.frameBuffer, 0, this.frameBuffer.Length);
            if (readBytes != this.frameBuffer.Length)
                throw new InvalidDataException("Corrupt compressed data");

            this.currentFrame++;
        }

        public override DmxDataOutputPacket ReadFrame()
        {
            double timestampMS;

            if (this.currentNetwork == 0)
            {
                // See if we should emit a sync first
                if (this.currentFrame > -1 && !this.emittedSync)
                {
                    // Emit sync
                    this.emittedSync = true;

                    timestampMS = this.currentFrame * this.header.StepTimeMS;
                    return DmxDataOutputPacket.CreateSync(timestampMS, ++this.syncSequenceId, 1, null);
                }

                // Read a full frame
                ReadFullFrame();
                this.currentReadPosition = 0;
            }

            timestampMS = this.currentFrame * this.header.StepTimeMS;

            FileFormat.NetworkNode network;
            while (true)
            {
                network = this.networks[this.currentNetwork];

                this.currentNetwork++;
                if (this.currentNetwork >= this.networks.Length)
                {
                    this.currentNetwork = 0;
                    break;
                }

                if (network.MaxChannels == 0)
                    // Skip this network
                    continue;

                break;
            }

            // Simulated sync address
            var dmxData = new DmxDataFrame
            {
                Data = new byte[network.MaxChannels],
                SyncAddress = 1,
                Destination = System.Net.IPAddress.Parse(network.ComPort)
            };

            var packet = new DmxDataOutputPacket
            {
                Content = dmxData,
                Sequence = this.currentFrame,
                TimestampMS = timestampMS
            };

            Buffer.BlockCopy(this.frameBuffer, this.currentReadPosition, dmxData.Data, 0, dmxData.Data.Length);
            this.currentReadPosition += dmxData.Data.Length;

            switch (network.NetworkType)
            {
                case "E131":
                    dmxData.UniverseId = int.Parse(network.BaudRate);
                    break;

                default:
                    return null;
            }

            this.emittedSync = false;

            return packet;
        }
    }
}
