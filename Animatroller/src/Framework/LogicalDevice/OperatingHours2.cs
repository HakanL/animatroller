using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class OperatingHours2 : BaseDevice
    {
        protected class TimeRange
        {
            public DateTime From { get; set; }
            public DateTime To { get; set; }
        }

        private List<TimeRange> ranges;
        private Timer timer;
        private bool? isOpen;
        private bool? forced;

        private ISubject<bool> outputValue;

        public OperatingHours2([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.isOpen = null;
            this.ranges = new List<TimeRange>();

            this.outputValue = new Subject<bool>();

            this.timer = new Timer(x =>
                {
                    EvaluateOpenHours();
                }, null, Timeout.Infinite, Timeout.Infinite);
        }

        public IObservable<bool> Output
        {
            get
            {
                return this.outputValue;
            }
        }

        private void EvaluateOpenHours()
        {
            if (this.forced.HasValue)
            {
                IsOpen = this.forced.Value;
                return;
            }

            bool isOpenNow = false;
            if (!this.ranges.Any())
                isOpenNow = true;

            var now = DateTime.Now.TimeOfDay;
            foreach (var range in this.ranges)
            {
                if (range.From.TimeOfDay < range.To.TimeOfDay)
                {
                    if (now >= range.From.TimeOfDay &&
                        now <= range.To.TimeOfDay)
                    {
                        isOpenNow = true;
                        break;
                    }
                }
                else
                {
                    // Assume the To timestamp is the following day
                    if (now >= range.From.TimeOfDay ||
                        now <= range.To.TimeOfDay)
                    {
                        isOpenNow = true;
                        break;
                    }
                }
            }

            IsOpen = isOpenNow;
        }

        public override void StartDevice()
        {
            base.StartDevice();

            this.timer.Change(0, 1000);
        }

        public void SetForced(bool? forcedIsOpen)
        {
            this.forced = forcedIsOpen;
        }

        public bool IsOpen
        {
            get { return this.isOpen.GetValueOrDefault(); }
            set
            {
                if (this.isOpen != value)
                {
                    this.isOpen = value;

                    UpdateOutput();
                }
            }
        }

        public OperatingHours2 AddRange(string from, string to)
        {
            var range = new TimeRange
            {
                From = DateTime.Parse(from),
                To = DateTime.Parse(to)
            };

            this.ranges.Add(range);

            return this;
        }

        public OperatingHours2 ControlsMasterPower(IHasMasterPower device)
        {
            this.Output.Subscribe(device.InputMasterPower);

            return this;
        }

        protected override void UpdateOutput()
        {
            this.outputValue.OnNext(this.isOpen.GetValueOrDefault());
        }
    }
}
