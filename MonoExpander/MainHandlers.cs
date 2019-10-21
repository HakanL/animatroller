using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using Animatroller.Framework.MonoExpanderMessages;

namespace Animatroller.MonoExpander
{
    public partial class Main
    {
        public void Handle(SetOutputRequest message)
        {
            this.log.Information("Set output {Output} to {Value}", message.Output, message.Value);

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
            this.log.Information("Send serial data to port {Port}", message.Port);

            SerialPort serialPort;
            if (!this.serialPorts.TryGetValue(message.Port, out serialPort))
            {
                this.log.Warning("Invalid serial port {Port}", message.Port);
                return;
            }

            serialPort.Write(message.Data, 0, message.Data.Length);
        }

        public void Handle(AudioEffectCue message)
        {
            this.log.Information("Cue audio FX {Filename} on output {Output}", message.FileName, message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.LoadSound(Path.Combine(this.soundEffectPath, message.FileName)));
        }

        public void Handle(AudioEffectPlay message)
        {
            this.log.Information("Play audio FX {Filename} on output {Output}", message.FileName, message.Output);

            if (message.VolumeLeft.HasValue && message.VolumeRight.HasValue)
                ExecuteAudioSystemCommand(message.Output, a => a.PlayFx(
                    Path.Combine(this.soundEffectPath, message.FileName), message.Simultaneous, message.VolumeLeft.Value, message.VolumeRight.Value));
            else
                ExecuteAudioSystemCommand(message.Output, a => a.PlayFx(
                    Path.Combine(this.soundEffectPath, message.FileName), message.Simultaneous));
        }

        public void Handle(AudioEffectPause message)
        {
            this.log.Information("Pause audio FX on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.FxSystem.Pause());
        }

        public void Handle(AudioEffectStop message)
        {
            this.log.Information("Stop audio FX on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.FxSystem.Stop());
        }

        public void Handle(AudioEffectResume message)
        {
            this.log.Information("Resume audio FX on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.FxSystem.Resume());
        }

        public void Handle(AudioEffectSetVolume message)
        {
            this.log.Information("Set FX audio volume to {0:P0} on output {Output}", message.Volume, message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.FxSystem.Volume = (float)message.Volume);
        }

        public void Handle(AudioTrackSetVolume message)
        {
            this.log.Information("Set Track audio volume to {0:P0} on output {Output}", message.Volume, message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.TrkSystem.Volume = (float)message.Volume);
        }

        public void Handle(AudioBackgroundSetVolume message)
        {
            this.log.Information("Set BG audio volume to {0:P0} on output {Output}", message.Volume, message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.BgSystem.Volume = (float)message.Volume);
        }

        public void Handle(AudioBackgroundResume message)
        {
            this.log.Information("Resume audio BG on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.ResumeBackground());
        }

        public void Handle(AudioBackgroundPause message)
        {
            this.log.Information("Pause audio BG on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.PauseBackground());
        }

        public void Handle(SetBackgroundAudioFiles message)
        {
            this.log.Information("Set background audio files on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.SetBackgroundTracks(message.Filenames
                .Select(x => Path.Combine(FileStoragePath, FileTypes.AudioBackground.ToString(), x))));
        }

        public void Handle(AudioBackgroundNext message)
        {
            this.log.Information("Next audio BG track on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.PlayNextBackground());
        }

        public void Handle(AudioTrackPlay message)
        {
            this.log.Information("Play audio track {Filename} on output {Output}", message.FileName, message.Output);

            ExecuteAudioSystemCommand(message.Output, a =>
            {
                a.LoadTrack(Path.Combine(this.trackPath, message.FileName));
                a.PlayTrack();
            });
        }

        public void Handle(AudioTrackCue message)
        {
            this.log.Information("Cue audio track {Filename} on output {Output}", message.FileName, message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.LoadTrack(Path.Combine(this.trackPath, message.FileName)));
        }

        public void Handle(AudioTrackResume message)
        {
            this.log.Information("Resume audio track on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.ResumeTrack());
        }

        public void Handle(AudioTrackPause message)
        {
            this.log.Information("Pause audio track on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.PauseTrack());
        }

        public void Handle(AudioTrackStop message)
        {
            this.log.Information("Stop audio track on output {Output}", message.Output);

            ExecuteAudioSystemCommand(message.Output, a => a.StopTrack());
        }

        public void Handle(VideoPlay message)
        {
            this.log.Information("Play video track {Filename}", message.FileName);

            PlayVideo(message.FileName);
        }
    }
}
