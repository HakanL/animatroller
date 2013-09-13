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

            this.DigitalInputs = new PhysicalDevice.DigitalInput[4];
            for (int index = 0; index < this.DigitalInputs.Length; index++)
                this.DigitalInputs[index] = new PhysicalDevice.DigitalInput();

            this.oscServer = new OscServer(listenPort);
            this.oscServer.RegisterAction<string>("/init", x =>
                {
                    log.Info("Raspberry is up");
                });

            Executor.Current.Register(this);
        }

        public PhysicalDevice.DigitalInput[] DigitalInputs { get; private set; }

        public void Start()
        {
            this.oscClient.Send("/init");
        }

        public void Stop()
        {
        }

        public Raspberry Connect(LogicalDevice.AudioPlayer logicalDevice)
        {
            logicalDevice.AudioChanged += (sender, e) =>
                {
                    switch(e.Command)
                    {
                        case LogicalDevice.Event.AudioChangedEventArgs.Commands.PlayFX:
                            this.oscClient.Send("/audio/fx/play", e.AudioFile);
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
                            this.oscClient.Send("/audio/bg/stop");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.ResumeFX:
                            this.oscClient.Send("/audio/fx/resume");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.PauseFX:
                            this.oscClient.Send("/audio/fx/pause");
                            break;
                        case LogicalDevice.Event.AudioCommandEventArgs.Commands.BackgroundVolume:
                            this.oscClient.Send("/audio/bg/volume", ((LogicalDevice.Event.AudioCommandValueEventArgs)e).Value);
                            break;
                    }
                };

            return this;
        }
    }
}
