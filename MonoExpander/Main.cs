#define NETTY
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
using Animatroller.Framework.MonoExpanderMessages;
using Newtonsoft.Json;
using Animatroller.ExpanderCommunication;

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
        private string fileStoragePath;
        private List<Tuple<IClientCommunication, MonoExpanderClient>> connections;

        public Main(Arguments args)
        {
            this.log = LogManager.GetLogger("Main");
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
            Directory.CreateDirectory(fileStoragePath);

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

            this.log.Info("Initializing ExpanderCommunication client");

            this.connections = new List<Tuple<IClientCommunication, MonoExpanderClient>>();
            foreach (var server in args.Servers)
            {
                var client = new MonoExpanderClient(this);

#if SIGNALR
                var communication = new SignalRClient(
                    host: server.Host,
                    port: server.Port,
                    instanceId: InstanceId,
                    dataReceivedAction: (t, d) => DataReceived(client, t, d));
#endif
#if NETTY
                var communication = new NettyClient(
                    host: server.Host,
                    port: server.Port,
                    instanceId: InstanceId,
                    dataReceivedAction: (t, d) => DataReceived(client, t, d));
#endif
                this.connections.Add(Tuple.Create((IClientCommunication)communication, client));

                Task.Run(async () => await communication.StartAsync()).Wait();
            }
        }

        private void DataReceived(MonoExpanderClient client, string messageType, byte[] data)
        {
            client.HandleMessage(messageType, data);
        }

        public string FileStoragePath
        {
            get { return this.fileStoragePath; }
        }

        public string InstanceId
        {
            get { return this.instanceId; }
        }

        internal static void Serialize(object value, Stream s)
        {
            using (var writer = new StreamWriter(s))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                var ser = new JsonSerializer();
                ser.Serialize(jsonWriter, value);
                jsonWriter.Flush();
            }
        }

        internal static object DeserializeFromStream(Stream stream, Type messageType)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize(jsonTextReader, messageType);
            }
        }

        public void SendMessage(object message)
        {
            if (connections == null)
                return;

            this.connections.ForEach(tt =>
            {
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        Serialize(message, ms);

                        if (!Task.Run(async () => await tt.Item1.SendData(
                            messageType: message.GetType().FullName,
                            data: ms.ToArray())).Result)
                        {
                            this.log.Debug("Not connected to {0}", tt.Item1.Server);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ignore
                    this.log.Debug("Failed to send: {0}", ex.Message);
                }
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
            this.connections.ForEach(tt =>
            {
                Task.Run(async () => await tt.Item1.StopAsync()).Wait();
            });
            this.connections.Clear();

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

                        SendMessage(new Ping());

                        watch.Restart();
                    }

                    Thread.Sleep(50);
                }

                this.log.Info("Shutting down");
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }
    }
}
