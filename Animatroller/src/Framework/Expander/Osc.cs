using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using NLog;

namespace Animatroller.Framework.Expander
{
    public abstract class Osc : IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private Rug.Osc.OscReceiver receiver;
        private Task receiverTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private Dictionary<string, Action<IEnumerable<object>>> dispatch;

        public Osc(int listenPort)
        {
            this.receiver = new Rug.Osc.OscReceiver(listenPort);
            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.dispatch = new Dictionary<string, Action<IEnumerable<object>>>();

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
                                        if (oscMessage != null)
                                        {
                                            Action<IEnumerable<object>> action;
                                            if (this.dispatch.TryGetValue(oscMessage.Address, out action))
                                            {
                                                try
                                                {
                                                    action.Invoke(oscMessage);
                                                }
                                                catch(Exception ex)
                                                {
                                                    log.ErrorException("Error while dispatching OSC message", ex);
                                                }
                                            }
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
