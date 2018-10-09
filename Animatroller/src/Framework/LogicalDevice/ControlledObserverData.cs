using System;

namespace Animatroller.Framework.LogicalDevice
{
    public class ControlledObserverData : IPushDataController
    {
        private IControlToken controlToken;
        private ControlSubject<(IChannel Channel, IData Data), IControlToken> control;
        private IData data;

        public ControlledObserverData(IControlToken controlToken, ControlSubject<(IChannel Channel, IData Data), IControlToken> control, IData data)
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

        //public void OnError(Exception error)
        //{
        //    this.control.OnError(error);
        //}

        public IData Data
        {
            get { return this.data; }
        }

        //public void OnNext(IData value)
        //{
        //    this.data.CurrentToken = this.controlToken;

        //    foreach (var kvp in value)
        //        this.data[kvp.Key] = kvp.Value;

        //    this.control.OnNext(this.data, this.controlToken);
        //}

        public void PushData(IChannel channel)
        {
            this.control.OnNext((channel, this.data), this.controlToken);
        }

        public void SetDataFromIData(IData source)
        {
            if (source != null)
            {
                foreach (var kvp in source)
                    this.data[kvp.Key] = kvp.Value;
            }
        }
    }
}
