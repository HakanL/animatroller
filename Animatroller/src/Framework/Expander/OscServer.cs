//#define DEBUG_OSC

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
        public class Message
        {
            public string Address { get; private set; }
            public Rug.Osc.OscMessage RawMessage { get; private set; }
            public IEnumerable<object> Data { get; private set; }

            public Message(Rug.Osc.OscMessage message)
            {
                this.Address = message.Address;
                this.RawMessage = message;
                this.Data = message;
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        private Rug.Osc.OscReceiver receiver;
        private Task receiverTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private Dictionary<string, Action<Message>> dispatch;
        private Dictionary<string, Action<Message>> dispatchPartial;

        public OscServer(int listenPort)
        {
            this.receiver = new Rug.Osc.OscReceiver(listenPort);
            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.dispatch = new Dictionary<string, Action<Message>>();
            this.dispatchPartial = new Dictionary<string, Action<Message>>();

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
#if DEBUG_OSC
                                log.Debug("Received OSC message: {0}", packet);
#endif

                                if (packet is Rug.Osc.OscBundle)
                                {
                                    var bundles = (Rug.Osc.OscBundle)packet;
                                    if (bundles.Any())
                                    {
                                        foreach (var bundle in bundles)
                                        {
                                            var oscMessage = bundle as Rug.Osc.OscMessage;
                                            if (oscMessage != null)
                                            {
                                                Invoke(oscMessage);
                                            }
                                        }
                                    }
                                }

                                if(packet is Rug.Osc.OscMessage)
                                {
                                    var msg = (Rug.Osc.OscMessage)packet;
                                    Invoke(msg);
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

        private void Invoke(Rug.Osc.OscMessage oscMessage)
        {
            var message = new Message(oscMessage);

            Action<Message> action;
            if (this.dispatch.TryGetValue(oscMessage.Address, out action))
            {
                try
                {
                    action.Invoke(message);
                }
                catch (Exception ex)
                {
                    log.ErrorException("Error while dispatching OSC message", ex);
                }
            }

            foreach (var kvp in this.dispatchPartial)
            {
                if (kvp.Key == string.Empty || message.Address.StartsWith(kvp.Key))
                {
                    try
                    {
                        kvp.Value.Invoke(message);
                    }
                    catch (Exception ex)
                    {
                        log.ErrorException("Error while dispatching OSC message", ex);
                    }
                }
            }
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

        public OscServer RegisterAction(string address, Action<Message> action)
        {
            if (address.EndsWith("*"))
                this.dispatchPartial[address.Substring(0, address.Length - 1)] = action;
            else
                this.dispatch[address] = action;

            return this;
        }

        public OscServer RegisterAction<T>(string address, Action<Message, IEnumerable<T>> action)
        {
            Action<Message> invokeAction = msg =>
                {
                    var list = msg.Data.ToList().ConvertAll<T>(y => (T)Convert.ChangeType(y, typeof(T)));
                    action(msg, list);
                };

            if (address.EndsWith("*"))
                this.dispatchPartial[address.Substring(0, address.Length - 1)] = invokeAction;
            else
                this.dispatch[address] = invokeAction;

            return this;
        }
    }
}
