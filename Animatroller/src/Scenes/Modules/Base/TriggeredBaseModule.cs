using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.Interface;

namespace Animatroller.Scenes.Modules
{
    public class TriggeredBaseModule : PoweredBaseModule
    {
        private Subject<bool> inputTrigger = new Subject<bool>();
        private ControlSubject<bool> inputTriggerBlock = new ControlSubject<bool>(false);
        private Subject<bool> trigger = new Subject<bool>();

        public TriggeredBaseModule([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputTrigger.Subscribe(x =>
            {
                if (x)
                {
                    if (!Power)
                        this.log.Verbose("{Name} has been triggered, but power is off", Name);
                    else
                    {
                        if (this.inputTriggerBlock.Value)
                            this.log.Verbose("{Name} has been triggered, but is blocked", Name);
                        else
                        {
                            this.log.Verbose("{Name} has been triggered", Name);
                            this.trigger.OnNext(true);
                        }
                    }
                }
            });

            this.inputTriggerBlock.Subscribe(x =>
            {
                this.log.Verbose("Trigger block for {Name} changed to {TriggerBlock}", Name, x);
            });
        }

        public IObserver<bool> InputTrigger
        {
            get { return this.inputTrigger.AsObserver(); }
        }

        public IObserver<bool> InputTriggerBlock
        {
            get { return this.inputTriggerBlock.AsObserver(); }
        }

        public IObservable<bool> OutputTrigger
        {
            get { return this.trigger.AsObservable(); }
        }
    }
}
