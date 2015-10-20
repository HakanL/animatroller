using Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Effect
{
    public class PopOut2 : GroupDimmer
    {
        protected class DeviceController : Controller.BaseDeviceController<IReceivesBrightness>
        {
            public ControlledObserver<double> BrightnessOwner { get; set; }

            public DeviceController(IReceivesBrightness device, int priority)
                : base(device, priority)
            {
            }
        }

        private double defaultStartBrightness;
        private TimeSpan defaultSweepDuration;
        private List<Func<Action>> takeControlActions;

        public PopOut2(
            TimeSpan defaultSweepDuration,
            double defaultStartBrightness = 1.0,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.defaultStartBrightness = defaultStartBrightness;
            this.defaultSweepDuration = defaultSweepDuration;

            this.takeControlActions = new List<Func<Action>>();
        }

        public Task Pop(double? startBrightness = null, int priority = 1, TimeSpan? sweepDuration = null)
        {
            var token = TakeControl(priority: priority, name: Name);

            var disposables = this.takeControlActions
                .Select(x => x())
                .ToList();

            return Executor.Current.MasterEffect.Fade(
                device: this,
                start: startBrightness ?? this.defaultStartBrightness,
                end: 0.0,
                durationMs: (int)(sweepDuration ?? this.defaultSweepDuration).TotalMilliseconds,
                token: token)
                .ContinueWith(x =>
                {
                    token.Dispose();
                    disposables.ForEach(d => d());
                });
        }

        public void ConnectTo(IReceivesBrightness device)
        {
            Add(device);

            //var colorDevice = device as IReceivesColor;
            //if (colorDevice != null)
            //{
            //    this.takeControlActions.Add(() =>
            //    {
            //        System.Drawing.Color currentColor = colorDevice.Color;

            //        colorDevice.Color = System.Drawing.Color.Purple;

            //        return () =>
            //        {
            //            colorDevice.Color = currentColor;
            //        };
            //    });
            //}
        }
    }
}

