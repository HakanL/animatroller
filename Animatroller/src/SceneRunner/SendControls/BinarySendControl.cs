using System;
using Animatroller.AdminMessage;
using Animatroller.Framework;

namespace Animatroller.SceneRunner.SendControls
{
    public class BinarySendControl : ISendControl
    {
        private bool performUpdate;
        private readonly Action updateAvailable;
        private bool isOwned;
        private bool currentValue;
        protected ILogicalDevice logicalDevice;

        public BinarySendControl(IApiVersion3 logicalDevice, string componentId, Action updateAvailable)
        {
            this.ComponentId = componentId;
            this.updateAvailable = updateAvailable;

            this.logicalDevice = logicalDevice;

            if (logicalDevice is ISendsData sendsData)
            {
                sendsData.OutputChanged.Subscribe(x =>
                {
                    SetFromIData(logicalDevice, x);

                    Output();
                });

                SetFromIData(logicalDevice, sendsData.CurrentData);
            }
        }

        public string ComponentId { get; }

        public ComponentType ComponentType => ComponentType.Binary;

        public object GetMessageToSend()
        {
            if (!this.performUpdate)
                return null;

            var msg = new AdminMessage.Binary
            {
                Value = this.currentValue,
                Owned = this.isOwned
            };

            return msg;
        }

        protected virtual void SetFromIData(ILogicalDevice logicalDevice, IData data)
        {
            bool? power = data.GetValue<bool>(DataElements.Power);
            if (power.HasValue)
                this.currentValue = power.Value;
        }

        protected void Output()
        {
            this.isOwned = false;

            var device = this.logicalDevice;
            if (device is IOwnedDevice && ((IOwnedDevice)device).IsOwned)
            {
                this.isOwned = true;
            }

            this.performUpdate = true;
            this.updateAvailable();
        }
    }
}
