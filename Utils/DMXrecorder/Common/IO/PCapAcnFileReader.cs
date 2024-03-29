﻿using Haukcode.PcapngUtils;
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

namespace Animatroller.Common.IO
{
    public class PCapAcnFileReader : PCapFileReader, IFileReader
    {
        private double? timestampOffsetMs = null;

        public PCapAcnFileReader(string fileName)
            : base(fileName)
        {
        }

        public DmxDataOutputPacket ReadFrame()
        {
            double timestampMs;
            SACNPacket packet;
            System.Net.IPAddress destination;

            while (true)
            {
                try
                {
                    var data = ReadPacket();

                    if (data.Packet == null && data.Payload != null)
                        // Invalid packet, read next
                        continue;

                    if (data.Packet == null)
                        return null;

                    timestampMs = data.Seconds * 1000 + (data.Microseconds / 1000.0);
                    packet = SACNPacket.Parse(data.Payload);
                    destination = data.Packet.DestinationAddress;
                }
                catch (InvalidDataException)
                {
                    // Ignore
                    continue;
                }

                break;
            }

            if (!this.timestampOffsetMs.HasValue)
            {
                timestampOffsetMs = timestampMs;
                // Offset
                timestampMs = 0;
            }
            else
            {
                timestampMs -= this.timestampOffsetMs.Value;
            }

            if (packet == null || packet.RootLayer == null || packet.RootLayer.FramingLayer == null)
                throw new ArgumentException("Invalid sACN packet");

            var rootLayer = packet.RootLayer;

            if (rootLayer.FramingLayer is DataFramingLayer dataFramingLayer)
            {
                var dmpLayer = dataFramingLayer.DMPLayer;
                if (dmpLayer == null)
                    throw new InvalidDataException("Not a valid Streaming DMX ACN packet");

                byte[] dmxData;
                if (dmpLayer.StartCode == 0xff)
                {
                    dmxData = new byte[dmpLayer.Data.Length - 1];
                    Buffer.BlockCopy(dmpLayer.Data, 1, dmxData, 0, dmxData.Length);
                }
                else
                {
                    dmxData = dmpLayer.Data;
                }

                return DmxDataOutputPacket.CreateFullFrame(timestampMs, dataFramingLayer.SequenceId, dataFramingLayer.UniverseId, destination, dmxData, dataFramingLayer.SyncAddress);
            }
            else if (rootLayer.FramingLayer is SyncFramingLayer syncFramingLayer)
            {
                return DmxDataOutputPacket.CreateSync(timestampMs, syncFramingLayer.SequenceId, syncFramingLayer.SyncAddress, destination);
            }
            else
            {
                throw new ArgumentException("Unknown packet type");
            }
        }
    }
}
