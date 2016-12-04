using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Subjects;

namespace Animatroller.Framework.LogicalDevice
{
    public class BrightnessPowerAdapter
    {
        protected IObserver<DoubleZeroToOne> inputBrightness;

        public BrightnessPowerAdapter(DigitalOutput2 device)
        {
            this.inputBrightness = Observer.Create<DoubleZeroToOne>(x =>
                {
                    device.SetValue(x.Value > 0.1);
                });
        }

        public IObserver<DoubleZeroToOne> InputBrightness
        {
            get
            {
                return this.inputBrightness;
            }
        }
    }
}
