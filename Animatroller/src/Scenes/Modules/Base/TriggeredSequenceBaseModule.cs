using System;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework;

namespace Animatroller.Scenes.Modules
{
    public class TriggeredSequence : TriggeredBaseModule
    {
        private Controller.Subroutine powerOnSub = new Controller.Subroutine();
        private Controller.Subroutine powerOffSub = new Controller.Subroutine();

        public TriggeredSequence([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            OutputTrigger.Subscribe(x =>
            {
                if (x)
                    Executor.Current.Execute(this.powerOnSub);
                else
                    Executor.Current.Execute(this.powerOffSub);

            });

            OutputPower.Subscribe(x =>
            {
                if (x)
                    Executor.Current.Cancel(this.powerOffSub);
                else
                    Executor.Current.Cancel(this.powerOnSub);
            });
        }

        public Controller.Subroutine PowerOn
        {
            get { return this.powerOnSub; }
        }

        public Controller.Subroutine PowerOff
        {
            get { return this.powerOffSub; }
        }

        protected override void LockDevices(params IOwnedDevice[] devices)
        {
            base.LockDevices(devices);

            this.powerOnSub.SetControlToken(this.controlToken);
            this.powerOffSub.SetControlToken(this.controlToken);
        }

        protected override void UnlockDevices()
        {
            base.UnlockDevices();

            this.powerOnSub.SetControlToken(this.controlToken);
            this.powerOffSub.SetControlToken(this.controlToken);
        }
    }
}
