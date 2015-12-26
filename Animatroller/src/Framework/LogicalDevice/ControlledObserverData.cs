using System;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledObserverData : IObserver<IData>
    {
        private IControlToken controlToken;
        private ControlSubject<IData, IControlToken> control;
        private IData data;

        public ControlledObserverData(IControlToken controlToken, ControlSubject<IData, IControlToken> control, IData data)
        {
            this.controlToken = controlToken;
            this.control = control;
            this.data = data;
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
            this.data.CurrentToken = this.controlToken;

            foreach (var kvp in value)
                this.data[kvp.Key] = kvp.Value;

            this.control.OnNext(this.data, this.controlToken);
        }
    }
}
