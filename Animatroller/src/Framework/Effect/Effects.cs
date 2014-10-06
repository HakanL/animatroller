using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reactive.Subjects;
using Animatroller.Framework.Extensions;
using NLog;

namespace Animatroller.Framework.Effect
{
    public class Pulsating : BaseSweeperEffect<LogicalDevice.IHasBrightnessControl>
    {
        private Transformer.EaseInOut easeTransform = new Transformer.EaseInOut();
        private double minBrightness;
        private double maxBrightness;

        public Pulsating(string name, TimeSpan sweepDuration, double minBrightness, double maxBrightness, bool startRunning = true)
            : base(name, sweepDuration, startRunning)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
        }

        public Pulsating(string name, TimeSpan sweepDuration)
            : this(name, sweepDuration, 0, 1)
        {
        }

        protected override void ExecutePerDevice(LogicalDevice.IHasBrightnessControl device,
            double zeroToOne, double negativeOneToOne, double oneToZeroToOne, bool final)
        {
            double brightness = easeTransform.Transform(oneToZeroToOne)
                .ScaleToMinMax(this.minBrightness, this.maxBrightness);

            device.SetBrightness(brightness, this);

            if (final)
                device.Brightness = 0;
        }

        public double MinBrightness
        {
            get { return this.minBrightness; }
            set { this.minBrightness = value.Limit(0, 1); }
        }

        public double MaxBrightness
        {
            get { return this.maxBrightness; }
            set { this.maxBrightness = value.Limit(0, 1); }
        }
    }

    public class Fader : BaseSweeperEffect<LogicalDevice.IHasBrightnessControl>
    {
        private Transformer.EaseInOut easeTransform = new Transformer.EaseInOut();
        private double minBrightness;
        private double maxBrightness;

        public Fader(string name, TimeSpan sweepDuration, double minBrightness, double maxBrightness, bool startRunning = true)
            : base(name, sweepDuration, startRunning)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
            base.sweeper.OneShot();
        }

        public Fader(string name, TimeSpan sweepDuration)
            : this(name, sweepDuration, 0, 1)
        {
        }

        protected override void ExecutePerDevice(LogicalDevice.IHasBrightnessControl device,
            double zeroToOne, double negativeOneToOne, double oneToZeroToOne, bool final)
        {
            double brightness = zeroToOne
                .ScaleToMinMax(this.minBrightness, this.maxBrightness);

            device.SetBrightness(brightness, this);

            if (final)
                device.ReleaseOwner();
        }

        public double MinBrightness
        {
            get { return this.minBrightness; }
            set { this.minBrightness = value.Limit(0, 1); }
        }

        public double MaxBrightness
        {
            get { return this.maxBrightness; }
            set { this.maxBrightness = value.Limit(0, 1); }
        }
    }

    public class PopOut : BaseSweeperEffect<LogicalDevice.IHasBrightnessControl>
    {
        private double startBrightness;

        public PopOut(string name, TimeSpan sweepDuration)
            : base(name, sweepDuration, false)
        {
            base.sweeper.OneShot();
        }

        public PopOut(string name, TimeSpan sweepDuration, int dataPoints)
            : base(name, sweepDuration, dataPoints, false)
        {
            base.sweeper.OneShot();
        }

        public PopOut Pop(double startBrightness)
        {
            this.startBrightness = startBrightness;

            base.sweeper.Reset();

            return this;
        }

        protected override void ExecutePerDevice(LogicalDevice.IHasBrightnessControl device,
            double zeroToOne, double negativeOneToOne, double oneToZeroToOne, bool final)
        {
            double brightness = this.startBrightness * (1 - zeroToOne);

            if (brightness < 0.1)
                brightness = 0;

            device.SetBrightness(brightness, this);

            if (final)
                device.Brightness = 0;
        }
    }

    public class PopOut2 : BaseSweeperEffect<ISubject<DoubleZeroToOne>>
    {
        private double startBrightness;

        public PopOut2(string name, TimeSpan sweepDuration)
            : base(name, sweepDuration, false)
        {
            base.sweeper.OneShot();
        }

        public PopOut2(string name, TimeSpan sweepDuration, int dataPoints)
            : base(name, sweepDuration, dataPoints, false)
        {
            base.sweeper.OneShot();
        }

        public PopOut2 Pop(double startBrightness)
        {
            this.startBrightness = startBrightness;

            base.sweeper.Reset();

            return this;
        }

        protected override void ExecutePerDevice(ISubject<DoubleZeroToOne> device,
            double zeroToOne, double negativeOneToOne, double oneToZeroToOne, bool final)
        {
            double brightness = this.startBrightness * (1 - zeroToOne);

            if (brightness < 0.1)
                brightness = 0;

            device.OnNext(new DoubleZeroToOne(brightness));

            if (final)
                device.OnNext(DoubleZeroToOne.Zero);
        }
    }

    public class Flicker : IEffect
    {
        protected bool isRunning;
        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected int priority;
        protected string name;
        private Random random = new Random();
        protected object lockObject = new object();
        protected Timer timer;
        protected List<LogicalDevice.IHasBrightnessControl> devices;
        protected List<ISubject<DoubleZeroToOne>> devices2;
        protected ISubject<bool> inputRun;
        private double minBrightness;
        private double maxBrightness;

        public Flicker([System.Runtime.CompilerServices.CallerMemberName] string name = "", double minBrightness = 0.0, double maxBrightness = 1.0, bool startRunning = true)
        {
            this.name = name;
            Executor.Current.Register(this);

            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;

            this.devices = new List<LogicalDevice.IHasBrightnessControl>();
            this.devices2 = new List<ISubject<DoubleZeroToOne>>();
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
                    foreach (var device in this.devices)
                        device.Brightness = this.random.NextDouble()
                            .ScaleToMinMax(this.minBrightness, this.maxBrightness);

                    foreach (var device in this.devices2)
                        device.OnNext(new DoubleZeroToOne(this.random.NextDouble()
                            .ScaleToMinMax(this.minBrightness, this.maxBrightness)));
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
                foreach (var device in this.devices)
                    device.Brightness = 0;

                foreach (var device in this.devices2)
                    device.OnNext(DoubleZeroToOne.Zero);
            }

            return this;
        }

        public string Name
        {
            get { return this.name; }
        }

        public int Priority
        {
            get { return this.priority; }
        }

        public Flicker AddDevice(LogicalDevice.IHasBrightnessControl device)
        {
            lock (lockObject)
            {
                this.devices.Add(device);
            }

            return this;
        }

        public Flicker ConnectTo(ISubject<DoubleZeroToOne> device)
        {
            lock (lockObject)
            {
                this.devices2.Add(device);
            }

            return this;
        }

        public Flicker RemoveDevice(LogicalDevice.IHasBrightnessControl device)
        {
            lock (lockObject)
            {
                if (this.devices.Contains(device))
                    this.devices.Remove(device);
            }
            return this;
        }
    }
}
