using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using NLog;

namespace Animatroller.Framework.Expander
{
    public class OscServer : IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private Rug.Osc.OscReceiver receiver;
        private Task receiverTask;
        private System.Threading.CancellationTokenSource cancelSource;

        public OscServer(int listenPort, int numberOfInputs)
        {
            this.DigitalInputs = new PhysicalDevice.DigitalInput[numberOfInputs];
            for (int index = 0; index < this.DigitalInputs.Length; index++)
                this.DigitalInputs[index] = new PhysicalDevice.DigitalInput();

            this.receiver = new Rug.Osc.OscReceiver(listenPort);
            this.cancelSource = new System.Threading.CancellationTokenSource();

            this.receiverTask = new Task(x =>
            {
                try
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        while (this.receiver.State != Rug.Osc.OscSocketState.Closed)
                        {
                            if (this.receiver.State == Rug.Osc.OscSocketState.Connected)
                            {
                                var packet = this.receiver.Receive();
                                log.Debug("Received OSC message: {0}", packet);

                                var bundles = packet as Rug.Osc.OscBundle;
                                if (bundles != null && bundles.Any())
                                {
                                    foreach (var bundle in bundles)
                                    {
                                        var oscMessage = bundle as Rug.Osc.OscMessage;
                                        if (oscMessage != null && oscMessage.Any() && oscMessage.First() is int)
                                        {
                                            var data = (int)oscMessage.First();

                                            if (oscMessage.Address == "/OnOff")
                                                DigitalInputs[0].Trigger(data != 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            Executor.Current.Register(this);
        }

        public PhysicalDevice.DigitalInput[] DigitalInputs { get; private set; }

        public void Start()
        {
            this.receiverTask.Start();
            this.receiver.Connect();
        }

        public void Stop()
        {
            this.cancelSource.Cancel();
            this.receiver.Close();
        }
    }
}
