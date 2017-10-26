using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Animatroller.Framework;
using Animatroller.Framework.Interface;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Scenes.Modules
{
    public class PoweredBaseModule : BaseModule, IDisposable
    {
        private ControlSubject<bool> inputPower;
        protected GroupControlToken controlToken = null;

        public PoweredBaseModule([System.Runtime.CompilerServices.CallerMemberName] string name = "", bool defaultPower = true)
            : base(name)
        {
            this.inputPower = new ControlSubject<bool>(defaultPower);

            this.inputPower.Subscribe(x =>
            {
                this.log.Verbose("Power for {Name} changed to {Power}", Name, x);
            });
        }

        protected virtual void LockDevices(params IOwnedDevice[] devices)
        {
            // Dispose if we already have a lock
            this.controlToken?.Dispose();

            this.controlToken = new GroupControlToken(new List<IOwnedDevice>(devices), null, Name);
        }

        protected virtual void UnlockDevices()
        {
            this.controlToken?.Dispose();
            this.controlToken = null;
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

        public void Dispose()
        {
            this.controlToken?.Dispose();
            this.controlToken = null;
        }
    }
}
