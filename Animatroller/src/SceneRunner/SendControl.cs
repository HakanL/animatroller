﻿using Animatroller.Framework;
using System;

namespace Animatroller.SceneRunner
{
    public class SendControl : Framework.PhysicalDevice.BaseStrobeLight
    {
        private bool performUpdate;
        private readonly Action updateAvailable;
        private readonly string componentId;

        public SendControl(IApiVersion3 logicalDevice, string componentId, Action updateAvailable)
            : base(logicalDevice)
        {
            this.componentId = componentId;
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

        public string ComponentId => this.componentId;

        public object GetMessageToSend()
        {
            if (!this.performUpdate)
                return null;

            var msg = new AdminMessage.StrobeColorDimmer
            {
                Brightness = this.colorBrightness.Brightness
            };

            return msg;
        }

        protected override void Output()
        {
            this.performUpdate = true;
            this.updateAvailable();
        }
    }
}
