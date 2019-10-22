using System;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reactive;

namespace Animatroller.Scenes.Modules
{
    public class TriggeredSubBaseModule : TriggeredBaseModule
    {
        private Controller.Subroutine powerOnSub = new Controller.Subroutine();
        private Controller.Subroutine powerOffSub = new Controller.Subroutine();
        private EventLoopScheduler scheduler = new EventLoopScheduler();
        private IObserver<bool> stateChecker;
        private bool transition;
        private bool reportMasterStatus;
        private Controller.Subroutine currentSub;
        private Controller.Subroutine requestedSub;

        public TriggeredSubBaseModule([System.Runtime.CompilerServices.CallerMemberName] string name = "", bool reportMasterStatus = true)
            : base(name)
        {
            this.reportMasterStatus = reportMasterStatus;

            this.stateChecker = Observer.NotifyOn(Observer.Create<bool>(_ =>
                {
                    if (this.transition)
                    {
                        if (this.currentSub != null)
                        {
                            this.currentSub.RequestCancel();
                            Executor.Current.Cancel(this.currentSub);
                        }

                        // Transition done, let's exit and check again
                        this.transition = false;
                        this.currentSub = null;
                        QueueCheckState();
                        return;
                    }

                    if (this.currentSub != this.requestedSub)
                    {
                        if (this.requestedSub == null)
                        {
                            // Request to turn off
                            this.transition = true;
                            QueueCheckState();
                            return;
                        }
                        else
                        {
                            if (this.currentSub != null)
                            {
                                this.currentSub.RequestCancel();
                                Executor.Current.Cancel(this.currentSub);
                            }

                            var sub = this.requestedSub;
                            this.currentSub = sub;
                            Executor.Current.Execute(sub);
                            QueueCheckState();
                        }
                    }
                }), this.scheduler);

            WireupLifeCycle(this.powerOffSub);
            WireupLifeCycle(this.powerOnSub);

            OutputTrigger.Subscribe(x =>
            {
                if (x.Trigger)
                {
                    this.requestedSub = (x.Power || AlwaysUsePowerOnSub) ? this.powerOnSub : this.powerOffSub;
                }
                else
                {
                    this.requestedSub = null;
                }

                QueueCheckState();
            });
        }

        public bool AlwaysUsePowerOnSub { get; set; }

        private void WireupLifeCycle(Controller.Subroutine sub)
        {
            sub.Lifecycle.Subscribe(lc =>
            {
                switch (lc)
                {
                    case Controller.Subroutine.LifeCycles.Running:
                    case Controller.Subroutine.LifeCycles.RunningLoop:
                        // Check if we shouldn't run
                        if (this.requestedSub != this.currentSub)
                            this.currentSub?.RequestCancel();
                        break;

                    case Controller.Subroutine.LifeCycles.Setup:
                        if (this.reportMasterStatus)
                            Executor.Current.LogMasterStatus(Name, true);
                        break;

                    case Controller.Subroutine.LifeCycles.Stopped:
                        if (this.reportMasterStatus)
                            Executor.Current.LogMasterStatus(Name, false);

                        QueueCheckState();
                        break;
                }
            });
        }

        private void QueueCheckState()
        {
            this.stateChecker.OnNext(false);
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
