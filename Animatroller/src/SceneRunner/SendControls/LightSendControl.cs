using System;
using Animatroller.AdminMessage;
using Animatroller.Framework;

namespace Animatroller.SceneRunner.SendControls
{
    public class LightSendControl : Framework.PhysicalDevice.BaseStrobeLight, ISendControl
    {
        private bool performUpdate;
        private readonly Action updateAvailable;
        private bool isOwned;

        public LightSendControl(IApiVersion3 logicalDevice, string componentId, Action updateAvailable)
            : base(logicalDevice)
        {
            this.ComponentId = componentId;
            this.updateAvailable = updateAvailable;
            /*            if (logicalDevice is ISendsData sendsData)
                        {
                            sendsData.OutputChanged.Subscribe(data =>
                            {
                                object value;

                                if (data.TryGetValue(DataElements.Brightness, out value))
                                {


                                }
                                //SetFromIData(logicalDevice, x);

                                //Output();
                            });

                            //SetFromIData(logicalDevice, sendsData.CurrentData);
                        }*/

            /*            parent.AddUpdateAction(() =>
            {
                lock (lockObject)
                {
                    if (this.performUpdate)
                    {
                        this.performUpdate = false;

                        if (this.hasNewData)
                            this.control.Invalidate();
                    }
                }
            });*/
        }

        public string ComponentId { get; }

        public ComponentType ComponentType => ComponentType.StrobeColorDimmer;

        public object GetMessageToSend()
        {
            if (!this.performUpdate)
                return null;

            var msg = new AdminMessage.StrobeColorDimmer
            {
                Brightness = this.colorBrightness.Brightness,
                Red = this.colorBrightness.Color.R,
                Green = this.colorBrightness.Color.G,
                Blue = this.colorBrightness.Color.B,
                Owned = this.isOwned
            };

            return msg;
        }

        protected override void Output()
        {
            this.isOwned = false;

            foreach (ILogicalDevice device in this.logicalDevices)
            {
                if (device is IOwnedDevice && ((IOwnedDevice)device).IsOwned)
                {
                    this.isOwned = true;
                    break;
                }
            }

            this.performUpdate = true;
            this.updateAvailable();
        }
    }
}
