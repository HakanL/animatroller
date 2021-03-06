﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class SingleOwnerOutputDevice : SingleOwnerDevice, IHasMasterPower
    {
        private bool currentMasterPower;
        protected IObserver<bool> inputMasterPower;

        public SingleOwnerOutputDevice(string name)
            : base(name)
        {
            // Default on
            this.currentMasterPower = true;

            this.inputMasterPower = Observer.Create<bool>(x =>
            {
                if (this.currentMasterPower != x)
                {
                    this.currentMasterPower = x;

                    RefreshOutput();
                }
            });
        }

        public IObserver<bool> InputMasterPower
        {
            get { return this.inputMasterPower; }
        }

        public bool MasterPower
        {
            get { return this.currentMasterPower; }
        }

        public override void SetInitialState()
        {
            base.SetInitialState();

            UpdateOutput();
        }
    }
}
