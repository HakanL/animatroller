using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Configuration;
using NLog;

namespace Animatroller.Framework.Expander
{
    public class MonoExpander : IPort, IRunnable, IOutputHardware
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private OscClient oscClient;
        private OscServer oscServer;
        private string hostName;
        private int hostPort;
        private event EventHandler<EventArgs> AudioTrackDone;
        private event EventHandler<EventArgs> VideoTrackDone;
        private ISubject<string> audioTrackStart;
        private List<string> lastMessageIds = new List<string>();

        public MonoExpander([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Initialize(
                hostEntry: Executor.Current.GetSetKey(this, name + ".hostEntry", "127.0.0.1:5005"),
                listenPort: Executor.Current.GetSetKey(this, name + ".listenPort", 3333));
        }

        public MonoExpander(string hostEntry, int listenPort)
        {
            Initialize(hostEntry, listenPort);
        }

        private bool CheckIdempotence(OscServer.Message msg)
        {
            return true;

            // We don't have the message id in there any more...
            if (!msg.Data.Any())
                return true;

            string messageId = (string)msg.Data.First();
            log.Trace("Received message id {0}", messageId);

            if (this.lastMessageIds.Contains(messageId))
                return false;

            this.lastMessageIds.Add(messageId);
            if (this.lastMessageIds.Count > 5)
                this.lastMessageIds.RemoveAt(0);

            return true;
        }

        private void Initialize(string hostEntry, int listenPort)
        {
            var hostParts = hostEntry.Split(':');
            if (hostParts.Length != 2)
                throw new ArgumentException("Requires a host entry with this format [IP:port]");

            this.hostName = hostParts[0];
            this.hostPort = int.Parse(hostParts[1]);

            var ipHostEntry = System.Net.Dns.GetHostAddresses(this.hostName);
            this.oscClient = new OscClient(ipHostEntry.First(), this.hostPort);

            this.DigitalInputs = new PhysicalDevice.DigitalInput[8];
            for (int index = 0; index < this.DigitalInputs.Length; index++)
                this.DigitalInputs[index] = new PhysicalDevice.DigitalInput();

            this.DigitalOutputs = new PhysicalDevice.DigitalOutput[8];
            for (int index = 0; index < this.DigitalOutputs.Length; index++)
                WireupOutput(index);

            this.audioTrackStart = new Subject<string>();

            this.Motor = new PhysicalDevice.MotorWithFeedback((target, speed, timeout) =>
            {
                this.oscClient.Send("/motor/exec", 1, target, (int)(speed * 100), timeout.TotalSeconds.ToString("F0"));
            });

            this.oscServer = new OscServer(listenPort);
            this.oscServer.RegisterAction("/init", msg =>
                {
                    if (!CheckIdempotence(msg))
                        return;

                    log.Info("Raspberry is up");
                });

            this.oscServer.RegisterAction("/audio/trk/done", msg =>
                {
                    if (!CheckIdempotence(msg))
                        return;

                    log.Debug("Audio track done");
                    RaiseAudioTrackDone();
                });

            this.oscServer.RegisterAction("/video/done", msg =>
            {
                if (!CheckIdempotence(msg))
                    return;

                log.Debug("Video done");
                RaiseVideoTrackDone();
            });

            this.oscServer.RegisterAction<string>("/audio/bg/start", (msg, data) =>
            {
                if (!CheckIdempotence(msg))
                    return;

                if (data.Count() >= 2)
                {
                    string track = data.Skip(1).First();
                    log.Debug("Playing background track {0}", track);
                    this.audioTrackStart.OnNext(track);
                }
            });

            this.oscServer.RegisterAction<string, int>("/input", (msg, id, data) =>
                {
                    if (!CheckIdempotence(msg))
                        return;

                    if (data.Count() >= 2)
                    {
                        var values = data.ToArray();
                        log.Info("Input {0} set to {1}", values[0], values[1]);

                        if (values[0] >= 0 && values[0] <= 7)
                            this.DigitalInputs[values[0]].Trigger(values[1] != 0);
                    }
                });

            this.oscServer.RegisterAction("/motor/feedback", msg =>
                {
                    if (!CheckIdempotence(msg))
                        return;

                    if (msg.Data.Count() >= 2)
                    {
                        var values = msg.Data.ToArray();

                        int motorChn = int.Parse(values[0].ToString());
                        string motorPos = values[1].ToString();

                        if (motorPos == "FAIL")
                        {
                            log.Info("Motor {0} failed", motorChn);

                            if (motorChn == 1)
                                this.Motor.Trigger(null, true);
                        }
                        else
                        {
                            if (motorPos.StartsWith("S"))
                            {
                                int pos = int.Parse(motorPos.Substring(1));
                                log.Info("Motor {0} starting at position {1}", motorChn, pos);
                            }
                            else if (motorPos.StartsWith("E"))
                            {
                                int pos = int.Parse(motorPos.Substring(1));
                                log.Info("Motor {0} ending at position {1}", motorChn, pos);

                                if (motorChn == 1)
                                    this.Motor.Trigger(pos, false);
                            }
                            else
                            {
                                int pos = int.Parse(motorPos);
                                log.Debug("Motor {0} at position {1}", motorChn, pos);
                            }
                        }
                    }
                });

            this.DigitalInputs = new PhysicalDevice.DigitalInput[8];
            for (int index = 0; index < this.DigitalInputs.Length; index++)
                this.DigitalInputs[index] = new PhysicalDevice.DigitalInput();

            Executor.Current.Register(this);
        }

        public IObservable<string> AudioTrackStart
        {
            get
            {
                return this.audioTrackStart;
            }
        }

        public PhysicalDevice.DigitalInput[] DigitalInputs { get; private set; }
        public PhysicalDevice.DigitalOutput[] DigitalOutputs { get; private set; }
        public PhysicalDevice.MotorWithFeedback Motor { get; private set; }

        protected virtual void RaiseAudioTrackDone()
        {
            var handler = AudioTrackDone;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected virtual void RaiseVideoTrackDone()
        {
            var handler = VideoTrackDone;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public void Start()
        {
            this.oscClient.Send("/init");
        }

        public void Stop()
        {
        }

        private void WireupOutput(int index)
        {
            this.DigitalOutputs[index] = new PhysicalDevice.DigitalOutput(x =>
            {
                this.oscClient.Send("/output", index, x ? 1 : 0);
            });
        }

        public void Test(int value)
        {
            this.oscClient.Send("/test", value.ToString());
        }

        public MonoExpander Connect(LogicalDevice.AudioPlayer logicalDevice)
        {
            this.AudioTrackDone += (o, e) =>
                {
                    logicalDevice.RaiseAudioTrackDone();
                };

            logicalDevice.AudioChanged += (sender, e) =>
                {
                    switch (e.Command)
                    {
                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayFX:
                            if (e.LeftVolume.HasValue && e.RightVolume.HasValue)
                                this.oscClient.Send("/audio/fx/play", e.AudioFile, (float)e.LeftVolume.Value, (float)e.RightVolume.Value);
                            else if (e.LeftVolume.HasValue)
                                this.oscClient.Send("/audio/fx/play", e.AudioFile, (float)e.LeftVolume.Value);
                            else
                                this.oscClient.Send("/audio/fx/play", e.AudioFile);
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX:
                            if (e.LeftVolume.HasValue && e.RightVolume.HasValue)
                                this.oscClient.Send("/audio/fx/playnew", e.AudioFile, (float)e.LeftVolume.Value, (float)e.RightVolume.Value);
                            else if (e.LeftVolume.HasValue)
                                this.oscClient.Send("/audio/fx/playnew", e.AudioFile, (float)e.LeftVolume.Value);
                            else
                                this.oscClient.Send("/audio/fx/playnew", e.AudioFile);
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueFX:
                            this.oscClient.Send("/audio/fx/cue", e.AudioFile);
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueTrack:
                            this.oscClient.Send("/audio/trk/cue", e.AudioFile);
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayTrack:
                            this.oscClient.Send("/audio/trk/play", e.AudioFile);
                            break;
                    }
                };

            logicalDevice.ExecuteCommand += (sender, e) =>
                {
                    switch (e.Command)
                    {
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PlayBackground:
                            this.oscClient.Send("/audio/bg/play");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseBackground:
                            this.oscClient.Send("/audio/bg/pause");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeFX:
                            this.oscClient.Send("/audio/fx/resume");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseFX:
                            this.oscClient.Send("/audio/fx/pause");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.NextBackground:
                            this.oscClient.Send("/audio/bg/next");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.BackgroundVolume:
                            this.oscClient.Send("/audio/bg/volume", (float)((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value);
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeTrack:
                            this.oscClient.Send("/audio/trk/resume");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseTrack:
                            this.oscClient.Send("/audio/trk/pause");
                            break;
                    }
                };

            return this;
        }

        public MonoExpander Connect(LogicalDevice.VideoPlayer logicalDevice)
        {
            this.VideoTrackDone += (o, e) =>
            {
                logicalDevice.RaiseVideoTrackDone();
            };

            logicalDevice.ExecuteCommand += (sender, e) =>
            {
                switch (e.Command)
                {
                    case LogicalDevice.Event.VideoCommandEventArgs.Commands.PlayVideo:
                        this.oscClient.Send("/video/play", e.VideoFile);
                        break;
                }
            };

            return this;
        }
    }
}
