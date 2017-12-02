﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class StrobeDimmer3 : Dimmer3, IReceivesStrobeSpeed
    {
        public StrobeDimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        public override void BuildDefaultData(IData data)
        {
            base.BuildDefaultData(data);

            data[DataElements.StrobeSpeed] = 0.0;
        }

        public double StrobeSpeed
        {
            get { return (double)GetCurrentData(DataElements.StrobeSpeed); }
        }

        public void SetStrobeSpeed(double strobeSpeed, int channel = 0, IControlToken token = null)
        {
            this.SetData(channel, token, Tuple.Create(DataElements.StrobeSpeed, (object)strobeSpeed));
        }
    }
}
