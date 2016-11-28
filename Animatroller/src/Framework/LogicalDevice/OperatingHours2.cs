using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class OperatingHours2 : IRunnable, IInputHardware
    {
        protected class TimeRange
        {
            public DateTime From { get; set; }

            public DateTime To { get; set; }

            public List<DayOfWeek> DayOfWeek { get; set; }
        }

        private List<TimeRange> ranges;
        private Timer timer;
        private bool? isOpen;
        private bool? forced;
        private bool noRangesMeansClosed;
        private bool disabled;
        private string name;

        private ISubject<bool> outputValue;

        public OperatingHours2([System.Runtime.CompilerServices.CallerMemberName] string name = "", bool noRangesMeansClosed = true)
        {
            this.name = name;
            this.noRangesMeansClosed = noRangesMeansClosed;
            this.isOpen = null;
            this.ranges = new List<TimeRange>();
            this.disabled = false;

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
            if (this.disabled)
                return;

            if (this.forced.HasValue)
            {
                IsOpen = this.forced.Value;
                return;
            }

            bool isOpenNow = false;
            if (!this.ranges.Any())
                isOpenNow = !this.noRangesMeansClosed;

            var now = DateTime.Now.TimeOfDay;
            foreach (var range in this.ranges)
            {
                if (range.From.TimeOfDay < range.To.TimeOfDay)
                {
                    if (now >= range.From.TimeOfDay &&
                        now <= range.To.TimeOfDay)
                    {
                        if (range.DayOfWeek.Count > 0 && !range.DayOfWeek.Contains(DateTime.Today.DayOfWeek))
                            continue;

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
                        if (range.DayOfWeek.Count > 0 && !range.DayOfWeek.Contains(DateTime.Today.DayOfWeek))
                            continue;

                        isOpenNow = true;
                        break;
                    }
                }
            }

            IsOpen = isOpenNow;
        }

        public bool Disabled
        {
            get { return this.disabled; }
            set
            {
                this.disabled = value;

                EvaluateOpenHours();
            }
        }

        public void SetForced(bool? forcedIsOpen)
        {
            this.forced = forcedIsOpen;
            EvaluateOpenHours();
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

        public OperatingHours2 AddRange(string from, string to, params DayOfWeek[] daysOfWeek)
        {
            var range = new TimeRange
            {
                From = DateTime.Parse(from),
                To = DateTime.Parse(to),
                DayOfWeek = daysOfWeek.ToList()
            };

            this.ranges.Add(range);

            return this;
        }

        public OperatingHours2 ControlsMasterPower(IHasMasterPower device)
        {
            this.Output.Subscribe(device.InputMasterPower);

            return this;
        }

        private void UpdateOutput()
        {
            this.outputValue.OnNext(this.isOpen.GetValueOrDefault());
        }

        public void Start()
        {
            EvaluateOpenHours();
            // No need to call the base class to output state, EvaluateOpenHours already did that via IsOpen

            this.timer.Change(0, 1000);
        }

        public void Stop()
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}
