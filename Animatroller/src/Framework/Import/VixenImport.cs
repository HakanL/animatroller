using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using Animatroller.Framework.Controller;
using VIX = Animatroller.Framework.Import.FileFormat.Vixen;

namespace Animatroller.Framework.Import
{
    public class VixenImport : BufferImporter
    {
        public VixenImport(string filename)
        {
            var deserializer = new XmlSerializer(typeof(VIX.Program));

            VIX.Program sequence;
            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (VIX.Program)deserializer.Deserialize(textReader);
            }

            this.eventPeriodInMilliseconds = sequence.EventPeriodInMilliseconds;

            byte[] effectData = Convert.FromBase64String(sequence.EventValues);
            this.effectsPerChannel = effectData.Length / sequence.Channels.Length;

            int i = 0;
            foreach (var channel in sequence.Channels)
            {
                var channelIdentity = new VixenChannel(channel.output);
                AddChannelData(channelIdentity, new ChannelData(channel.Value));

                var channelEffectData = new byte[this.effectsPerChannel];
                Array.Copy(effectData, i, channelEffectData, 0, channelEffectData.Length);
                effectDataPerChannel[channelIdentity] = channelEffectData;

                i += this.effectsPerChannel;
            }
        }

        public class VixenChannel : IChannelIdentity
        {
            public int Channel { get; set; }

            public VixenChannel(int channel)
            {
                this.Channel = channel;
            }

            public override bool Equals(object obj)
            {
                var b = (obj as VixenChannel);

                return this.Channel == b.Channel;
            }

            public override int GetHashCode()
            {
                return this.Channel.GetHashCode();
            }

            public override string ToString()
            {
                return string.Format("{0}", this.Channel);
            }

            public int CompareTo(object other)
            {
                return this.Channel.CompareTo(((VixenChannel)other).Channel);
            }
        }
    }
}
