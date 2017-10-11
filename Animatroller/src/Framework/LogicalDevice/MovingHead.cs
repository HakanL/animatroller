using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class MovingHead : StrobeColorDimmer3, IReceivesPanTilt
    {
        public MovingHead([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        public override void BuildDefaultData(IData data)
        {
            base.BuildDefaultData(data);

            data[DataElements.Pan] = 0.0;
            data[DataElements.Tilt] = 0.0;
        }

        public double Pan
        {
            get { return (double)this.currentData[DataElements.Pan]; }
        }

        public double Tilt
        {
            get { return (double)this.currentData[DataElements.Tilt]; }
        }

        public void SetPanTilt(double pan, double tilt, IControlToken token)
        {
            this.SetData(token,
                Tuple.Create(DataElements.Pan, (object)pan),
                Tuple.Create(DataElements.Tilt, (object)tilt)
                );
        }

        public void SetPan(double pan, IControlToken token)
        {
            this.SetData(token, Tuple.Create(DataElements.Pan, (object)pan));
        }

        public void SetTilt(double tilt, IControlToken token)
        {
            this.SetData(token, Tuple.Create(DataElements.Tilt, (object)tilt));
        }
    }
}
