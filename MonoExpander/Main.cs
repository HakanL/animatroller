#define DEBUG_OSC
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
using Rug.Osc;
using SupersonicSound.Exceptions;
using System.Diagnostics;
using org.freedesktop.DBus;

namespace Animatroller.MonoExpander
{
    public class Main : IDisposable
    {
        public enum VideoSystems
        {
            None,
            OMX
        }

        private Logger log;
        private Rug.Osc.OscReceiver receiver;
        private List<Rug.Osc.OscSender> senders;
        private PiFaceDigitalDevice piFace;
        private SupersonicSound.LowLevel.LowLevelSystem fmodSystem;
        private Task receiverTask;
        private CancellationTokenSource cancelSource;
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

        public Main(Arguments args)
        {
            this.log = LogManager.GetLogger("Main");

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

                var config = LogManager.Configuration;
                var consoleTargets = new List<string>();
                consoleTargets.AddRange(config.AllTargets
                    .OfType<NLog.Targets.ColoredConsoleTarget>()
                    .Select(x => x.Name));
                consoleTargets.AddRange(config.AllTargets
                    .OfType<NLog.Targets.ConsoleTarget>()
                    .Select(x => x.Name));
                foreach (var loggingRule in config.LoggingRules)
                {
                    loggingRule.Targets
                        .Where(x => consoleTargets.Contains(x.Name) || consoleTargets.Contains(x.Name + "_wrapped"))
                        .ToList()
                        .ForEach(x => loggingRule.Targets.Remove(x));
                }
                LogManager.Configuration = config;

                Console.CursorVisible = false;
                Console.Clear();
            }

            this.loadedSounds = new Dictionary<string, Sound>();
            this.currentBgTrack = -1;
            this.random = new Random();
            this.disposeList = new List<IDisposable>();

            if (string.IsNullOrEmpty(args.SoundEffectPath))
                this.soundEffectPath = Directory.GetCurrentDirectory();
            else
                this.soundEffectPath = Path.GetFullPath(args.SoundEffectPath);
            if (string.IsNullOrEmpty(args.TrackPath))
                this.trackPath = Directory.GetCurrentDirectory();
            else
                this.trackPath = Path.GetFullPath(args.TrackPath);
            if (string.IsNullOrEmpty(args.VideoPath))
                this.videoPath = Directory.GetCurrentDirectory();
            else
                this.videoPath = Path.GetFullPath(args.VideoPath);
            this.autoStartBackgroundTrack = args.BackgroundTrackAutoStart;

            // Try to read instance id from disk
            string tempPath = Path.GetTempPath();
            try
            {
                using (var f = File.OpenText(Path.Combine(tempPath, "MonoExpander_InstanceId.txt")))
                {
                    this.instanceId = f.ReadLine();
                }
            }
            catch
            {
                // Generate new
                this.instanceId = Guid.NewGuid().ToString("n");

                using (var f = File.CreateText(Path.Combine(tempPath, "MonoExpander_InstanceId.txt")))
                {
                    f.WriteLine(this.instanceId);
                    f.Flush();
                }
            }

            this.log.Info("Instance Id {0}", this.instanceId);
            this.log.Info("Video Path {0}", this.videoPath);
            this.log.Info("Track Path {0}", this.trackPath);
            this.log.Info("FX Path {0}", this.soundEffectPath);

            this.senders = new List<Rug.Osc.OscSender>();
            foreach (var endpoint in args.OscServers)
            {
                senders.Add(new Rug.Osc.OscSender(endpoint.Address, 0, endpoint.Port));
            }

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

            this.log.Info("Initializing OSC listener");
            this.receiver = new Rug.Osc.OscReceiver(args.OscListenPort);

            this.cancelSource = new System.Threading.CancellationTokenSource();

            this.receiverTask = new Task(x =>
            {
                this.log.Debug("Starting up Receive Task");

                try
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        while (this.receiver.State != Rug.Osc.OscSocketState.Closed)
                        {
                            if (this.receiver.State == Rug.Osc.OscSocketState.Connected)
                            {
                                var packet = this.receiver.Receive();
#if DEBUG_OSC
                                this.log.Debug("Received OSC message: {0}", packet);
#endif

                                if (packet is Rug.Osc.OscBundle)
                                {
                                    var bundles = (Rug.Osc.OscBundle)packet;
                                    bundles
                                        .OfType<Rug.Osc.OscMessage>()
                                        .ToList()
                                        .ForEach(msg => InvokeOSC(msg));
                                }

                                if (packet is Rug.Osc.OscMessage)
                                {
                                    var msg = (Rug.Osc.OscMessage)packet;
                                    InvokeOSC(msg);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == "The receiver socket has been disconnected")
                        // Ignore
                        return;

                    this.log.Error(ex, "Unhandled exception in Receive Task");
                }

                this.log.Debug("Closed down Receive Task");
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);
        }

        private void SendInputMessage(int input, bool state)
        {
            this.log.Debug("Input {0} set to {1}", input, state ? 1 : 0);

            this.senders.ForEach(x =>
            {
                var msg = new OscMessage("/input",
                    this.instanceId,
                    input,
                    state ? 1 : 0);

                x.Send(msg);
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

                this.senders.ForEach(x => x.Send(new OscMessage("/video/done", this.instanceId, fileName)));

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

                    this.senders.ForEach(x => x.Send(new OscMessage("/audio/trk/done", this.instanceId, trackName)));
                }
            });

            this.currentTrkChannel = channel;

            // Play
            channel.Pause = false;

            // Send OSC message
            senders.ForEach(x => x.Send(new OscMessage("/audio/trk/start", this.instanceId, trackName)));
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

                    this.senders.ForEach(x => x.Send(new OscMessage("/audio/bg/done", this.instanceId, bgName)));

                    if (this.currentBgChannel.HasValue)
                        PlayNextBackground();
                }
            });

            this.currentBgSound = sound;
            this.currentBgChannel = channel;

            // Play
            channel.Pause = false;

            // Send OSC message
            senders.ForEach(x => x.Send(new OscMessage("/audio/bg/start", this.instanceId, bgName)));
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

        private void PlaySound(string fileName, bool playOnNewChannel, float leftVol = 1.0f, float? rightVol = null)
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

            channel.FmodChannel.setMixLevelsOutput(leftVol, rightVol.Value, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

            // Play
            channel.Pause = false;

            if (!playOnNewChannel)
                this.currentFxChannel = channel;
        }

        private void InvokeOSC(Rug.Osc.OscMessage msg)
        {
            try
            {
                switch (msg.Address)
                {
                    case "/test":
                        this.log.Info("Received OSC test message {0}", string.Join(",", msg));
                        break;

                    case "/init":
                        this.log.Info("Received OSC init message {0}", string.Join(",", msg));
                        break;

                    case "/output":
                        switch (msg.Count)
                        {
                            case 2:
                                int output = (int)msg[0];
                                bool state;
                                if (msg[1] is bool)
                                    state = (bool)msg[1];
                                else
                                    state = (int)msg[1] != 0;
                                this.log.Info("Set output {0} to {1}", output, state);

                                if (this.piFace != null)
                                {
                                    this.piFace.OutputPins[output].State = state;
                                    this.piFace.UpdatePiFaceOutputPins();
                                }
                                break;

                            default:
                                throw new ArgumentException(string.Format("Missing argument for OSC message {0}", msg.Address));
                        }
                        break;

                    case "/audio/fx/cue":
                        switch (msg.Count)
                        {
                            case 1:
                                LoadSound((string)msg[0]);
                                break;

                            default:
                                throw new ArgumentException(string.Format("Missing argument for OSC message {0}", msg.Address));
                        }
                        break;

                    case "/audio/fx/play":
                    case "/audio/fx/playnew":
                        bool playOnNewChannel = msg.Address == "/audio/fx/playnew";
                        switch (msg.Count)
                        {
                            case 1:
                                PlaySound((string)msg[0], playOnNewChannel);
                                break;

                            case 2:
                                PlaySound((string)msg[0], playOnNewChannel, (float)msg[1]);
                                break;

                            case 3:
                                PlaySound((string)msg[0], playOnNewChannel, (float)msg[1], (float)msg[2]);
                                break;

                            default:
                                throw new ArgumentException(string.Format("Missing argument for OSC message {0}", msg.Address));
                        }

                        break;

                    case "/audio/fx/pause":
                        if (this.currentFxChannel.HasValue)
                        {
                            var chn = this.currentBgChannel.Value;
                            chn.Pause = true;
                        }
                        break;

                    case "/audio/fx/resume":
                        if (this.currentFxChannel.HasValue)
                        {
                            var chn = this.currentFxChannel.Value;
                            chn.Pause = false;
                        }
                        break;

                    case "/audio/fx/volume":
                        if (msg.Count == 0)
                            throw new ArgumentException("Missing volume argument");

                        this.fxGroup.Volume = (float)msg[0];
                        break;

                    case "/audio/bg/volume":
                        if (msg.Count == 0)
                            throw new ArgumentException("Missing volume argument");

                        this.bgGroup.Volume = (float)msg[0];
                        break;

                    case "/audio/bg/resume":
                    case "/audio/bg/play":
                        if (this.currentBgChannel.HasValue)
                        {
                            var chn = this.currentBgChannel.Value;
                            chn.Pause = false;
                        }
                        else
                            PlayNextBackground();
                        break;

                    case "/audio/bg/pause":
                        if (this.currentBgChannel.HasValue)
                        {
                            var chn = this.currentBgChannel.Value;
                            chn.Pause = true;
                        }
                        break;

                    case "/audio/bg/next":
                        PlayNextBackground();
                        break;

                    case "/audio/trk/play":
                        switch (msg.Count)
                        {
                            case 1:
                                LoadTrack((string)msg[0]);
                                PlayTrack();
                                break;

                            default:
                                throw new ArgumentException(string.Format("Missing argument for OSC message {0}", msg.Address));
                        }
                        break;

                    case "/audio/trk/cue":
                        switch (msg.Count)
                        {
                            case 1:
                                LoadTrack((string)msg[0]);
                                break;

                            default:
                                throw new ArgumentException(string.Format("Missing argument for OSC message {0}", msg.Address));
                        }
                        break;

                    case "/audio/trk/pause":
                        if (this.currentTrkChannel.HasValue)
                        {
                            var chn = this.currentTrkChannel.Value;
                            chn.Pause = true;
                        }
                        break;

                    case "/audio/trk/resume":
                        if (this.currentTrkChannel.HasValue)
                        {
                            var chn = this.currentTrkChannel.Value;
                            chn.Pause = false;
                        }
                        break;

                    case "/video/play":
                        switch (msg.Count)
                        {
                            case 1:
                                PlayVideo((string)msg[0]);
                                break;

                            default:
                                throw new ArgumentException(string.Format("Missing argument for OSC message {0}", msg.Address));
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                this.log.Error(ex, "Unhandled exception in InvokeOSC");
            }
        }

        public void Dispose()
        {
            this.receiver.Dispose();

            if (this.fmodSystem != null)
                this.fmodSystem.Dispose();
        }

        private bool ReportChannelPosition(Channel? channel, string oscAddress, string trackId)
        {
            if (channel.HasValue)
            {
                var chn = channel.Value;

                int? pos = (int?)chn.GetPosition(TimeUnit.Milliseconds);

                if (pos.HasValue)
                {
                    var msg = new OscMessage(oscAddress, this.instanceId, trackId, pos.Value);
                    this.senders.ForEach(x => x.Send(msg));

                    return true;
                }
            }

            return false;
        }

        public void Execute(CancellationToken cancel)
        {
            try
            {
                this.log.Info("Starting up listeners, etc");

                this.receiver.Connect();
                this.receiverTask.Start();

                this.senders.ForEach(x =>
                {
                    x.Connect();

                    x.Send(new OscMessage("/init", this.instanceId));
                });

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
                        if (ReportChannelPosition(this.currentTrkChannel, "/audio/trk/pos", this.currentTrack))
                            watch.Restart();
                    }

                    if (reportCounter % 10 == 0)
                    {
                        if (ReportChannelPosition(this.currentBgChannel, "/audio/bg/pos", this.currentBgTrackName))
                            watch.Restart();
                    }

                    if (reportCounter % 10 == 0 && watch.ElapsedMilliseconds > 5000)
                    {
                        // Send ping
                        this.log.Trace("Send ping");

                        this.senders.ForEach(x => x.Send(new OscMessage("/ping")));

                        watch.Restart();
                    }

                    Thread.Sleep(50);
                }

                this.log.Info("Shutting down");

                this.senders.ForEach(x => x.Close());
                this.cancelSource.Cancel();
                this.receiver.Close();
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }
    }
}
