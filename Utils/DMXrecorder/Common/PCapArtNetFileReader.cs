﻿using Haukcode.ArtNet.Packets;
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
    public class PCapArtNetFileReader : PCapFileReader, IFileReader
    {
        private long sequence;

        public PCapArtNetFileReader(string fileName)
            : base(fileName)
        {
        }

        public DmxData ReadFrame()
        {
            var data = ReadPacket();

            var artNetData = new Haukcode.ArtNet.IO.ArtNetReceiveData
            {
                buffer = data.Payload,
                DataLength = data.Payload.Length
            };
            var packet = ArtNetPacket.Create(artNetData, null);

            if (packet is ArtNetDmxPacket dmxPacket)
            {
                return new DmxData
                {
                    DataType = DmxData.DataTypes.FullFrame,
                    Sequence = ++this.sequence,
                    TimestampMS = data.Seconds * 1000 + (ulong)(data.Microseconds / 1000),
                    Universe = dmxPacket.Universe,
                    Data = dmxPacket.DmxData
                };
            }
            else
                throw new ArgumentException("Unhandled packet type");
        }
    }
}
