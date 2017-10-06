using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raspberry.IO.Components.Devices.PiFaceDigital;
using SupersonicSound.LowLevel;
using Serilog;
using SupersonicSound.Exceptions;
using System.Diagnostics;
using Animatroller.Framework.MonoExpanderMessages;
using Newtonsoft.Json.Linq;
using System.IO.Ports;

namespace Animatroller.MonoExpander
{
    public partial class Main
    {
        public void Handle(SetOutputRequest message)
        {
            this.log.Information("Set output {0} to {1}", message.Output, message.Value);

            if (!message.Output.StartsWith("d"))
                return;

            int outputId;
            if (!int.TryParse(message.Output.Substring(1), out outputId))
                return;

            if (outputId < 0 || outputId > 7)
                return;

            if (this.piFace != null)
            {
                this.piFace.OutputPins[outputId].State = message.Value != 0.0;
                this.piFace.UpdatePiFaceOutputPins();
            }
        }

        public void Handle(SendSerialRequest message)
        {
            this.log.Information("Send serial data to port {0}", message.Port);

            SerialPort serialPort;
            if (!this.serialPorts.TryGetValue(message.Port, out serialPort))
            {
                this.log.Warning("Invalid serial port {0}", message.Port);
                return;
            }

            serialPort.Write(message.Data, 0, message.Data.Length);
        }

        public void Handle(AudioEffectCue message)
        {
            this.log.Information("Cue audio FX {0}", message.FileName);

            LoadSound(message.FileName);
        }

        public void Handle(AudioEffectPlay message)
        {
            this.log.Information("Play audio FX {0}", message.FileName);

            if (message.VolumeLeft.HasValue && message.VolumeRight.HasValue)
                PlaySound(message.FileName, message.Simultaneous, message.VolumeLeft.Value, message.VolumeRight.Value);
            else
                PlaySound(message.FileName, message.Simultaneous);
        }

        public void Handle(AudioEffectPause message)
        {
            this.log.Information("Pause audio FX");

            if (this.currentFxChannel.HasValue)
            {
                var chn = this.currentBgChannel.Value;
                chn.Pause = true;
            }
        }

        public void Handle(AudioEffectResume message)
        {
            this.log.Information("Resume audio FX");

            if (this.currentFxChannel.HasValue)
            {
                var chn = this.currentFxChannel.Value;
                chn.Pause = false;
            }
        }

        public void Handle(AudioEffectSetVolume message)
        {
            this.log.Information("Set FX audio volume to {0:P0}", message.Volume);

            this.fxGroup.Volume = (float)message.Volume;
        }

        public void Handle(AudioTrackSetVolume message)
        {
            this.log.Information("Set Track audio volume to {0:P0}", message.Volume);

            this.trkGroup.Volume = (float)message.Volume;
        }

        public void Handle(AudioBackgroundSetVolume message)
        {
            this.log.Information("Set BG audio volume to {0:P0}", message.Volume);

            this.bgGroup.Volume = (float)message.Volume;
        }

        public void Handle(AudioBackgroundResume message)
        {
            this.log.Information("Resume audio BG");

            if (this.currentBgChannel.HasValue)
            {
                var chn = this.currentBgChannel.Value;
                chn.Pause = false;
            }
            else
                PlayNextBackground();
        }

        public void Handle(AudioBackgroundPause message)
        {
            this.log.Information("Pause audio BG");

            if (this.currentBgChannel.HasValue)
            {
                var chn = this.currentBgChannel.Value;
                chn.Pause = true;
            }
        }

        public void Handle(AudioBackgroundNext message)
        {
            this.log.Information("Next audio BG track");

            PlayNextBackground();
        }

        public void Handle(AudioTrackPlay message)
        {
            this.log.Information("Play audio track {0}", message.FileName);

            LoadTrack(message.FileName);
            PlayTrack();
        }

        public void Handle(AudioTrackCue message)
        {
            this.log.Information("Cue audio track {0}", message.FileName);

            LoadTrack(message.FileName);
        }

        public void Handle(AudioTrackResume message)
        {
            this.log.Information("Resume audio track");

            if (this.currentTrkChannel.HasValue)
            {
                var chn = this.currentTrkChannel.Value;
                chn.Pause = false;
            }
        }

        public void Handle(AudioTrackPause message)
        {
            this.log.Information("Pause audio track");

            if (this.currentTrkChannel.HasValue)
            {
                var chn = this.currentTrkChannel.Value;
                chn.Pause = true;
            }
        }

        public void Handle(VideoPlay message)
        {
            this.log.Information("Play video track {0}", message.FileName);

            PlayVideo(message.FileName);
        }
    }
}
