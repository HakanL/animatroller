using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reactive.Subjects;
using Animatroller.Framework.Extensions;
using NLog;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.Effect
{
    public class Flicker : IEffect
    {
        protected class DeviceController : Controller.BaseDeviceController<IReceivesBrightness>
        {
            public ControlledObserverData DataOwner { get; set; }

            public DeviceController(IReceivesBrightness device, int priority)
                : base(device, priority)
            {
            }
        }

        protected bool isRunning;
        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected string name;
        private Random random = new Random();
        protected object lockObject = new object();
        protected Timer timer;
        protected List<DeviceController> devices;
        protected ISubject<bool> inputRun;
        private double minBrightness;
        private double maxBrightness;

        public Flicker(double minBrightness = 0.0, double maxBrightness = 1.0, bool startRunning = true, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
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
                        var deviceOwner = heldDevice.DataOwner;

                        if (deviceOwner == null)
                        {
                            // Grab control
                            var token = heldDevice.Device.TakeControl(priority: heldDevice.Priority, name: Name);

                            deviceOwner =
                            heldDevice.DataOwner = heldDevice.Device.GetDataObserver(token);
                        }

                        deviceOwner
                            .OnNext(new Data(DataElements.Brightness, 
                                this.random.NextDouble().ScaleToMinMax(this.minBrightness, this.maxBrightness)));
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
                log.Warn("Missed execute in Flicker");

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

        public IEffect Start()
        {
            this.timer.Change(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(-1));
            this.isRunning = true;

            return this;
        }

        public IEffect Stop()
        {
            this.isRunning = false;
            this.timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            lock (lockObject)
            {
                foreach (var heldDevice in this.devices)
                {
                    if (heldDevice.DataOwner != null)
                    {
                        heldDevice.DataOwner.Dispose();
                        heldDevice.DataOwner = null;
                    }
                }
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

        public Flicker ConnectTo(IReceivesBrightness device, int priority = 1)
        {
            lock (lockObject)
            {
                this.devices.Add(new DeviceController(device, priority));
            }

            return this;
        }
    }
}
