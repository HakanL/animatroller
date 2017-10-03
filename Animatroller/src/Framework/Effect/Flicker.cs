using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reactive.Subjects;
using Animatroller.Framework.Extensions;
using Serilog;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.Effect
{
    public class Flicker : IEffect
    {
        protected class DeviceController : Controller.BaseDeviceController<IReceivesBrightness>
        {
            public IPushDataController Observer { get; set; }

            public DeviceController(IReceivesBrightness device)
                : base(device, 0)
            {
            }
        }

        protected bool isRunning;
        protected ILogger log;
        protected string name;
        private Random random = new Random();
        protected object lockObject = new object();
        protected Timer timer;
        protected List<DeviceController> devices;
        protected ISubject<bool> inputRun;
        private double minBrightness;
        private double maxBrightness;
        private GroupControlToken token;

        public Flicker(double minBrightness = 0.0, double maxBrightness = 1.0, bool startRunning = true, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.log = Log.Logger;
            this.name = name;
            Executor.Current.Register(this);

            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;

            this.devices = new List<DeviceController>();
            this.timer = new Timer(new TimerCallback(TimerCallback));

            this.inputRun = new Subject<bool>();

            this.inputRun.Subscribe(x =>
            {
                if (this.isRunning != x)
                {
                    if (x)
                        Start();
                    else
                        Stop();
                }
            });

            if (startRunning)
                Start();
        }

        private void TimerCallback(object state)
        {
            if (Monitor.TryEnter(lockObject))
            {
                try
                {
                    foreach (var heldDevice in this.devices)
                    {
                        var deviceOwner = heldDevice.Observer;
                        if (deviceOwner == null)
                            continue;

                        deviceOwner.Data[DataElements.Brightness] =
                                this.random.NextDouble().ScaleToMinMax(this.minBrightness, this.maxBrightness);

                        deviceOwner.PushData();
                    }
                }
                catch
                {
                }
                finally
                {
                    Monitor.Exit(lockObject);
                }
            }
            else
                this.log.Warning("Missed execute in Flicker");

            if (isRunning)
                this.timer.Change(random.Next(90) + 10, Timeout.Infinite);
        }

        public ISubject<bool> InputRun
        {
            get
            {
                return this.inputRun;
            }
        }

        public IEffect Start(int priority = 1)
        {
            if (this.token == null)
            {
                this.token = new GroupControlToken(this.devices.Select(x => x.Device), null, Name, priority);

                foreach (var device in this.devices)
                {
                    device.Observer = device.Device.GetDataObserver(this.token);
                }
            }

            this.timer.Change(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(-1));
            this.isRunning = true;

            return this;
        }

        public IEffect Stop()
        {
            this.isRunning = false;
            this.timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            foreach (var device in this.devices)
            {
                device.Observer = null;
            }

            if (this.token != null)
            {
                this.token.Dispose();
                this.token = null;
            }

            return this;
        }

        public string Name
        {
            get { return this.name; }
        }

        public int Priority
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Flicker ConnectTo(IReceivesBrightness device)
        {
            lock (lockObject)
            {
                this.devices.Add(new DeviceController(device));
            }

            return this;
        }
    }
}
