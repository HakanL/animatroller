using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Serilog;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class CommandDevice : SingleOwnerDevice, IReceivesData
    {
        public CommandDevice([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        public override void BuildDefaultData(IData data)
        {
        }

        public void SendCommand(IControlToken token, params byte[] command)
        {
            SetData(channel: Channel.Main, token: token, data: new Data(DataElements.Command, (object)command));
        }
    }
}
