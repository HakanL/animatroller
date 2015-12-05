//#define DEBUG_VERBOSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raspberry.IO.Components.Devices.PiFaceDigital;
using SupersonicSound.LowLevel;
using NLog;
using SupersonicSound.Exceptions;
using System.Diagnostics;
using org.freedesktop.DBus;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using Akka.Actor;
using Animatroller.Framework.MonoExpanderMessages;

namespace Animatroller.MonoExpander
{
    public partial class Main : IDisposable
    {
        public enum VideoSystems
        {
            None,
            OMX
        }

        private Logger log;
        private PiFaceDigitalDevice piFace;
        private SupersonicSound.LowLevel.LowLevelSystem fmodSystem;
        private List<string> backgroundAudioTracks;
        private string instanceId;
        private Dictionary<string, Sound> loadedSounds;
        private string soundEffectPath;
        private string trackPath;
        private string videoPath;
        private ChannelGroup fxGroup;
        private ChannelGroup bgGroup;
        private Channel? currentFxChannel;
        private Channel? currentBgChannel;
        private Channel? currentTrkChannel;
        private Sound? currentBgSound;
        private Sound? currentTrkSound;
        private string currentTrack;
        private int currentBgTrack;
        private string currentBgTrackName;
        private Random random;
        private bool autoStartBackgroundTrack;
        private VideoSystems videoSystem;
        private bool videoPlaying;
        private List<IDisposable> disposeList;
        private int? lastPosBg;
        private int? lastPosTrk;
        private ActorSystem system;
        private IActorRef clientActor;
        private Dictionary<Address, IActorRef> serverActors;
        private string fileStoragePath;

        public Main(Arguments args)
        {
            this.log = LogManager.GetLogger("Main");
            this.serverActors = new Dictionary<Address, IActorRef>();
            this.fileStoragePath = args.FileStoragePath;

            // Clean up temp folder
            string tempFolder = Path.Combine(this.fileStoragePath, "tmp");
            Directory.CreateDirectory(tempFolder);
            Directory.GetDirectories(tempFolder).ToList().ForEach(x => Directory.Delete(x, true));

            if (!string.IsNullOrEmpty(args.VideoSystem))
            {
                switch (args.VideoSystem.ToLower())
                {
                    case "omx":
                        this.videoSystem = VideoSystems.OMX;
                        break;

                    default:
                        throw new ArgumentException("Invalid video system type");
                }
            }
            else
                this.videoSystem = VideoSystems.None;

            if (args.AudioSystem && this.videoSystem != VideoSystems.None)
                throw new ArgumentException("Cannot support both audio and video system concurrently");

            if (this.videoSystem != VideoSystems.None)
            {
                // Disable console log output
                this.log.Info("Video System, turning off console logging and cursor");

                var logConfig = LogManager.Configuration;
                var consoleTargets = new List<string>();
                consoleTargets.AddRange(logConfig.AllTargets
                    .OfType<NLog.Targets.ColoredConsoleTarget>()
                    .Select(x => x.Name));
                consoleTargets.AddRange(logConfig.AllTargets
                    .OfType<NLog.Targets.ConsoleTarget>()
                    .Select(x => x.Name));
                foreach (var loggingRule in logConfig.LoggingRules)
                {
                    loggingRule.Targets
                        .Where(x => consoleTargets.Contains(x.Name) || consoleTargets.Contains(x.Name + "_wrapped"))
                        .ToList()
                        .ForEach(x => loggingRule.Targets.Remove(x));
                }
                LogManager.Configuration = logConfig;

                Console.CursorVisible = false;
                Console.Clear();
            }

            this.loadedSounds = new Dictionary<string, Sound>();
            this.currentBgTrack = -1;
            this.random = new Random();
            this.disposeList = new List<IDisposable>();

            string fileStoragePath = Path.GetFullPath(args.FileStoragePath);

            this.soundEffectPath = Path.Combine(fileStoragePath, FileTypes.AudioEffect.ToString());
            this.trackPath = Path.Combine(fileStoragePath, FileTypes.AudioTrack.ToString());
            this.videoPath = Path.Combine(fileStoragePath, FileTypes.Video.ToString());

            this.autoStartBackgroundTrack = args.BackgroundTrackAutoStart;

            // Try to read instance id from disk
            try
            {
                using (var f = File.OpenText(Path.Combine(fileStoragePath, "MonoExpander_InstanceId.txt")))
                {
                    this.instanceId = f.ReadLine();
                }
            }
            catch
            {
                // Generate new
                this.instanceId = Guid.NewGuid().ToString("n");

                using (var f = File.CreateText(Path.Combine(fileStoragePath, "MonoExpander_InstanceId.txt")))
                {
                    f.WriteLine(this.instanceId);
                    f.Flush();
                }
            }

            this.log.Info("Instance Id {0}", this.instanceId);
            this.log.Info("Video Path {0}", this.videoPath);
            this.log.Info("Track Path {0}", this.trackPath);
            this.log.Info("FX Path {0}", this.soundEffectPath);

            this.backgroundAudioTracks = new List<string>();
            if (!string.IsNullOrEmpty(args.BackgroundTracksPath))
            {
                this.backgroundAudioTracks.AddRange(Directory.GetFiles(args.BackgroundTracksPath, "*.wav"));
                this.backgroundAudioTracks.AddRange(Directory.GetFiles(args.BackgroundTracksPath, "*.mp3"));
            }

            if (args.AudioSystem)
            {
                this.log.Info("Initializing FMOD sound system");
                this.fmodSystem = new LowLevelSystem();

                this.fxGroup = this.fmodSystem.CreateChannelGroup("FX");
                this.bgGroup = this.fmodSystem.CreateChannelGroup("Background");
            }

            if (SupersonicSound.Wrapper.Util.IsUnix)
            {
                this.log.Info("Initializing PiFace");

                try
                {
                    this.piFace = new PiFaceDigitalDevice();

                    // Setup events
                    foreach (var ip in this.piFace.InputPins)
                    {
                        ip.OnStateChanged += (s, e) =>
                        {
                            SendInputMessage(e.pin.Id, e.pin.State);
                        };

                        // Send current state
                        SendInputMessage(ip.Id, ip.State);
                    }
                }
                catch (Exception ex)
                {
                    this.log.Warn(ex, "Failed to initialize PiFace");
                }
            }

            this.log.Info("Initializing Akka listener");

            var akkaConfig = ConfigurationFactory.ParseString(@"
                akka {
                    ##log-config-on-start = on
                    actor {
                        provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                    }
                    remote {
                        helios.tcp {
		                    #port = 0
		                    hostname = 0.0.0.0
                        }
                    }
                    cluster {
                        #seed-nodes = [""akka.tcp://Animatroller@127.0.0.1:8088""]
                        roles = [expander]
                    }
                }
            ");

            string seeds = string.Join(",", args.Servers.Select(x => string.Format("\"akka.tcp://Animatroller@{0}:{1}\"", x.Host, x.Port)));
            string seedsString = string.Format(@"akka.cluster.seed-nodes = [{0}]", seeds);

            var finalConfig = ConfigurationFactory.ParseString(
                    string.Format(@"akka.remote.helios.tcp.public-hostname = {0}
                        akka.remote.helios.tcp.port = {1}",
                    Environment.MachineName, args.ListenPort))
                .WithFallback(ConfigurationFactory.ParseString(seedsString))
                .WithFallback(akkaConfig);

            this.system = ActorSystem.Create("Animatroller", finalConfig);

            this.clientActor = this.system.ActorOf(Props.Create<MonoExpanderClientActor>(this), "Expander");
        }

        public string FileStoragePath
        {
            get { return this.fileStoragePath; }
        }

        public void AddServer(Address address, IActorRef sender)
        {
            this.serverActors.Add(address, sender);
        }

        public void RemoveServer(Address address)
        {
            this.serverActors.Remove(address);
        }

        public string InstanceId
        {
            get { return this.instanceId; }
        }

        public void SendMessage(object message)
        {
            this.serverActors.Values.ToList().ForEach(x =>
            {
                x.Tell(message, this.clientActor);
            });
        }

        private void SendInputMessage(int input, bool state)
        {
            this.log.Debug("Input {0} set to {1}", input, state ? 1 : 0);

            SendMessage(new InputChanged
            {
                Input = "d" + input.ToString(),
                Value = state ? 1.0 : 0.0
            });
        }

        private void LoadTrack(string fileName)
        {
            if (this.fmodSystem == null)
                return;

            if (!Path.HasExtension(fileName))
                fileName += ".wav";

            this.log.Info("Play track {0}", Path.GetFileName(fileName));

            var sound = this.fmodSystem.CreateStream(Path.Combine(this.trackPath, fileName), Mode.Default);

            if (this.currentTrkSound.HasValue)
                this.disposeList.Add(this.currentTrkSound);
            this.currentTrkSound = null;
            this.currentTrack = Path.GetFileName(fileName);
            this.currentTrkSound = sound;
        }

        private void PlayVideo(string fileName)
        {
            if (this.videoPlaying)
            {
                this.log.Warn("Already playing a video");
                return;
            }

            this.log.Info("Play video {0}", fileName);

            var processStart = new ProcessStartInfo
            {
                FileName = "/usr/bin/omxplayer",
                Arguments = "-o local -w -b -z --no-osd --no-keys " + Path.Combine(this.videoPath, fileName),
                CreateNoWindow = false,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            this.videoPlaying = true;
            var process = Process.Start(processStart);

            process.EnableRaisingEvents = true;
            process.Exited += (o, e) =>
            {
                this.videoPlaying = false;
                this.log.Info("Done playing video");

                SendMessage(new VideoFinished
                {
                    Id = fileName
                });

                process.Dispose();
            };
            /*
                        this.log.Warn("1");
                        var bus = DBus.Bus.System;
                        this.log.Warn("1.5");
                        this.log.Warn("2 {0}", bus.UniqueName);
                        var objPath = new DBus.ObjectPath("/org/mpris/MediaPlayer2");
                        this.log.Warn("3");
                        var dbusConnection = bus.GetObject<ITest>("org.mpris.MediaPlayer2.omxplayer", objPath);
                        this.log.Warn("4");

                        Thread.Sleep(5000);
                        dbusConnection.Quit();

                        bus.Close();*/
        }

        [DBus.Interface("omxplayer.root")]
        public interface ITest
        {
            void Quit();
        }

        private void PlayTrack()
        {
            if (this.fmodSystem == null)
                return;

            var sound = this.currentTrkSound;
            if (!sound.HasValue)
                // No sound loaded
                return;

            try
            {
                var chn = this.currentTrkChannel;
                if (chn.HasValue)
                {
                    // Make sure we reset this first so we can ignore the callback
                    this.currentTrkChannel = null;
                    chn?.Stop();
                }
            }
            catch (FmodInvalidHandleException)
            {
                // Ignore
            }

            var channel = this.fmodSystem.PlaySound(sound.Value, null, true);
            string trackName = this.currentTrack;

            channel.SetCallback((type, data1, data2) =>
            {
                if (type == ChannelControlCallbackType.End)
                {
                    this.log.Debug("Track {0} ended", trackName);

                    SendMessage(new AudioFinished
                    {
                        Id = trackName,
                        Type = AudioTypes.Track
                    });
                }
            });

            this.currentTrkChannel = channel;

            // Play
            channel.Pause = false;

            // Send status message
            SendMessage(new AudioStarted
            {
                Id = trackName,
                Type = AudioTypes.Track
            });
        }

        private void PlayNextBackground()
        {
            if (this.fmodSystem == null)
                return;

            // Find next background track
            if (this.backgroundAudioTracks.Count == 0)
                // No tracks
                return;

            int index;
            while (true)
            {
                index = this.random.Next(this.backgroundAudioTracks.Count - 1);
                if (this.backgroundAudioTracks.Count > 1 && this.currentBgTrack == index)
                    continue;
                break;
            }
            this.currentBgTrack = index;
            string fileName = this.backgroundAudioTracks[index];
            this.currentBgTrackName = Path.GetFileName(fileName);

            this.log.Info("Play background track {0}", Path.GetFileName(fileName));

            var sound = this.fmodSystem.CreateStream(fileName, Mode.Default);

            try
            {
                var chn = this.currentBgChannel;
                if (chn.HasValue)
                {
                    // Make sure we reset this first so we can ignore the callback
                    this.currentBgChannel = null;
                    chn?.Stop();
                }
            }
            catch (FmodInvalidHandleException)
            {
                // Ignore
            }

            if (this.currentBgSound.HasValue)
                this.disposeList.Add(this.currentBgSound);
            this.currentBgSound = null;

            var channel = this.fmodSystem.PlaySound(sound, this.bgGroup, true);

            string bgName = this.currentBgTrackName;
            channel.SetCallback((type, data1, data2) =>
            {
                if (type == ChannelControlCallbackType.End)
                {
                    this.log.Debug("Background {0} ended", bgName);

                    SendMessage(new AudioFinished
                    {
                        Id = bgName,
                        Type = AudioTypes.Background
                    });

                    if (this.currentBgChannel.HasValue)
                        PlayNextBackground();
                }
            });

            this.currentBgSound = sound;
            this.currentBgChannel = channel;

            // Play
            channel.Pause = false;

            SendMessage(new AudioStarted
            {
                Id = bgName,
                Type = AudioTypes.Background
            });

        }

        private Sound LoadSound(string fileName)
        {
            if (this.fmodSystem == null)
                return new Sound();

            if (!Path.HasExtension(fileName))
                fileName += ".wav";

            Sound sound;
            if (!this.loadedSounds.TryGetValue(fileName, out sound))
            {
                // Load
                sound = this.fmodSystem.CreateSound(Path.Combine(this.soundEffectPath, fileName), Mode.Default);

                this.loadedSounds.Add(fileName, sound);
            }

            return sound;
        }

        private void PlaySound(string fileName, bool playOnNewChannel, double leftVol = 1.0, double? rightVol = null)
        {
            if (this.fmodSystem == null)
                return;

            var sound = LoadSound(fileName);

            if (!playOnNewChannel)
            {
                try
                {
                    this.currentFxChannel?.Stop();
                }
                catch (FmodInvalidHandleException)
                {
                    // Ignore
                }
            }

            var channel = this.fmodSystem.PlaySound(sound, this.fxGroup, true);

            if (!rightVol.HasValue)
                rightVol = leftVol;

            channel.FmodChannel.setMixLevelsOutput((float)leftVol, (float)rightVol.Value, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

            // Play
            channel.Pause = false;

            if (!playOnNewChannel)
                this.currentFxChannel = channel;
        }

        public void Dispose()
        {
            if (this.system != null)
            {
                this.system.Dispose();
                this.system = null;
            }

            if (this.fmodSystem != null)
                this.fmodSystem.Dispose();
        }

        private bool ReportChannelPosition(Channel? channel, string trackId, AudioTypes audioType, ref int? lastPos)
        {
            if (channel.HasValue)
            {
                var chn = channel.Value;

                int? pos = (int?)chn.GetPosition(TimeUnit.Milliseconds);

                if (pos == lastPos)
                    return false;

                lastPos = pos;

                if (pos.HasValue)
                {
                    SendMessage(new AudioPositionChanged
                    {
                        Id = trackId,
                        Type = audioType,
                        Position = pos.Value
                    });

                    return true;
                }
            }

            lastPos = null;

            return false;
        }

        public void Execute(CancellationToken cancel)
        {
            try
            {
                this.log.Info("Starting up listeners, etc");

                this.log.Info("Running");

                if (this.autoStartBackgroundTrack)
                    PlayNextBackground();

                var watch = Stopwatch.StartNew();
                int reportCounter = 0;
                while (!cancel.IsCancellationRequested)
                {
                    if (this.piFace != null)
                        this.piFace.PollInputPins();

                    if (this.fmodSystem != null)
                        this.fmodSystem.Update();

                    this.disposeList.ForEach(x => x.Dispose());
                    this.disposeList.Clear();

                    reportCounter++;

                    if (reportCounter % 5 == 0)
                    {
                        if (ReportChannelPosition(this.currentTrkChannel, this.currentTrack, AudioTypes.Track, ref this.lastPosTrk))
                            watch.Restart();
                    }

                    if (reportCounter % 10 == 0)
                    {
                        if (ReportChannelPosition(this.currentBgChannel, this.currentBgTrackName, AudioTypes.Background, ref this.lastPosBg))
                            watch.Restart();
                    }

                    if (reportCounter % 10 == 0 && watch.ElapsedMilliseconds > 5000)
                    {
                        // Send ping
                        this.log.Trace("Send ping");

                        SendMessage(new WhoAreYouResponse
                        {
                            InstanceId = InstanceId
                        });

                        watch.Restart();
                    }

                    Thread.Sleep(50);
                }

                this.log.Info("Shutting down");

                this.clientActor.GracefulStop(TimeSpan.FromSeconds(1));
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }
    }
}
