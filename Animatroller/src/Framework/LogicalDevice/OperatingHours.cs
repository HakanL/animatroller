using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class OperatingHours : BaseDevice
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

        public event EventHandler<Event.OpenHoursEventArgs> OpenHoursChanged;

        public OperatingHours([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.isOpen = null;
            this.ranges = new List<TimeRange>();
            this.timer = new Timer(x =>
                {
                    EvaluateOpenHours();
                }, null, Timeout.Infinite, Timeout.Infinite);
        }

        protected void RaiseOpenHoursChanged()
        {
            var handler = OpenHoursChanged;
            if (handler != null)
                handler(this, new Event.OpenHoursEventArgs(IsOpen));
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

        public override void SetInitialState()
        {
            base.SetInitialState();

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

        public OperatingHours AddRange(string from, string to)
        {
            var range = new TimeRange
            {
                From = DateTime.Parse(from),
                To = DateTime.Parse(to)
            };

            this.ranges.Add(range);

            return this;
        }

        protected override void UpdateOutput()
        {
            RaiseOpenHoursChanged();
        }
    }
}
