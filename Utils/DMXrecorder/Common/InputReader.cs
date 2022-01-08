﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Animatroller.Common
{
    public class InputReader : IInputReader
    {
        private readonly IO.IFileReader reader;
        private readonly List<DmxDataOutputPacket> readPackets = new List<DmxDataOutputPacket>();
        private int readPosition = 0;
        private readonly List<InputFrame> frames = new List<InputFrame>();
        private readonly Dictionary<int, InputFrame> framePerSyncAddress = new Dictionary<int, InputFrame>();

        public InputReader(IO.IFileReader reader)
        {
            this.reader = reader;
            ReadAll();
        }

        public void ReadAll()
        {
            var readFrames = new List<DmxDataOutputPacket>();
            double? initialTimestampOffset = null;

            while (this.reader.DataAvailable)
            {
                var data = this.reader.ReadFrame();

                if (!initialTimestampOffset.HasValue)
                    initialTimestampOffset = data.TimestampMS;

                if (data == null)
                    break;
                if (data.Content == null)
                    continue;

                // Remove any offset in the file
                data.TimestampMS -= initialTimestampOffset ?? 0;
                readFrames.Add(data);
            }

            HasSyncFrames = readFrames.Any(x => x.Content is SyncFrame);

            // Organize data
            foreach (var data in readFrames)
            {
                InputFrame currentFrame;

                if (HasSyncFrames)
                {
                    switch (data.Content)
                    {
                        case DmxDataFrame dmxDataFrame:
                            if (!this.framePerSyncAddress.TryGetValue(dmxDataFrame.SyncAddress, out currentFrame))
                            {
                                currentFrame = new InputFrame
                                {
                                    SyncAddress = dmxDataFrame.SyncAddress
                                };
                                this.framePerSyncAddress.Add(dmxDataFrame.SyncAddress, currentFrame);
                            }
                            currentFrame.DmxData.Add(dmxDataFrame);
                            break;

                        case SyncFrame syncFrame:
                            if (this.framePerSyncAddress.TryGetValue(syncFrame.SyncAddress, out currentFrame))
                            {
                                currentFrame.TimestampMS = data.TimestampMS;
                                currentFrame.Position = this.frames.Count;

                                this.frames.Add(currentFrame);

                                this.framePerSyncAddress.Remove(syncFrame.SyncAddress);
                            }
                            break;
                    }
                }
                else
                {
                    if (data.Content is DmxDataFrame dmxDataFrame)
                    {
                        currentFrame = new InputFrame
                        {
                            TimestampMS = data.TimestampMS,
                            Position = this.frames.Count,
                            SyncAddress = dmxDataFrame.UniverseId       // Set it to the universe to simplify the rest of the code
                        };
                        currentFrame.DmxData.Add(dmxDataFrame);

                        this.frames.Add(currentFrame);
                    }
                }
            }
        }

        public InputFrame ReadFrame()
        {
            if (this.readPosition < this.frames.Count)
                return this.frames[this.readPosition++];

            return null;
        }

        /*public DmxDataOutputPacket ReadFrameLegacy()
        {
            if (this.readPosition < this.readPackets.Count)
                return this.readPackets[this.readPosition++];

            do
            {
                if (!this.reader.DataAvailable)
                    break;

                var frame = this.reader.ReadFrame();
                if (frame == null)
                    break;
                if (frame.Content == null)
                    continue;

                this.readPackets.Add(frame);
                this.readPosition++;
                return frame;
            } while (this.reader.DataAvailable);

            return null;
        }*/

        public void Rewind()
        {
            this.readPosition = 0;
        }

        public int FramesRead => this.readPackets.Count;

        public int TotalFrames => this.frames.Count;

        public bool HasSyncFrames { get; set; }
    }
}
