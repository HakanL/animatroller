using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.MonoExpanderMessages;
using Serilog;
using SupersonicSound.Exceptions;
using SupersonicSound.LowLevel;

namespace Animatroller.MonoExpander
{
    public class AudioSystem : IDisposable
    {
        public class SubSystem
        {
            private readonly LowLevelSystem lowLevelSystem;
            private ChannelGroup channelGroup;

            protected internal SubSystem(LowLevelSystem lowLevelSystem, string name, AudioTypes audioType)
            {
                this.lowLevelSystem = lowLevelSystem;
                this.channelGroup = this.lowLevelSystem.CreateChannelGroup(name);
                AudioType = audioType;
            }

            public AudioTypes AudioType { get; }

            public Sound? CurrentSound { get; set; }

            public Channel? CurrentChannel { get; set; }

            public void Pause()
            {
                this.channelGroup.Pause = true;
            }

            public void Resume()
            {
                this.channelGroup.Pause = false;
            }

            public void Stop()
            {
                this.channelGroup.Stop();
            }

            public float? Volume
            {
                get => this.channelGroup.Volume;
                set => this.channelGroup.Volume = value;
            }

            public int? LastPosition { get; set; }

            public Channel PlaySound(Sound sound, bool paused)
            {
                return this.lowLevelSystem.PlaySound(sound, this.channelGroup, paused);
            }
        }

        private static readonly HashSet<int> usedDrivers = new HashSet<int>();
        private readonly ILogger log;
        private readonly LowLevelSystem fmodSystem;
        private readonly int outputId;
        private string currentBgTrackName;
        private string currentTrack;
        private int currentBgTrack;
        private List<string> backgroundAudioTracks;
        private readonly Dictionary<string, Sound> loadedSounds;
        private bool autoStartBackgroundTrack;
        private bool backgroundAudioPlaying;
        private readonly List<IDisposable> disposeList;
        private readonly Dictionary<Guid, Channel> currentChannels = new Dictionary<Guid, Channel>();
        private readonly Action<object> messageSenderAction;
        private Stopwatch reportWatch;
        private int reportCounter;
        private readonly Random random;

        public AudioSystem(
            ILogger logger,
            string driverName,
            int outputId,
            bool autoStartBackgroundTrack,
            List<string> backgroundAudioTracks,
            Action<object> messageSenderAction)
        {
            this.log = logger;
            this.outputId = outputId;
            this.loadedSounds = new Dictionary<string, Sound>();
            this.currentBgTrack = -1;
            this.disposeList = new List<IDisposable>();
            this.autoStartBackgroundTrack = autoStartBackgroundTrack;
            this.backgroundAudioTracks = backgroundAudioTracks;
            this.messageSenderAction = messageSenderAction;
            this.random = new Random();

            this.log.Information("Initializing FMOD sound system");

            this.fmodSystem = new LowLevelSystem(preInit: c =>
            {
                int? driverId = null;
                for (int i = 0; i < c.GetNumDrivers(); i++)
                {
                    var driverInfo = c.GetDriverInfo(i);

                    this.log.Debug("Driver id {DriverId} - {DriverName}", i, driverInfo.Name);

                    if (!driverId.HasValue && driverInfo.Name.StartsWith(driverName))
                    {
                        if (usedDrivers.Contains(i))
                            throw new ArgumentException("This driver id has already been used");

                        driverId = i;
                    }
                }

                this.log.Information("Setting driver id {DriverId}", driverId);
                c.Driver = driverId ?? 0;
                usedDrivers.Add(driverId ?? 0);
            });

            FxSystem = new SubSystem(this.fmodSystem, "FX", AudioTypes.Effect);
            TrkSystem = new SubSystem(this.fmodSystem, "Track", AudioTypes.Track);
            BgSystem = new SubSystem(this.fmodSystem, "Background", AudioTypes.Background);
        }

        public SubSystem FxSystem { get; }

        public SubSystem TrkSystem { get; }

        public SubSystem BgSystem { get; }

        public Sound LoadSound(string fileName)
        {
            if (!Path.HasExtension(fileName))
                fileName += ".wav";

            Sound sound;
            if (!this.loadedSounds.TryGetValue(fileName, out sound))
            {
                // Load
                sound = this.fmodSystem.CreateSound(fileName, Mode.Default);

                this.loadedSounds.Add(fileName, sound);
            }

            return sound;
        }

        public void SetBackgroundTracks(IEnumerable<string> fileNames)
        {
            this.backgroundAudioTracks = fileNames.ToList();

            if (this.backgroundAudioPlaying)
                PlayNextBackground();
        }

        public void PlaySound(string fileName, bool playOnNewChannel, double leftVol = 1.0, double? rightVol = null)
        {
            var sound = LoadSound(fileName);
            sound.LoopCount = 0;

            if (!playOnNewChannel)
            {
                try
                {
                    FxSystem.CurrentChannel?.Stop();
                    FxSystem.CurrentChannel?.RemoveCallback();
                }
                catch (FmodInvalidHandleException)
                {
                    // Ignore
                }
            }

            var channel = FxSystem.PlaySound(sound, true);

            Guid channelId = Guid.NewGuid();

            channel.SetCallback((type, data1, data2) =>
            {
                if (type == ChannelControlCallbackType.End)
                {
                    this.log.Debug("FX {0} ended", fileName);

                    this.messageSenderAction(new AudioFinished
                    {
                        Output = this.outputId,
                        Id = Path.GetFileName(fileName),
                        Type = AudioTypes.Effect
                    });

                    if (this.currentChannels.TryGetValue(channelId, out Channel chn))
                    {
                        this.currentChannels.Remove(channelId);
                    }

                    FxSystem.CurrentChannel = null;
                }
            });
            this.currentChannels.Add(channelId, channel);

            if (!rightVol.HasValue)
                rightVol = leftVol;

            channel.FmodChannel.setMixLevelsOutput((float)leftVol, (float)rightVol.Value, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

            // Play
            FxSystem.Resume();
            channel.Pause = false;

            if (!playOnNewChannel)
                FxSystem.CurrentChannel = channel;

            this.messageSenderAction(new AudioStarted
            {
                Output = this.outputId,
                Id = Path.GetFileName(fileName),
                Type = AudioTypes.Effect
            });
        }

        public void LoadTrack(string fileName)
        {
            if (!Path.HasExtension(fileName))
                fileName += ".wav";

            this.log.Information("Play track {0}", Path.GetFileName(fileName));

            var sound = this.fmodSystem.CreateStream(fileName, Mode.Default);

            if (TrkSystem.CurrentSound.HasValue)
                this.disposeList.Add(TrkSystem.CurrentSound.Value);
            TrkSystem.CurrentSound = null;
            this.currentTrack = Path.GetFileName(fileName);
            TrkSystem.CurrentSound = sound;
        }

        public void PlayTrack()
        {
            var sound = TrkSystem.CurrentSound;
            if (!sound.HasValue)
                // No sound loaded
                return;

            try
            {
                var chn = TrkSystem.CurrentChannel;
                if (chn.HasValue)
                {
                    // Make sure we reset this first so we can ignore the callback
                    TrkSystem.CurrentChannel = null;
                    chn?.Stop();
                    chn?.RemoveCallback();
                }
            }
            catch (FmodInvalidHandleException)
            {
                // Ignore
            }

            var channel = TrkSystem.PlaySound(sound.Value, true);
            string trackName = this.currentTrack;

            channel.SetCallback((type, data1, data2) =>
            {
                if (type == ChannelControlCallbackType.End)
                {
                    this.log.Debug("Track {0} ended", trackName);

                    this.messageSenderAction(new AudioFinished
                    {
                        Output = this.outputId,
                        Id = trackName,
                        Type = AudioTypes.Track
                    });
                }
            });

            TrkSystem.CurrentChannel = channel;

            // Play
            channel.Pause = false;

            // Send status message
            this.messageSenderAction(new AudioStarted
            {
                Output = this.outputId,
                Id = trackName,
                Type = AudioTypes.Track
            });
        }

        public void PlayNextBackground()
        {
            // Find next background track
            if (this.backgroundAudioTracks.Count == 0)
                // No tracks
                return;

            int index;
            while (true)
            {
                index = this.random.Next(this.backgroundAudioTracks.Count);
                if (this.backgroundAudioTracks.Count > 1 && this.currentBgTrack == index)
                    continue;
                break;
            }
            this.currentBgTrack = index;
            string fileName = this.backgroundAudioTracks[index];
            this.currentBgTrackName = Path.GetFileName(fileName);

            this.log.Information("Play background track {0}", Path.GetFileName(fileName));

            var sound = this.fmodSystem.CreateStream(fileName, Mode.Default);

            try
            {
                var chn = BgSystem.CurrentChannel;
                if (chn.HasValue)
                {
                    // Make sure we reset this first so we can ignore the callback
                    BgSystem.CurrentChannel = null;
                    chn?.Stop();
                    chn?.RemoveCallback();
                }
            }
            catch (FmodInvalidHandleException)
            {
                // Ignore
            }

            if (BgSystem.CurrentSound.HasValue)
                this.disposeList.Add(BgSystem.CurrentSound);
            BgSystem.CurrentSound = null;

            var channel = BgSystem.PlaySound(sound, true);

            string bgName = this.currentBgTrackName;
            channel.SetCallback((type, data1, data2) =>
            {
                if (type == ChannelControlCallbackType.End)
                {
                    this.log.Debug("Background {0} ended", bgName);

                    this.messageSenderAction(new AudioFinished
                    {
                        Output = this.outputId,
                        Id = bgName,
                        Type = AudioTypes.Background
                    });

                    if (BgSystem.CurrentChannel.HasValue)
                        PlayNextBackground();
                }
            });

            BgSystem.CurrentSound = sound;
            BgSystem.CurrentChannel = channel;

            // Play
            channel.Pause = false;
            this.backgroundAudioPlaying = true;

            this.messageSenderAction(new AudioStarted
            {
                Output = this.outputId,
                Id = bgName,
                Type = AudioTypes.Background
            });
        }

        public void Dispose()
        {
            this.fmodSystem?.Dispose();
        }

        private bool ReportChannelPosition(Channel? channel, string trackId, SubSystem subSystem)
        {
            if (channel.HasValue)
            {
                var chn = channel.Value;

                int? pos = (int?)chn.GetPosition(TimeUnit.Milliseconds);

                if (pos == subSystem.LastPosition)
                    return false;

                subSystem.LastPosition = pos;

                if (pos.HasValue)
                {
                    this.messageSenderAction(new AudioPositionChanged
                    {
                        Output = this.outputId,
                        Id = trackId,
                        Type = subSystem.AudioType,
                        Position = pos.Value
                    });

                    return true;
                }
            }

            subSystem.LastPosition = null;

            return false;
        }

        public void Update()
        {
            if (this.reportWatch == null)
                throw new Exception("Start is not called");

            this.fmodSystem.Update();

            this.disposeList.ForEach(x => x.Dispose());
            this.disposeList.Clear();

            this.reportCounter++;

            if (this.reportCounter % 5 == 0)
            {
                if (ReportChannelPosition(TrkSystem.CurrentChannel, this.currentTrack, TrkSystem))
                    this.reportWatch.Restart();
            }

            if (this.reportCounter % 10 == 0)
            {
                if (ReportChannelPosition(BgSystem.CurrentChannel, this.currentBgTrackName, BgSystem))
                    this.reportWatch.Restart();
            }
        }

        public void Start()
        {
            if (this.autoStartBackgroundTrack)
                PlayNextBackground();

            this.reportWatch = Stopwatch.StartNew();
            this.reportCounter = 0;
        }

        public void ResumeBackground()
        {
            if (BgSystem.CurrentChannel.HasValue)
            {
                var chn = BgSystem.CurrentChannel.Value;
                chn.Pause = false;
            }
            else
                PlayNextBackground();
            this.backgroundAudioPlaying = true;
        }

        public void PauseBackground()
        {
            if (BgSystem.CurrentChannel.HasValue)
            {
                var chn = BgSystem.CurrentChannel.Value;
                chn.Pause = true;
            }

            this.backgroundAudioPlaying = false;
        }

        public void ResumeTrack()
        {
            if (TrkSystem.CurrentChannel.HasValue)
            {
                var chn = TrkSystem.CurrentChannel.Value;
                chn.Pause = false;
            }
        }

        public void PauseTrack()
        {
            if (TrkSystem.CurrentChannel.HasValue)
            {
                var chn = TrkSystem.CurrentChannel.Value;
                chn.Pause = true;
            }
        }

        public void StopTrack()
        {
            if (TrkSystem.CurrentChannel.HasValue)
            {
                var chn = TrkSystem.CurrentChannel.Value;
                chn.Stop();
            }
        }
    }
}
