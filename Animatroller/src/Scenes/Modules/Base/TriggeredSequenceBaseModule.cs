using System;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework;

namespace Animatroller.Scenes.Modules
{
    public class TriggeredSequence : TriggeredBaseModule
    {
        private Controller.Sequence seq = new Controller.Sequence();

        public TriggeredSequence([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            OutputTrigger.Subscribe(x =>
            {
                if (x)
                    Executor.Current.Execute(this.seq);

            });

            OutputPower.Subscribe(x =>
            {
                if (!x)
                    Executor.Current.Cancel(this.seq);
            });
        }

        public Controller.Sequence Seq
        {
            get { return this.seq; }
        }
    }
}
