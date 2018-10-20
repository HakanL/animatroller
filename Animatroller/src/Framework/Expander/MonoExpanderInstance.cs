using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Animatroller.Framework.MonoExpanderMessages;

namespace Animatroller.Framework.Expander
{
    public class MonoExpanderInstance : MonoExpanderBaseInstance, IPort, IRunnable, IOutputHardware
    {
        private event EventHandler<EventArgs> AudioTrackDone;
        private event EventHandler<EventArgs> VideoTrackDone;
        private readonly ISubject<Tuple<AudioTypes, string>> audioTrackStart;
        private readonly ISubject<DiagData> diagnostics;

        public MonoExpanderInstance(int inputs = 8, int outputs = 8, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;

            this.DigitalInputs = new PhysicalDevice.DigitalInput[inputs];
            for (int index = 0; index < this.DigitalInputs.Length; index++)
                this.DigitalInputs[index] = new PhysicalDevice.DigitalInput();

            this.DigitalOutputs = new PhysicalDevice.DigitalOutput[outputs];
            for (int index = 0; index < this.DigitalOutputs.Length; index++)
                WireupOutput(index);

            this.audioTrackStart = new Subject<Tuple<AudioTypes, string>>();

            this.Motor = new PhysicalDevice.MotorWithFeedback((target, speed, timeout) =>
            {
                //                this.oscClient.Send("/motor/exec", 1, target, (int)(speed * 100), timeout.TotalSeconds.ToString("F0"));
            });

            Executor.Current.Register(this);
        }

        public IObservable<Tuple<AudioTypes, string>> AudioTrackStart => this.audioTrackStart.AsObservable();

        public PhysicalDevice.DigitalInput[] DigitalInputs { get; private set; }

        public PhysicalDevice.DigitalOutput[] DigitalOutputs { get; private set; }

        public PhysicalDevice.MotorWithFeedback Motor { get; private set; }

        protected virtual void RaiseAudioTrackDone()
        {
            AudioTrackDone?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void RaiseVideoTrackDone()
        {
            VideoTrackDone?.Invoke(this, EventArgs.Empty);
        }

        public void Start()
        {
            // Send initial
            for (int index = 0; index < DigitalInputs.Length; index++)
            {
                Executor.Current.Diagnostics.OnNext(new DiagDataPortStatus
                {
                    Name = this.name,
                    Port = index,
                    Value = false
                });
            }

            Executor.Current.Diagnostics.OnNext(new DiagDataAudioPlayback
            {
                Type = AudioTypes.Effect.ToString(),
                Name = this.name,
                Value = "-"
            });

            Executor.Current.Diagnostics.OnNext(new DiagDataAudioPlayback
            {
                Type = AudioTypes.Track.ToString(),
                Name = this.name,
                Value = "-"
            });

            Executor.Current.Diagnostics.OnNext(new DiagDataAudioPlayback
            {
                Type = AudioTypes.Background.ToString(),
                Name = this.name,
                Value = "-"
            });
        }

        public void Stop()
        {
        }

        private void WireupOutput(int index)
        {
            this.DigitalOutputs[index] = new PhysicalDevice.DigitalOutput(x =>
            {
                SendMessage(new SetOutputRequest
                {
                    Output = string.Format("d{0}", index),
                    Value = x ? 1.0 : 0.0
                }, $"d-out{index}");
            });
        }

        public MonoExpanderInstance Connect(LogicalDevice.AudioPlayer logicalDevice)
        {
            this.AudioTrackDone += (o, e) =>
                {
                    logicalDevice.RaiseAudioTrackDone();
                };

            this.AudioTrackStart.Subscribe(x =>
                {
                    if (x.Item1 == AudioTypes.Track)
                        logicalDevice.RaiseAudioTrackStart(x.Item2);
                });

            logicalDevice.AudioChanged += (sender, e) =>
                {
                    switch (e.Command)
                    {
                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX:
                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayFX:
                            if (e.LeftVolume.HasValue && e.RightVolume.HasValue)
                                SendMessage(new AudioEffectPlay
                                {
                                    FileName = e.AudioFile,
                                    VolumeLeft = e.LeftVolume.Value,
                                    VolumeRight = e.RightVolume.Value,
                                    Simultaneous = e.Command == LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX
                                });
                            else
                                SendMessage(new AudioEffectPlay
                                {
                                    FileName = e.AudioFile,
                                    Simultaneous = e.Command == LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX
                                });
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueFX:
                            SendMessage(new AudioEffectCue
                            {
                                FileName = e.AudioFile
                            });
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueTrack:
                            SendMessage(new AudioTrackCue
                            {
                                FileName = e.AudioFile
                            });
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayTrack:
                            SendMessage(new AudioTrackPlay
                            {
                                FileName = e.AudioFile
                            });
                            break;
                    }
                };

            logicalDevice.ExecuteCommand += (sender, e) =>
                {
                    switch (e.Command)
                    {
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PlayBackground:
                            SendMessage(new AudioBackgroundResume(), "play-bg");
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseBackground:
                            SendMessage(new AudioBackgroundPause(), "pause-bg");
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeFX:
                            SendMessage(new AudioEffectResume());
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseFX:
                            SendMessage(new AudioEffectPause());
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.NextBackground:
                            SendMessage(new AudioBackgroundNext());
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.BackgroundVolume:
                            SendMessage(new AudioBackgroundSetVolume
                            {
                                Volume = ((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value
                            }, "mv-bg");
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeTrack:
                            SendMessage(new AudioTrackResume());
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseTrack:
                            SendMessage(new AudioTrackPause());
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.EffectVolume:
                            SendMessage(new AudioEffectSetVolume
                            {
                                Volume = ((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value
                            }, "mv-fx");
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.TrackVolume:
                            SendMessage(new AudioTrackSetVolume
                            {
                                Volume = ((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value
                            }, "mv-trk");
                            break;
                    }
                };

            return this;
        }

        public MonoExpanderInstance Connect(LogicalDevice.VideoPlayer logicalDevice)
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
                        SendMessage(new VideoPlay
                        {
                            FileName = e.VideoFile
                        });
                        break;
                }
            };

            return this;
        }

        public void Handle(InputChanged message)
        {
            this.log.Information("Input {0} on {1} set to {2}", message.Input, this.name, message.Value);

            if (message.Input.StartsWith("d"))
            {
                int inputId;
                if (int.TryParse(message.Input.Substring(1), out inputId))
                {
                    if (inputId >= 0 && inputId <= 7)
                    {
                        this.DigitalInputs[inputId].Trigger(message.Value != 0.0);
                        Executor.Current.Diagnostics.OnNext(new DiagDataPortStatus
                        {
                            Name = this.name,
                            Port = inputId,
                            Value = message.Value != 0.0
                        });
                    }
                }
            }
        }

        public void Handle(AudioPositionChanged message)
        {
        }

        public void Handle(VideoPositionChanged message)
        {
        }

        public void Handle(VideoStarted message)
        {
        }

        public void Handle(VideoFinished message)
        {
            log.Debug("Video {0} done", message.Id);
            RaiseVideoTrackDone();
        }

        public void Handle(AudioStarted message)
        {
            switch (message.Type)
            {
                case AudioTypes.Track:
                case AudioTypes.Background:
                    log.Debug("Playing {0} track {1} on {2}", message.Type, message.Id, this.name);

                    this.audioTrackStart.OnNext(Tuple.Create(message.Type, message.Id));
                    break;
            }

            Executor.Current.Diagnostics.OnNext(new DiagDataAudioPlayback
            {
                Type = message.Type == AudioTypes.Effect ? message.Type.ToString() : AudioTypes.Track.ToString(),
                Name = this.name,
                Value = message.Id
            });
        }

        public void Handle(AudioFinished message)
        {
            switch (message.Type)
            {
                case AudioTypes.Track:
                    log.Debug("Audio track {0} done", message.Id);
                    RaiseAudioTrackDone();
                    break;
            }

            Executor.Current.Diagnostics.OnNext(new DiagDataAudioPlayback
            {
                Type = message.Type == AudioTypes.Effect ? message.Type.ToString() : AudioTypes.Track.ToString(),
                Name = this.name,
                Value = "-"
            });
        }

        public void SendSerial(int port, byte[] data)
        {
            log.Debug("Send serial data to port {0}", port);

            SendMessage(new SendSerialRequest
            {
                Port = port,
                Data = data
            });
        }
    }
}
