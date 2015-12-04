using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Configuration;
using Animatroller.Framework.MonoExpanderMessages;
using NLog;

namespace Animatroller.Framework.Expander
{
    public class MonoExpanderInstance : IPort, IRunnable, IOutputHardware, IMonoExpanderInstance
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private string name;
        private IActorRef clientActorRef;
        private IActorRef serverActorRef;
        private event EventHandler<EventArgs> AudioTrackDone;
        private event EventHandler<EventArgs> VideoTrackDone;
        private ISubject<string> audioTrackStart;

        public MonoExpanderInstance(int inputs = 8, int outputs = 8, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;

            this.DigitalInputs = new PhysicalDevice.DigitalInput[inputs];
            for (int index = 0; index < this.DigitalInputs.Length; index++)
                this.DigitalInputs[index] = new PhysicalDevice.DigitalInput();

            this.DigitalOutputs = new PhysicalDevice.DigitalOutput[outputs];
            for (int index = 0; index < this.DigitalOutputs.Length; index++)
                WireupOutput(index);

            this.audioTrackStart = new Subject<string>();

            this.Motor = new PhysicalDevice.MotorWithFeedback((target, speed, timeout) =>
            {
                //                this.oscClient.Send("/motor/exec", 1, target, (int)(speed * 100), timeout.TotalSeconds.ToString("F0"));
            });


            Executor.Current.Register(this);
        }

        public void SetActor(IActorRef clientActorRef, IActorRef serverActorRef)
        {
            this.clientActorRef = clientActorRef;
            this.serverActorRef = serverActorRef;
        }

        private void SendMessage(object message)
        {
            this.clientActorRef?.Tell(message, this.serverActorRef);
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
                });
            });
        }

        public MonoExpanderInstance Connect(LogicalDevice.AudioPlayer logicalDevice)
        {
            this.AudioTrackDone += (o, e) =>
                {
                    logicalDevice.RaiseAudioTrackDone();
                };

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
                            else if (e.LeftVolume.HasValue)
                                SendMessage(new AudioEffectPlay
                                {
                                    FileName = e.AudioFile,
                                    VolumeLeft = e.LeftVolume.Value,
                                    VolumeRight = e.LeftVolume.Value,
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
                            SendMessage(new AudioBackgroundResume());
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseBackground:
                            SendMessage(new AudioBackgroundPause());
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
                            });
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeTrack:
                            SendMessage(new AudioTrackResume());
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseTrack:
                            SendMessage(new AudioTrackPause());
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
            log.Info("Input {0} set to {1}", message.Input, message.Value);

            if (message.Input.StartsWith("d"))
            {
                int inputId;
                if (int.TryParse(message.Input.Substring(1), out inputId))
                {
                    if (inputId >= 0 && inputId <= 7)
                        this.DigitalInputs[inputId].Trigger(message.Value != 0.0);
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
                case AudioTypes.Background:
                    log.Debug("Playing background track {0}", message.Id);
                    this.audioTrackStart.OnNext(message.Id);
                    break;
            }
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
        }
    }
}
