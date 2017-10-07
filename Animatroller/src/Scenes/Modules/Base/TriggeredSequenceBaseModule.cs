using System;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework;

namespace Animatroller.Scenes.Modules
{
    public class TriggeredSequence : TriggeredBaseModule
    {
        private Controller.Sequence powerOnSeq = new Controller.Sequence();
        private Controller.Sequence powerOffSeq = new Controller.Sequence();

        public TriggeredSequence([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            OutputTrigger.Subscribe(x =>
            {
                if (x)
                    Executor.Current.Execute(this.powerOnSeq);
                else
                    Executor.Current.Execute(this.powerOffSeq);

            });

            OutputPower.Subscribe(x =>
            {
                if (x)
                    Executor.Current.Cancel(this.powerOffSeq);
                else
                    Executor.Current.Cancel(this.powerOnSeq);
            });
        }

        public Controller.Sequence PowerOnSeq
        {
            get { return this.powerOnSeq; }
        }

        public Controller.Sequence PowerOffSeq
        {
            get { return this.powerOffSeq; }
        }
    }
}
