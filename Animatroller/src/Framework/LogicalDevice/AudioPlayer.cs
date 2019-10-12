using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice.Event;
using Animatroller.Framework.MonoExpanderMessages;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.Streams;

namespace Animatroller.Framework.LogicalDevice
{
    public class AudioPlayer : BaseDevice
    {
        public struct AudioData
        {
            public AudioTypes AudioType { get; set; }

            public string Filename { get; set; }
        }

        private bool silent;
        private double currentBackgroundVolume = 1.0;
        private double currentEffectVolume = 1.0;
        private double currentTrackVolume = 1.0;

        private ISubject<AudioData> audioTrackStart;
        private ISubject<AudioTypes> audioTrackDone;

        public event EventHandler<AudioChangedEventArgs> AudioChanged;
        public event EventHandler<AudioCommandEventArgs> ExecuteCommand;

        public AudioPlayer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.audioTrackStart = new Subject<AudioData>();
            this.audioTrackDone = new Subject<AudioTypes>();

            Executor.Current.MasterVolume.Subscribe(mv =>
                {
                    RaiseExecuteCommand(AudioCommandEventArgs.Commands.BackgroundVolume, this.currentBackgroundVolume * mv);
                    RaiseExecuteCommand(AudioCommandEventArgs.Commands.EffectVolume, this.currentEffectVolume * mv);
                    RaiseExecuteCommand(AudioCommandEventArgs.Commands.TrackVolume, this.currentTrackVolume * mv);
                });
        }

        public IObservable<AudioData> AudioTrackStart
        {
            get { return this.audioTrackStart.AsObservable(); }
        }

        public IObservable<AudioTypes> AudioTrackDone
        {
            get { return this.audioTrackDone.AsObservable(); }
        }

        internal void RaiseAudioTrackStart(AudioTypes audioType, string fileName)
        {
            this.audioTrackStart.OnNext(new AudioData
            {
                AudioType = audioType,
                Filename = fileName
            });
        }

        internal void RaiseAudioTrackDone()
        {
            this.audioTrackDone.OnNext(AudioTypes.Track);
        }

        protected virtual void RaiseAudioChanged(AudioChangedEventArgs.Commands command, string audioFile, double? leftVolume = null, double? rightVolume = null)
        {
            if (this.silent)
                return;

            AudioChanged?.Invoke(this, new AudioChangedEventArgs(command, audioFile, leftVolume, rightVolume));
        }

        protected virtual void RaiseExecuteCommand(AudioCommandEventArgs.Commands command)
        {
            if (this.silent)
                return;

            ExecuteCommand?.Invoke(this, new AudioCommandEventArgs(command));
        }

        protected virtual void RaiseExecuteCommand(AudioCommandEventArgs.Commands command, double value)
        {
            if (this.silent)
                return;

            ExecuteCommand?.Invoke(this, new AudioCommandValueEventArgs(command, value));
        }

        public AudioPlayer PlayEffect(string audioFile, double leftVolume, double rightVolume)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayFX, audioFile, leftVolume, rightVolume);

            return this;
        }

        public AudioPlayer PlayEffect(string audioFile, Import.LevelsPlayback levelsPlayback = null)
        {
            levelsPlayback?.Load(GetLevelsFromAudioFX("AudioEffect", audioFile));

            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayFX, audioFile);

            return this;
        }

        public AudioPlayer PlayEffect(string audioFile, double volume)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayFX, audioFile, volume, volume);

            return this;
        }

        public AudioPlayer PlayNewEffect(string audioFile, double leftVolume, double rightVolume, Import.LevelsPlayback levelsPlayback = null)
        {
            levelsPlayback?.Load(GetLevelsFromAudioFX("AudioEffect", audioFile));

            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayNewFX, audioFile, leftVolume, rightVolume);

            return this;
        }

        public AudioPlayer PlayNewEffect(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayNewFX, audioFile);

            return this;
        }

        public AudioPlayer PlayNewEffect(string audioFile, double volume)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayNewFX, audioFile, volume, volume);

            return this;
        }

        public AudioPlayer PlayBackground()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.PlayBackground);

            return this;
        }

        public AudioPlayer PauseBackground()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.PauseBackground);

            return this;
        }

        public AudioPlayer SetBackgroundVolume(double volume)
        {
            this.currentBackgroundVolume = volume;

            RaiseExecuteCommand(AudioCommandEventArgs.Commands.BackgroundVolume, volume * Executor.Current.MasterVolume.Value);

            return this;
        }

        public AudioPlayer SetEffectVolume(double volume)
        {
            this.currentEffectVolume = volume;

            RaiseExecuteCommand(AudioCommandEventArgs.Commands.EffectVolume, volume * Executor.Current.MasterVolume.Value);

            return this;
        }

        public AudioPlayer SetTrackVolume(double volume)
        {
            this.currentTrackVolume = volume;

            RaiseExecuteCommand(AudioCommandEventArgs.Commands.TrackVolume, volume * Executor.Current.MasterVolume.Value);

            return this;
        }

        public AudioPlayer NextBackgroundTrack()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.NextBackground);

            return this;
        }

        public AudioPlayer CueFX(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.CueFX, audioFile);

            return this;
        }

        public AudioPlayer ResumeFX()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.ResumeFX);

            return this;
        }

        public AudioPlayer PauseFX()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.PauseFX);

            return this;
        }

        public AudioPlayer StopFX()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.StopFX);

            return this;
        }

        public AudioPlayer SetSilent(bool silent)
        {
            this.silent = silent;

            return this;
        }

        public AudioPlayer CueTrack(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.CueTrack, audioFile);

            return this;
        }

        /// <summary>
        /// Cue and play track
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public AudioPlayer PlayTrack(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayTrack, audioFile);

            return this;
        }

        public AudioPlayer ResumeTrack()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.ResumeTrack);

            return this;
        }

        public AudioPlayer PauseTrack()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.PauseTrack);

            return this;
        }

        public AudioPlayer StopTrack()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.StopTrack);

            return this;
        }

        protected override void UpdateOutput()
        {
            // Nothing to do here
        }

        public string GetLevelsFromAudioFX(string audioType, string audioFile)
        {
            string audioFilename = Path.Combine(Executor.Current.ExpanderSharedFiles, audioType, audioFile);
            string levelsFilename = Path.Combine(Executor.Current.ExpanderSharedFiles, audioType, audioFile + ".levels");

            if (!File.Exists(levelsFilename))
            {
                using (ISampleSource source = CodecFactory.Instance.GetCodec(audioFilename).ToSampleSource())
                {
                    var fftProvider = new FftProvider(source.WaveFormat.Channels, FftSize.Fft1024);

                    int millisecondsPerFrame = 1000 / 40;

                    long maxBufferLengthInSamples = source.GetRawElements(millisecondsPerFrame);

                    long bufferLength = Math.Min(source.Length, maxBufferLengthInSamples);

                    float[] buffer = new float[bufferLength];

                    int read = 0;
                    int totalSamplesRead = 0;

                    var fftData = new float[1024];

                    var list = new List<float>();
                    float highest = 0;
                    do
                    {
                        //determine how many samples to read
                        int samplesToRead = (int)Math.Min(source.Length - totalSamplesRead, buffer.Length);

                        read = source.Read(buffer, 0, samplesToRead);
                        if (read == 0)
                            break;

                        totalSamplesRead += read;

                        //add read data to the fftProvider
                        fftProvider.Add(buffer, read);

                        fftProvider.GetFftData(fftData);

                        float highestAmplitude = 0;
                        for (int i = 0; i < fftData.Length / 2; i++)
                        {
                            if (fftData[i] > highestAmplitude)
                                highestAmplitude = fftData[i];
                        }

                        list.Add(highestAmplitude);
                        if (highestAmplitude > highest)
                            highest = highestAmplitude;
                    } while (totalSamplesRead < source.Length);

                    if (highest > 0)
                    {
                        // Adjust to equalize
                        float adjustment = 1 / highest;

                        for (int i = 0; i < list.Count; i++)
                        {
                            list[i] *= adjustment;
                        }
                    }

                    using (var fs = File.Create(levelsFilename))
                    {
                        fs.Write(list.Select(x => (byte)(x * 255)).ToArray(), 0, list.Count);
                    }
                }
            }

            return levelsFilename;
        }
    }

    /*    public class BasicSpectrumProvider : FftProvider, ISpectrumProvider
        {
            private readonly int _sampleRate;
            private readonly List<object> _contexts = new List<object>();

            public BasicSpectrumProvider(IChannel channels, int sampleRate, FftSize fftSize)
                : base(channels, fftSize)
            {
                if (sampleRate <= 0)
                    throw new ArgumentOutOfRangeException("sampleRate");
                _sampleRate = sampleRate;
            }

            public int GetFftBandIndex(float frequency)
            {
                int fftSize = (int)FftSize;
                double f = _sampleRate / 2.0;
                // ReSharper disable once PossibleLossOfFraction
                return (int)((frequency / f) * (fftSize / 2));
            }

            public bool GetFftData(float[] fftResultBuffer, object context)
            {
                if (_contexts.Contains(context))
                    return false;

                _contexts.Add(context);
                GetFftData(fftResultBuffer);
                return true;
            }

            public override void Add(float[] samples, int count)
            {
                base.Add(samples, count);
                if (count > 0)
                    _contexts.Clear();
            }

            public override void Add(float left, float right)
            {
                base.Add(left, right);
                _contexts.Clear();
            }
        }*/
}
