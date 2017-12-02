using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using System.Drawing;
using Animatroller.Framework.Extensions;
using System.Reactive.Subjects;
using System.Reactive;
using System.Threading;
using Controller = Animatroller.Framework.Controller;

namespace Animatroller.Scenes.Modules
{
    public class FireProjector : PoweredBaseModule
    {
        private Subject<bool> inputTrigger1 = new Subject<bool>();
        private Subject<bool> inputTrigger2 = new Subject<bool>();
        private ControlSubject<bool> inputTriggerBlock = new ControlSubject<bool>(false);

        private Controller.Subroutine sub1 = new Controller.Subroutine();
        private Controller.Subroutine sub2 = new Controller.Subroutine();

        public FireProjector(
            DigitalOutput2 fire,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputTrigger1.Subscribe(x =>
            {
                if (x)
                {
                    if (this.inputTriggerBlock.Value)
                        this.log.Verbose("{Name} has been triggered on 1, but is blocked", Name);
                    else
                    {
                        if (Power)
                        {
                            this.log.Verbose("Trigger 1 for {Name}", Name);
                            Executor.Current.Execute(this.sub1);
                        }
                    }
                }
            });

            this.inputTrigger2.Subscribe(x =>
            {
                if (x)
                {
                    if (this.inputTriggerBlock.Value)
                        this.log.Verbose("{Name} has been triggered on 2, but is blocked", Name);
                    else
                    {
                        if (Power)
                        {
                            this.log.Verbose("Trigger 2 for {Name}", Name);
                            Executor.Current.Execute(this.sub2);
                        }
                    }
                }
            });

            this.inputTriggerBlock.Subscribe(x =>
            {
                this.log.Verbose("Trigger block for {Name} changed to {TriggerBlock}", Name, x);
            });

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    LockDevices(fire);
                }
                else
                {
                    UnlockDevices();
                }
            });

            sub1.RunAction(ins =>
            {
                fire.SetValue(true, token: this.controlToken);
                ins.WaitFor(S(0.5));
            })
            .TearDown(ins =>
            {
                fire.SetValue(false, token: this.controlToken);
            });

            sub2.RunAction(ins =>
            {
                fire.SetValue(true, token: this.controlToken);
                ins.WaitFor(S(2.0));
            })
            .TearDown(ins =>
            {
                fire.SetValue(false, token: this.controlToken);
            });
        }

        public IObserver<bool> InputTriggerBlock
        {
            get { return this.inputTriggerBlock.AsObserver(); }
        }

        public IObserver<bool> InputTriggerShort
        {
            get { return this.inputTrigger1.AsObserver(); }
        }

        public IObserver<bool> InputTriggerLong
        {
            get { return this.inputTrigger2.AsObserver(); }
        }
    }
}
