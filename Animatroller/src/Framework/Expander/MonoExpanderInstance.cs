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

        private void Send(object message)
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
            //            this.oscClient.Send("/init");
        }

        public void Stop()
        {
        }

        private void WireupOutput(int index)
        {
            this.DigitalOutputs[index] = new PhysicalDevice.DigitalOutput(x =>
            {
                Send(new SetOutputRequest
                {
                    Output = string.Format("d{0}", index),
                    Value = x ? 1.0 : 0.0
                });
            });
        }

        public void Test(int value)
        {
            //            this.oscClient.Send("/test", value.ToString());
        }

        public MonoExpanderInstance Connect(LogicalDevice.AudioPlayer logicalDevice)
        {
            this.AudioTrackDone += (o, e) =>
                {
                    logicalDevice.RaiseAudioTrackDone();
                };

            //logicalDevice.AudioChanged += (sender, e) =>
            //    {
            //        switch (e.Command)
            //        {
            //            case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayFX:
            //                if (e.LeftVolume.HasValue && e.RightVolume.HasValue)
            //                    this.oscClient.Send("/audio/fx/play", e.AudioFile, (float)e.LeftVolume.Value, (float)e.RightVolume.Value);
            //                else if (e.LeftVolume.HasValue)
            //                    this.oscClient.Send("/audio/fx/play", e.AudioFile, (float)e.LeftVolume.Value);
            //                else
            //                    this.oscClient.Send("/audio/fx/play", e.AudioFile);
            //                break;

            //            case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX:
            //                if (e.LeftVolume.HasValue && e.RightVolume.HasValue)
            //                    this.oscClient.Send("/audio/fx/playnew", e.AudioFile, (float)e.LeftVolume.Value, (float)e.RightVolume.Value);
            //                else if (e.LeftVolume.HasValue)
            //                    this.oscClient.Send("/audio/fx/playnew", e.AudioFile, (float)e.LeftVolume.Value);
            //                else
            //                    this.oscClient.Send("/audio/fx/playnew", e.AudioFile);
            //                break;

            //            case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueFX:
            //                this.oscClient.Send("/audio/fx/cue", e.AudioFile);
            //                break;

            //            case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueTrack:
            //                this.oscClient.Send("/audio/trk/cue", e.AudioFile);
            //                break;

            //            case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayTrack:
            //                this.oscClient.Send("/audio/trk/play", e.AudioFile);
            //                break;
            //        }
            //    };

            //logicalDevice.ExecuteCommand += (sender, e) =>
            //    {
            //        switch (e.Command)
            //        {
            //            case LogicalDevice.Event.AudioCommandEventArgs.Commands.PlayBackground:
            //                this.oscClient.Send("/audio/bg/play");
            //                break;
            //            case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseBackground:
            //                this.oscClient.Send("/audio/bg/pause");
            //                break;
            //            case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeFX:
            //                this.oscClient.Send("/audio/fx/resume");
            //                break;
            //            case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseFX:
            //                this.oscClient.Send("/audio/fx/pause");
            //                break;
            //            case LogicalDevice.Event.AudioCommandEventArgs.Commands.NextBackground:
            //                this.oscClient.Send("/audio/bg/next");
            //                break;
            //            case LogicalDevice.Event.AudioCommandEventArgs.Commands.BackgroundVolume:
            //                this.oscClient.Send("/audio/bg/volume", (float)((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value);
            //                break;
            //            case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeTrack:
            //                this.oscClient.Send("/audio/trk/resume");
            //                break;
            //            case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseTrack:
            //                this.oscClient.Send("/audio/trk/pause");
            //                break;
            //        }
            //    };

            return this;
        }

        public MonoExpanderInstance Connect(LogicalDevice.VideoPlayer logicalDevice)
        {
            this.VideoTrackDone += (o, e) =>
            {
                logicalDevice.RaiseVideoTrackDone();
            };

            //logicalDevice.ExecuteCommand += (sender, e) =>
            //{
            //    switch (e.Command)
            //    {
            //        case LogicalDevice.Event.VideoCommandEventArgs.Commands.PlayVideo:
            //            this.oscClient.Send("/video/play", e.VideoFile);
            //            break;
            //    }
            //};

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
            throw new NotImplementedException();
        }
    }
}
