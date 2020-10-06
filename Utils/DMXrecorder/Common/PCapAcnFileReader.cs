using Haukcode.PcapngUtils;
using Haukcode.PcapngUtils.Pcap;
using Haukcode.sACN;
using Haukcode.sACN.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class PCapAcnFileReader : PCapFileReader, IFileReader
    {
        public PCapAcnFileReader(string fileName)
            : base(fileName)
        {
        }

        public DmxData ReadFrame()
        {
            var data = ReadPacket();

            var packet = SACNPacket.Parse(data.Payload);
            var framingLayer = packet?.RootLayer?.FramingLayer;
            var dmpLayer = framingLayer?.DMPLayer;
            if (dmpLayer == null)
                throw new InvalidDataException("Not a valid Streaming DMX ACN packet");

            byte[] dmxData;
            if (dmpLayer.StartCode == 0xff)
            {
                dmxData = new byte[dmpLayer.Data.Length - 1];
                Buffer.BlockCopy(dmpLayer.Data, 1, dmxData, 0, dmxData.Length);
            }
            else
                dmxData = dmpLayer.Data;

            return new DmxData
            {
                DataType = DmxData.DataTypes.FullFrame,
                Sequence = framingLayer.SequenceID,
                TimestampMS = data.Seconds * 1000 + (data.Microseconds / 1000.0),
                UniverseId = framingLayer.UniverseID,
                Data = dmxData
            };
        }
    }
}
