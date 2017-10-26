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
        private ControlSubject<bool> inputTrigger = new ControlSubject<bool>(false);
        private ControlSubject<bool> inputTriggerBlock = new ControlSubject<bool>(false);
        private Subject<(bool Power, bool Trigger)> trigger = new Subject<(bool Power, bool Trigger)>();

        public TriggeredBaseModule([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputTrigger.Subscribe(x =>
            {
                if (x && this.inputTriggerBlock.Value)
                    this.log.Verbose("{Name} has been triggered, but is blocked", Name);
                else
                {
                    if (x)
                    {
                        if (Power)
                            this.log.Verbose("{Name} has been triggered, with power on", Name);
                        else
                            this.log.Verbose("{Name} has been triggered, with power off", Name);
                    }

                    this.trigger.OnNext((Power, x));
                }
            });

            this.inputTriggerBlock.Subscribe(x =>
            {
                this.log.Verbose("Trigger block for {Name} changed to {TriggerBlock}", Name, x);
            });

            OutputPower.Subscribe(p =>
            {
                // Force a trigger when power changed
                this.inputTrigger.OnNext(this.inputTrigger.Value);
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

        public IObservable<(bool Power, bool Trigger)> OutputTrigger
        {
            get { return this.trigger.AsObservable(); }
        }
    }
}
