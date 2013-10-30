using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using NLog;

namespace Animatroller.Framework.Expander
{
    public class Raspberry : IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private OscClient oscClient;
        private OscServer oscServer;
        private string hostName;
        private int hostPort;

        public Raspberry(string hostEntry, int listenPort)
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

            this.Motor = new PhysicalDevice.MotorWithFeedback((target, speed, timeout) =>
            {
                this.oscClient.Send("/motor/exec", 1, target, (int)(speed * 100), timeout.TotalSeconds.ToString("F0"));
            });

            this.oscServer = new OscServer(listenPort);
            this.oscServer.RegisterAction("/init", msg =>
                {
                    log.Info("Raspberry is up");
                });

            this.oscServer.RegisterAction<int>("/input", (msg, data) =>
                {
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

        public PhysicalDevice.DigitalInput[] DigitalInputs { get; private set; }
        public PhysicalDevice.DigitalOutput[] DigitalOutputs { get; private set; }
        public PhysicalDevice.MotorWithFeedback Motor { get; private set; }

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

        public Raspberry Connect(LogicalDevice.AudioPlayer logicalDevice)
        {
            logicalDevice.AudioChanged += (sender, e) =>
                {
                    switch (e.Command)
                    {
                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayFX:
                            if (e.LeftVolume.HasValue && e.RightVolume.HasValue)
                                this.oscClient.Send("/audio/fx/play", e.AudioFile, e.LeftVolume.Value.ToString("F2"), e.RightVolume.Value.ToString("F2"));
                            else if (e.LeftVolume.HasValue)
                                this.oscClient.Send("/audio/fx/play", e.AudioFile, e.LeftVolume.Value.ToString("F2"));
                            else
                                this.oscClient.Send("/audio/fx/play", e.AudioFile);
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayNewFX:
                            if (e.LeftVolume.HasValue && e.RightVolume.HasValue)
                                this.oscClient.Send("/audio/fx/playnew", e.AudioFile, e.LeftVolume.Value.ToString("F2"), e.RightVolume.Value.ToString("F2"));
                            else if (e.LeftVolume.HasValue)
                                this.oscClient.Send("/audio/fx/playnew", e.AudioFile, e.LeftVolume.Value.ToString("F2"));
                            else
                                this.oscClient.Send("/audio/fx/playnew", e.AudioFile);
                            break;

                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.CueFX:
                            this.oscClient.Send("/audio/fx/cue", e.AudioFile);
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
                            this.oscClient.Send("/audio/bg/volume", ((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value.ToString("f2"));
                            break;
                    }
                };

            return this;
        }
    }
}
