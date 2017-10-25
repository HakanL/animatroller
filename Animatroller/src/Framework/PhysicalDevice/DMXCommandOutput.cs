using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Animatroller.Framework.PhysicalDevice
{
    public class DMXCommandOutput : BaseDevice, INeedsDmxOutput, IDisposable
    {
        private int startDmxChannel;
        private TimeSpan? autoResetAfter;
        private byte autoResetCommand;
        private Timer resetTimer;

        public DMXCommandOutput(IApiVersion3 logicalDevice, int startDmxChannel, TimeSpan? autoResetAfter = null, byte autoResetCommand = 0)
            : base(logicalDevice)
        {
            this.startDmxChannel = startDmxChannel;
            this.autoResetAfter = autoResetAfter;
            this.autoResetCommand = autoResetCommand;
            this.resetTimer = new Timer(ResetTimerCallback);

            if (logicalDevice is ISendsData sendsData)
            {
                sendsData.OutputChanged.Subscribe(x =>
                {
                    OutputFromIData(logicalDevice, x);
                });
            }
        }

        public IDmxOutput DmxOutputPort { protected get; set; }

        private void ResetTimerCallback(object state)
        {
            this.resetTimer.Change(Timeout.Infinite, Timeout.Infinite);

            lock (this)
            {
                DmxOutputPort.SendDimmerValue(this.startDmxChannel, this.autoResetCommand);
            }
        }

        protected virtual void OutputFromIData(ILogicalDevice logicalDevice, IData data)
        {
            if (data.TryGetValue(DataElements.Command, out object value))
            {
                if (value is byte[] arr)
                {
                    byte lastByte = this.autoResetCommand;

                    lock (this)
                    {
                        foreach (byte b in arr)
                        {
                            DmxOutputPort.SendDimmerValue(this.startDmxChannel, b);

                            lastByte = b;
                        }
                    }

                    if (this.autoResetAfter.HasValue && lastByte != this.autoResetCommand)
                        this.resetTimer.Change(this.autoResetAfter.Value, TimeSpan.FromMilliseconds(-1));
                    else
                        this.resetTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        public void Dispose()
        {
            this.resetTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            this.resetTimer?.Dispose();
            this.resetTimer = null;
        }
    }
}
