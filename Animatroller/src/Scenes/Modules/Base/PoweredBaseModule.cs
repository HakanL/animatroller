using System;
using System.Reactive;
using System.Reactive.Linq;
using Animatroller.Framework;
using Animatroller.Framework.Interface;

namespace Animatroller.Scenes.Modules
{
    public class PoweredBaseModule : BaseModule
    {
        private ControlSubject<bool> inputPower = new ControlSubject<bool>(false);

        public PoweredBaseModule([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.inputPower.Subscribe(x =>
            {
                this.log.Verbose("Power for {Name} changed to {Power}", Name, x);
            });
        }

        public IObserver<bool> InputPower
        {
            get { return this.inputPower.AsObserver(); }
        }

        protected IObservable<bool> OutputPower
        {
            get { return this.inputPower.AsObservable(); }
        }

        protected bool Power
        {
            get { return this.inputPower.Value; }
        }
    }
}
