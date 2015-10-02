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

namespace Animatroller.MonoExpander
{
    public class Main : IDisposable
    {
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

        public Main(Arguments args)
        {
            this.log = LogManager.GetLogger("Main");

            this.loadedSounds = new Dictionary<string, Sound>();
            this.currentBgTrack = -1;
            this.random = new Random();

            if (string.IsNullOrEmpty(args.SoundEffectPath))
                this.soundEffectPath = Directory.GetCurrentDirectory();
            else
                this.soundEffectPath = Path.GetFullPath(args.SoundEffectPath);
            if (string.IsNullOrEmpty(args.TrackPath))
                this.trackPath = Directory.GetCurrentDirectory();
            else
                this.trackPath = Path.GetFullPath(args.TrackPath);
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

            this.log.Info("Initializing FMOD sound system");
            this.fmodSystem = new LowLevelSystem();

            this.fxGroup = this.fmodSystem.CreateChannelGroup("FX");
            this.bgGroup = this.fmodSystem.CreateChannelGroup("Background");

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
            if (!Path.HasExtension(fileName))
                fileName += ".wav";

            this.log.Info("Play track {0}", Path.GetFileName(fileName));

            var sound = fmodSystem.CreateStream(Path.Combine(this.trackPath, fileName), Mode.Default);

            this.currentTrkSound?.Dispose();
            this.currentTrkSound = null;
            this.currentTrack = Path.GetFileName(fileName);
            this.currentTrkSound = sound;
        }

        private void PlayTrack()
        {
            var sound = this.currentTrkSound;
            if (!sound.HasValue)
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

            channel.SetCallback((type, data1, data2) =>
            {
                if (type == ChannelControlCallbackType.End)
                {
                    this.senders.ForEach(x => x.Send(new OscMessage("/audio/trk/done", this.instanceId)));
                }
            });

            this.currentTrkChannel = channel;

            // Play
            channel.Pause = false;

            // Send OSC message
            senders.ForEach(x => x.Send(new OscMessage("/audio/trk/start", this.instanceId, this.currentTrack)));
        }

        private void PlayNextBackground()
        {
            // Find next background track
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

            this.currentBgSound?.Dispose();
            this.currentBgSound = null;

            var channel = this.fmodSystem.PlaySound(sound, this.bgGroup, true);

            channel.SetCallback((type, data1, data2) =>
            {
                if (type == ChannelControlCallbackType.End)
                {
                    if (this.currentBgChannel.HasValue)
                        PlayNextBackground();
                }
            });

            this.currentBgSound = sound;
            this.currentBgChannel = channel;

            // Play
            channel.Pause = false;

            // Send OSC message
            senders.ForEach(x => x.Send(new OscMessage("/audio/bg/start", this.instanceId, Path.GetFileName(fileName))));
        }

        private Sound LoadSound(string fileName)
        {
            if (!Path.HasExtension(fileName))
                fileName += ".wav";

            Sound sound;
            if (!this.loadedSounds.TryGetValue(fileName, out sound))
            {
                // Load
                sound = fmodSystem.CreateSound(Path.Combine(this.soundEffectPath, fileName), Mode.Default);

                this.loadedSounds.Add(fileName, sound);
            }

            return sound;
        }

        private void PlaySound(string fileName, bool playOnNewChannel, float leftVol = 1.0f, float? rightVol = null)
        {
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
                                bool state = (bool)msg[1];
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
            this.fmodSystem.Dispose();
        }

        private void ReportChannelPosition(Channel? channel, string oscAddress, string trackId)
        {
            if (channel.HasValue)
            {
                var chn = channel.Value;

                try
                {
                    if (chn.IsPlaying)
                    {
                        int pos = (int)chn.GetPosition(TimeUnit.Milliseconds);

                        var msg = new OscMessage(oscAddress, this.instanceId, trackId, pos);
                        this.senders.ForEach(x => x.Send(msg));
                    }
                }
                catch (FmodInvalidHandleException)
                {
                    // Ignore
                }
            }
        }

        public void Execute(CancellationToken cancel)
        {
            this.log.Info("Starting up listeners, etc");

            this.receiverTask.Start();
            this.receiver.Connect();

            this.senders.ForEach(x =>
            {
                x.Connect();

                x.Send(new OscMessage("/init", this.instanceId));
            });

            this.log.Info("Running");

            if (this.autoStartBackgroundTrack)
                PlayNextBackground();

            int reportCounter = 0;
            while (!cancel.IsCancellationRequested)
            {
                if (this.piFace != null)
                    this.piFace.PollInputPins();

                this.fmodSystem.Update();

                reportCounter++;

                if (reportCounter % 5 == 0)
                {
                    ReportChannelPosition(this.currentTrkChannel, "/audio/trk/pos", this.currentTrack);
                }

                if (reportCounter % 10 == 0)
                {
                    ReportChannelPosition(this.currentBgChannel, "/audio/bg/pos", this.currentBgTrackName);
                }

                Thread.Sleep(50);
            }

            this.log.Info("Shutting down");

            this.senders.ForEach(x => x.Close());
            this.cancelSource.Cancel();
            this.receiver.Close();
        }
    }
}
