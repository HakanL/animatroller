using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using Serilog;

namespace Animatroller.Framework.Effect
{
    public abstract class BaseSweeperEffect : IEffect
    {
        protected class DeviceController : Controller.BaseDeviceController<IReceivesBrightness>
        {
            public IPushDataController Observer { get; set; }

            public IData AdditionalData { get; set; }

            public DeviceController(IReceivesBrightness device, IData additionalData)
                : base(device, 0)
            {
                AdditionalData = additionalData;
            }
        }

        protected bool isRunning;
        protected ILogger log;
        protected int priority;
        protected string name;
        protected object lockObject = new object();
        protected Sweeper sweeper;
        protected List<DeviceController> devices;
        protected ISubject<bool> inputRun;
        private IControlToken token;

        public BaseSweeperEffect(
            TimeSpan sweepDuration,
            int dataPoints,
            bool startRunning,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.log = Log.Logger;
            this.name = name;
            Executor.Current.Register(this);

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

            this.devices = new List<DeviceController>();
            this.sweeper = new Sweeper(sweepDuration, dataPoints, startRunning);

            this.sweeper.RegisterJob((zeroToOne, negativeOneToOne, zeroToOneToZero, forced, totalTicks, final) =>
                {
                    bool isUnlocked = false;
                    if (forced)
                    {
                        Monitor.Enter(lockObject);
                        isUnlocked = true;
                    }
                    else
                        isUnlocked = Monitor.TryEnter(lockObject);

                    if (isUnlocked)
                    {
                        try
                        {
                            double value = GetValue(zeroToOne, negativeOneToOne, zeroToOneToZero, final);

                            SendOutput(value);
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
                        this.log.Warning("Missed Job in BaseSweepEffect   Name: " + Name);
                });
        }

        // Generate sweeper with 50 ms interval
        public BaseSweeperEffect(TimeSpan sweepDuration, bool startRunning, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : this(
            sweepDuration,
            (int)(sweepDuration.TotalMilliseconds > 500 ? sweepDuration.TotalMilliseconds / 50 : sweepDuration.TotalMilliseconds / 25),
            startRunning,
            name)
        {
        }

        public IObserver<bool> InputRun
        {
            get
            {
                return this.inputRun;
            }
        }

        protected abstract double GetValue(double zeroToOne, double negativeOneToOne, double zeroToOneToZero, bool final);

        protected void SendOutput(double value)
        {
            var totalWatch = System.Diagnostics.Stopwatch.StartNew();

            var watches = new System.Diagnostics.Stopwatch[this.devices.Count];
            for (int i = 0; i < this.devices.Count; i++)
            {
                watches[i] = System.Diagnostics.Stopwatch.StartNew();

                var deviceOwner = this.devices[i].Observer;
                if (deviceOwner == null)
                    continue;

                deviceOwner.Data[DataElements.Brightness] = value;
                deviceOwner.PushData(channel: Channel.Main);

                watches[i].Stop();
            }
            totalWatch.Stop();

            if (watches.Any())
            {
                double max = watches.Select(x => x.ElapsedMilliseconds).Max();
                double avg = watches.Select(x => x.ElapsedMilliseconds).Average();

                if (totalWatch.ElapsedMilliseconds > 25)
                {
                    this.log.Information(string.Format("Devices {0}   Max: {1:N1}   Avg: {2:N1}   Total: {3:N1}",
                        this.devices.Count, max, avg, totalWatch.ElapsedMilliseconds));
                }
            }
        }

        public IEffect Start(IChannel channel = null, int priority = 1, IControlToken token = null)
        {
            return Start(channel, priority, token, false);
        }

        public IEffect StartWithTakeControl(IChannel channel = null, int priority = 1)
        {
            return Start(channel, priority, token: null, takeControl: true);
        }

        private IEffect Start(IChannel channel, int priority, IControlToken token, bool takeControl)
        {
            if (this.token == null)
            {
                IControlToken controlToken = token;
                if (controlToken == null && takeControl)
                {
                    this.token = new GroupControlToken(this.devices.Select(x => x.Device), null, Name, channel, priority);
                    controlToken = this.token;
                }

                foreach (var device in this.devices)
                {
                    device.Observer = device.Device.GetDataObserver(channel, controlToken);

                    device.Observer.SetDataFromIData(device.AdditionalData);
                }
            }

            this.sweeper.Resume();
            this.isRunning = true;

            return this;
        }

        public IEffect Prime()
        {
            this.sweeper.Prime();

            return this;
        }

        public IEffect Restart()
        {
            this.sweeper.Reset();

            return this;
        }

        public IEffect Stop()
        {
            this.sweeper.Pause();

            this.sweeper.ForceValue(0, 0, 0, 0);
            this.isRunning = false;

            foreach (var device in this.devices)
            {
                device.Observer = null;
            }

            this.token?.Dispose();
            this.token = null;

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

        public BaseSweeperEffect ConnectTo(IReceivesBrightness device, params Tuple<DataElements, object>[] additionalData)
        {
            IData data = additionalData.GenerateIData();

            lock (this.lockObject)
            {
                this.devices.Add(new DeviceController(device, data));
            }

            return this;
        }

        public BaseSweeperEffect Disconnect(IReceivesBrightness device)
        {
            lock (this.lockObject)
            {
                this.devices.RemoveAll(x => x.Device == device);
            }

            return this;
        }

        public void SetAdditionalData(IReceivesBrightness device, params Tuple<DataElements, object>[] additionalData)
        {
            lock (this.lockObject)
            {
                var foundDevice = this.devices.FirstOrDefault(x => x.Device == device);

                if (foundDevice != null)
                {
                    foundDevice.AdditionalData = additionalData.GenerateIData();

                    if (foundDevice.Observer != null)
                        foundDevice.Observer.SetDataFromIData(foundDevice.AdditionalData);
                }
            }
        }

        public Action<int> NewIterationAction
        {
            get { return this.sweeper.NewIterationAction; }
            set { this.sweeper.NewIterationAction = value; }
        }
    }
}
