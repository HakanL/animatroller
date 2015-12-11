﻿using System;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledObserverData : IObserver<IData>
    {
        private IControlToken controlToken;
        private ControlSubject<IData, IControlToken> control;

        public ControlledObserverData(IControlToken controlToken, ControlSubject<IData, IControlToken> control)
        {
            this.controlToken = controlToken;
            this.control = control;
        }

        public void OnCompleted()
        {
            // We don't want to end the underlaying IData observer
//            this.control.OnCompleted();
        }

        public void OnError(Exception error)
        {
            this.control.OnError(error);
        }

        public void OnNext(IData value)
        {
            value.CurrentToken = this.controlToken;

            this.control.OnNext(value, this.controlToken);
        }
    }
}
