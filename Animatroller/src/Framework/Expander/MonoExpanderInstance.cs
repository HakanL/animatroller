using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Animatroller.Framework.MonoExpanderMessages;

namespace Animatroller.Framework.Expander
{
    public class MonoExpanderInstance : MonoExpanderBaseInstance, IPort, IRunnable, IOutputHardware
    {
        public enum HardwareType
        {
            None,
            PiFace
        }

        private ISubject<int> audioTrackDone;
        private ISubject<int> videoTrackDone;
        private ISubject<(int Output, AudioTypes AudioType, string Filename)> audioTrackStart;

        public MonoExpanderInstance(HardwareType hardware, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            switch (hardware)
            {
                case HardwareType.None:
                    Init(name, 0, 0);
                    break;

                case HardwareType.PiFace:
                    Init(name, 8, 8);

                    // Default for the PiFace
                    InvertedInputs[0] = true;
                    InvertedInputs[1] = true;
                    InvertedInputs[2] = true;
                    InvertedInputs[3] = true;
                    break;

                default:
                    throw new ArgumentException("Unknown type");
            }
        }

        public MonoExpanderInstance(int inputs, int outputs, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Init(name, inputs, outputs);
        }

        private void Init(string name, int inputs, int outputs)
        {
            this.name = name;

            DigitalInputs = new PhysicalDevice.DigitalInput[inputs];
            InvertedInputs = new bool[inputs];

            for (int index = 0; index < this.DigitalInputs.Length; index++)
                this.DigitalInputs[index] = new PhysicalDevice.DigitalInput();

            this.DigitalOutputs = new PhysicalDevice.DigitalOutput[outputs];
            for (int index = 0; index < this.DigitalOutputs.Length; index++)
                WireupOutput(index);

            this.audioTrackDone = new Subject<int>();
            this.videoTrackDone = new Subject<int>();
            this.audioTrackStart = new Subject<(int Output, AudioTypes AudioType, string Filename)>();

            this.Motor = new PhysicalDevice.MotorWithFeedback((target, speed, timeout) =>
            {
                //                this.oscClient.Send("/motor/exec", 1, target, (int)(speed * 100), timeout.TotalSeconds.ToString("F0"));
            });

            Executor.Current.RegisterAutoName(this.name);

            Executor.Current.SetupCommands.Subscribe(x =>
            {
                if (x.Name != this.name)
                    return;

                switch (x)
                {
                    case SetupDataPort portStatus:
                        if (portStatus.Direction == Direction.Input)
                        {
                            if (portStatus.Port >= 0 && portStatus.Port < DigitalOutputs.Length)
                                this.DigitalInputs[portStatus.Port].Trigger(portStatus.Value);
                        }
                        else if (portStatus.Direction == Direction.Output)
                        {
                            if (portStatus.Port >= 0 && portStatus.Port < DigitalOutputs.Length)
                                SendDigitalOutputMessage(portStatus.Port, portStatus.Value);
                        }
                        break;
                }
            });

            Executor.Current.Register(this);
        }

        public PhysicalDevice.DigitalInput[] DigitalInputs { get; private set; }

        public PhysicalDevice.DigitalOutput[] DigitalOutputs { get; private set; }

        public PhysicalDevice.MotorWithFeedback Motor { get; private set; }

        public bool[] InvertedInputs { get; private set; }

        protected virtual void RaiseVideoTrackDone()
        {
            this.videoTrackDone.OnNext(0);
        }

        public void Start()
        {
            // Send initial
            for (int index = 0; index < DigitalInputs.Length; index++)
            {
                Executor.Current.Diagnostics.OnNext(new DiagDataPortStatus
                {
                    Name = this.name,
                    Direction = Direction.Input,
                    Port = index,
                    Value = false
                });

                Executor.Current.Diagnostics.OnNext(new DiagDataPortStatus
                {
                    Name = this.name,
                    Direction = Direction.Output,
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
                SendDigitalOutputMessage(index, x);

                Executor.Current.Diagnostics.OnNext(new DiagDataPortStatus
                {
                    Name = this.name,
                    Direction = Direction.Output,
                    Port = index,
                    Value = x
                });
            });
        }

        private void SendDigitalOutputMessage(int output, bool value)
        {
            SendMessage(new SetOutputRequest
            {
                Output = string.Format("d{0}", output),
                Value = value ? 1.0 : 0.0
            }, $"d-out{output}");
        }

        public MonoExpanderInstance Connect(LogicalDevice.AudioPlayer logicalDevice, int output = 0)
        {
            this.audioTrackDone.Where(x => x == output).Subscribe(o =>
             {
                 logicalDevice.RaiseAudioTrackDone();
             });

            this.audioTrackStart.Where(x => x.Output == output).Subscribe(d => logicalDevice.RaiseAudioTrackStart(d.AudioType, d.Filename));

            logicalDevice.AudioChanged += (sender, e) =>
                {
                    switch (e.Command)
                    {
                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX:
                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayFX:
                            if (e.LeftVolume.HasValue && e.RightVolume.HasValue)
                                SendMessage(new AudioEffectPlay
                                {
                                    Output = output,
                                    FileName = e.AudioFile,
                                    VolumeLeft = e.LeftVolume.Value,
                                    VolumeRight = e.RightVolume.Value,
                                    Simultaneous = e.Command == LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX
                                });
                            else
                                SendMessage(new AudioEffectPlay
                                {
                                    Output = output,
                                    FileName = e.AudioFile,
                                    Simultaneous = e.Command == LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX
                                });
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueFX:
                            SendMessage(new AudioEffectCue
                            {
                                Output = output,
                                FileName = e.AudioFile
                            });
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueTrack:
                            SendMessage(new AudioTrackCue
                            {
                                Output = output,
                                FileName = e.AudioFile
                            });
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayTrack:
                            SendMessage(new AudioTrackPlay
                            {
                                Output = output,
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
                            SendMessage(new AudioBackgroundResume { Output = output }, "background-audio");
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseBackground:
                            SendMessage(new AudioBackgroundPause { Output = output }, "background-audio");
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeFX:
                            SendMessage(new AudioEffectResume { Output = output });
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseFX:
                            SendMessage(new AudioEffectPause { Output = output });
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.StopFX:
                            SendMessage(new AudioEffectStop { Output = output });
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.NextBackground:
                            SendMessage(new AudioBackgroundNext { Output = output });
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.BackgroundVolume:
                            SendMessage(new AudioBackgroundSetVolume
                            {
                                Output = output,
                                Volume = ((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value
                            }, "mv-bg");
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeTrack:
                            SendMessage(new AudioTrackResume { Output = output });
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseTrack:
                            SendMessage(new AudioTrackPause { Output = output });
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.StopTrack:
                            SendMessage(new AudioTrackStop { Output = output });
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.EffectVolume:
                            SendMessage(new AudioEffectSetVolume
                            {
                                Output = output,
                                Volume = ((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value
                            }, "mv-fx");
                            break;

                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.TrackVolume:
                            SendMessage(new AudioTrackSetVolume
                            {
                                Output = output,
                                Volume = ((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value
                            }, "mv-trk");
                            break;
                    }
                };

            return this;
        }

        public MonoExpanderInstance Connect(LogicalDevice.VideoPlayer logicalDevice)
        {
            this.videoTrackDone.Subscribe(o =>
            {
                logicalDevice.RaiseVideoTrackDone();
            });

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
            this.log.Debug("Input {0} on {1} set to {2}", message.Input, this.name, message.Value);

            if (message.Input.StartsWith("d"))
            {
                int inputId;
                if (int.TryParse(message.Input.Substring(1), out inputId))
                {
                    if (inputId >= 0 && inputId < this.DigitalInputs.Length)
                    {
                        bool value = message.Value != 0;
                        if (this.InvertedInputs[inputId])
                            value = !value;

                        this.DigitalInputs[inputId].Trigger(value);

                        Executor.Current.Diagnostics.OnNext(new DiagDataPortStatus
                        {
                            Name = this.name,
                            Direction = Direction.Input,
                            Port = inputId,
                            Value = value
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

                    this.audioTrackStart.OnNext((message.Output, message.Type, message.Id));
                    break;

                case AudioTypes.Effect:
                    log.Debug("Playing effect {0} on {1}", message.Id, this.name);
                    break;
            }

            string playValue = message.Id;
            if (playValue.Contains('/'))
            {
                // It's a path, strip the path
                int lastPos = playValue.LastIndexOf('/');
                if (lastPos > -1)
                    playValue = playValue.Substring(lastPos + 1);
            }
            if (message.Output > 0)
                playValue = $"{message.Output}: {playValue}";

            Executor.Current.Diagnostics.OnNext(new DiagDataAudioPlayback
            {
                Type = message.Type == AudioTypes.Effect ? message.Type.ToString() : AudioTypes.Track.ToString(),
                Name = this.name,
                Value = playValue
            });
        }

        public void Handle(AudioFinished message)
        {
            switch (message.Type)
            {
                case AudioTypes.Track:
                    log.Debug("Audio track {0} done", message.Id);
                    this.audioTrackDone.OnNext(message.Output);
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
