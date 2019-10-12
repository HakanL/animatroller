#define NETTY
//#define DEBUG_VERBOSE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Animatroller.ExpanderCommunication;
using Animatroller.Framework.MonoExpanderMessages;
using Newtonsoft.Json;
using Raspberry.IO.Components.Devices.PiFaceDigital;
using Serilog;
using SupersonicSound.Exceptions;
using SupersonicSound.LowLevel;

namespace Animatroller.MonoExpander
{
    public partial class Main : IDisposable
    {
        public enum VideoSystems
        {
            None,
            OMX
        }

        private ILogger log;
        private PiFaceDigitalDevice piFace;
        private IDictionary<int, AudioSystem> audioSystems;
        private string instanceId;
        private string soundEffectPath;
        private string trackPath;
        private string videoPath;
        private VideoSystems videoSystem;
        private bool videoPlaying;
        private string fileStoragePath;
        private List<Tuple<IClientCommunication, MonoExpanderClient>> connections;
        private Dictionary<int, SerialPort> serialPorts;
        private string version;

        public Main(Arguments args)
        {
            this.log = Log.Logger;
            this.fileStoragePath = args.FileStoragePath;
            this.version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            this.audioSystems = new Dictionary<int, AudioSystem>();

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
                this.log.Information("Video System, turning off console logging and cursor");

                //FIXME
                //var logConfig = LogManager.Configuration;
                //var consoleTargets = new List<string>();
                //consoleTargets.AddRange(logConfig.AllTargets
                //    .OfType<NLog.Targets.ColoredConsoleTarget>()
                //    .Select(x => x.Name));
                //consoleTargets.AddRange(logConfig.AllTargets
                //    .OfType<NLog.Targets.ConsoleTarget>()
                //    .Select(x => x.Name));
                //foreach (var loggingRule in logConfig.LoggingRules)
                //{
                //    loggingRule.Targets
                //        .Where(x => consoleTargets.Contains(x.Name) || consoleTargets.Contains(x.Name + "_wrapped"))
                //        .ToList()
                //        .ForEach(x => loggingRule.Targets.Remove(x));
                //}
                //LogManager.Configuration = logConfig;

                Console.CursorVisible = false;
                Console.Clear();
            }

            this.serialPorts = new Dictionary<int, SerialPort>();

            string fileStoragePath = Path.GetFullPath(args.FileStoragePath);
            Directory.CreateDirectory(fileStoragePath);

            this.soundEffectPath = Path.Combine(fileStoragePath, FileTypes.AudioEffect.ToString());
            this.trackPath = Path.Combine(fileStoragePath, FileTypes.AudioTrack.ToString());
            this.videoPath = Path.Combine(fileStoragePath, FileTypes.Video.ToString());

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

            this.log.Information("Instance Id {0}", this.instanceId);
            this.log.Information("Video Path {0}", this.videoPath);
            this.log.Information("Track Path {0}", this.trackPath);
            this.log.Information("FX Path {0}", this.soundEffectPath);

            var backgroundAudioTracks = new List<string>();
            if (!string.IsNullOrEmpty(args.BackgroundTracksPath))
            {
                backgroundAudioTracks.AddRange(Directory.GetFiles(args.BackgroundTracksPath, "*.wav"));
                backgroundAudioTracks.AddRange(Directory.GetFiles(args.BackgroundTracksPath, "*.mp3"));
            }

            if (args.AudioSystem)
            {
                this.log.Information("Initializing FMOD sound system");

                for (int id = 0; id < args.AudioDriver.Length; id++)
                {
                    try
                    {
                        var audioSystem = new AudioSystem(
                            this.log,
                            args.AudioDriver[id],
                            id,
                            args.BackgroundTrackAutoStart,
                            backgroundAudioTracks,
                            SendMessage);

                        this.audioSystems.Add(id, audioSystem);
                    }
                    catch (Exception ex)
                    {
                        this.log.Error(ex, "Failed to initialize audio system with name {AudioDriver}", args.AudioDriver[id]);
                    }
                }
            }

            if (SupersonicSound.Wrapper.Util.IsUnix)
            {
                this.log.Information("Initializing PiFace");

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
                    }

                    SendInputStatus();
                }
                catch (Exception ex)
                {
                    this.log.Warning(ex, "Failed to initialize PiFace");
                }
            }

            if (!string.IsNullOrEmpty(args.SerialPort0) && args.SerialPort0BaudRate > 0)
            {
                this.log.Information("Initialize serial port 0 ({0}) for {1} bps", args.SerialPort0, args.SerialPort0BaudRate);

                var serialPort = new SerialPort(args.SerialPort0, args.SerialPort0BaudRate);

                serialPort.Open();

                this.serialPorts.Add(0, serialPort);
            }

            this.log.Information("Initializing ExpanderCommunication client");

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
                    logger: this.log,
                    host: server.Host,
                    port: server.Port,
                    instanceId: InstanceId,
                    dataReceivedAction: (t, d) => DataReceived(client, t, d),
                    connectedAction: () =>
                    {
                        SendPing();
                        SendInputStatus();
                    });
#endif
                this.connections.Add(Tuple.Create((IClientCommunication)communication, client));

                Task.Run(async () => await communication.StartAsync()).Wait();
            }
        }

        private void ExecuteAudioSystemCommand(int output, Action<AudioSystem> action)
        {
            if (this.audioSystems.TryGetValue(output, out var audioSystem))
            {
                action(audioSystem);
            }
        }

        private void SendInputStatus()
        {
            if (this.piFace != null)
            {
                foreach (var ip in this.piFace.InputPins)
                {
                    SendInputMessage(ip.Id, ip.State);
                }
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
            if (this.connections == null)
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

        private void PlayVideo(string fileName)
        {
            if (this.videoPlaying)
            {
                this.log.Warning("Already playing a video");
                return;
            }

            this.log.Information("Play video {0}", fileName);

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
                this.log.Information("Done playing video");

                SendMessage(new VideoFinished
                {
                    Id = fileName
                });

                process.Dispose();
            };
        }

        public void Dispose()
        {
            this.connections.ForEach(tt =>
            {
                Task.Run(async () => await tt.Item1.StopAsync()).Wait();
            });
            this.connections.Clear();

            foreach (var audioSystem in this.audioSystems.Values)
                audioSystem.Dispose();
            this.audioSystems.Clear();

            foreach (var serialPort in this.serialPorts.Values)
                serialPort.Close();
        }

        private void SendPing()
        {
            SendMessage(new Ping
            {
                HostName = Environment.MachineName,
                Version = version,
                Inputs = this.piFace?.InputPins.Length ?? 0,
                Outputs = this.piFace?.OutputPins.Length ?? 0
            });
        }

        public void Execute(CancellationToken cancel)
        {
            try
            {
                this.log.Information("Starting up listeners, etc");

                this.log.Information("Running version {Version} on {HostName}", this.version, Environment.MachineName);

                foreach (var kvp in this.audioSystems)
                    kvp.Value.Start();

                var watch = Stopwatch.StartNew();
                int reportCounter = 0;
                while (!cancel.IsCancellationRequested)
                {
                    if (this.piFace != null)
                        this.piFace.PollInputPins();

                    foreach (var kvp in this.audioSystems)
                        kvp.Value.Update();

                    if (reportCounter % 10 == 0 && watch.ElapsedMilliseconds > 5000)
                    {
                        // Send ping
                        this.log.Verbose("Send ping");

                        SendPing();

                        watch.Restart();
                    }

                    Thread.Sleep(50);
                }

                this.log.Information("Shutting down");
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }
    }
}
