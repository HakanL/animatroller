using System;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.LogicalDevice
{
    public class ColorDimmer3 : Dimmer3, IReceivesColor
    {
        public ColorDimmer3([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        public override void BuildDefaultData(IData data)
        {
            base.BuildDefaultData(data);

            data[DataElements.Color] = Color.White;
        }
    }
}
