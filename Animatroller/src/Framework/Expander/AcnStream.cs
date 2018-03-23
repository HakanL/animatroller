using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using Animatroller.Framework.PhysicalDevice;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;
using Serilog;
using kadmium_sacn_core;
using System.Threading;
using System.Diagnostics;
using System.Reactive;

namespace Animatroller.Framework.Expander
{
    public class AcnStream : IPort, IRunnable, IOutputHardware
    {
        public readonly Guid animatrollerAcnId = new Guid("{53A974B9-8286-4DC1-BFAB-00FEC91FD7A9}");
        protected ILogger log;
        private Timer keepAliveTimer;

        protected class AcnPixelUniverse : IPixelOutput
        {
            private object lockObject = new object();
            private AcnStream acnStream;
            private Dictionary<int, AcnUniverse> acnUniverses;
            private int startUniverse;
            private int startDmxChannel;

            public AcnPixelUniverse(AcnStream acnStream, int startUniverse, int startDmxChannel)
            {
                this.acnStream = acnStream;
                this.startUniverse = startUniverse;
                this.startDmxChannel = startDmxChannel;

                this.acnUniverses = new Dictionary<int, AcnUniverse>();
            }

            protected AcnUniverse GetAcnUniverse(int universe)
            {
                AcnUniverse acnUniverse;
                lock (lockObject)
                {
                    if (!this.acnUniverses.TryGetValue(universe, out acnUniverse))
                    {
                        acnUniverse = this.acnStream.GetSendingUniverse(universe);
                        this.acnUniverses.Add(universe, acnUniverse);
                    }
                }

                return acnUniverse;
            }

            public SendStatus SendPixelValue(int channel, PixelRGBByte rgb)
            {
                var values = new byte[3];
                values[0] = rgb.R;
                values[1] = rgb.G;
                values[2] = rgb.B;

                // Max 510 RGB values per universe
                int universe = (this.startDmxChannel + (channel * 3)) / 510;
                int localStart = (this.startDmxChannel + (channel * 3)) % 510;

                var acnUniverse = GetAcnUniverse(this.startUniverse + universe);

                return acnUniverse.SendDimmerValues(localStart, values, 0, 3);
            }

            public SendStatus SendPixelsValue(int channel, PixelRGBByte[] rgb)
            {
                // Max 510 RGB values per universe
                int universe = (this.startDmxChannel + (channel * 3)) / 510;
                int localStart = (this.startDmxChannel + (channel * 3)) % 510;

                var acnUniverse = GetAcnUniverse(this.startUniverse + universe);

                int chn = 0;
                var values = new byte[3 * rgb.Length];
                foreach (var rgbValue in rgb)
                {
                    values[chn++] = rgbValue.R;
                    values[chn++] = rgbValue.G;
                    values[chn++] = rgbValue.B;

                    if (chn + localStart > 510)
                    {
                        acnUniverse.SendDimmerValues(localStart, values, 0, chn);

                        // Get next universe
                        chn = 0;
                        universe++;
                        localStart = 1;
                        acnUniverse = GetAcnUniverse(this.startUniverse + universe);
                    }
                }

                if (chn > 0)
                    acnUniverse.SendDimmerValues(localStart, values, 0, chn);

                return SendStatus.NotSet;
            }

            public void SendPixelsValue(int channel, byte[] rgb, int length)
            {
                // Max 510 RGB values per universe
                int universe = (this.startDmxChannel + (channel * 3)) / 510;
                int localStart = (this.startDmxChannel + (channel * 3)) % 510;

                int startOffset = 0;
                while (startOffset < length)
                {
                    var acnUniverse = GetAcnUniverse(this.startUniverse + universe);

                    int maxChannels = Math.Min(511 - localStart, length - startOffset);
                    acnUniverse.SendDimmerValues(localStart, rgb, startOffset, maxChannels);

                    startOffset += maxChannels;
                    universe++;
                    localStart = 1;
                }
            }

            public void SendPixelsValue(int channel, byte[][] dmxData)
            {
                for (int universe = 0; universe < dmxData.Length; universe++)
                {
                    var acnUniverse = GetAcnUniverse(this.startUniverse + universe);
                    acnUniverse.SendDimmerValues(1, dmxData[universe], 0, dmxData[universe].Length);
                }
            }
        }

        protected class AcnUniverse : IDmxOutput
        {
            public const long KeepAliveMilliseconds = 2000;

            private short universe;
            private byte priority;
            private object lockObject = new object();
            private AcnStream parent;
            private byte[] currentData;
            private Stopwatch lastSendWatch;

            public AcnUniverse(AcnStream parent, int universe, byte priority)
            {
                this.universe = (short)universe;
                this.parent = parent;
                this.priority = priority;

                this.currentData = new byte[512];
                this.lastSendWatch = new Stopwatch();

                var observer = Observer.Create<int>(
                    onNext: pos =>
                    {
                        ActuallySendCurrentData();
                    });

                Executor.Current.TimerJobRunner.AddTimerJobCounter(observer);
            }

            public bool IsDueForKeepAlive
            {
                get
                {
                    if (Executor.Current.IsOffline)
                        return false;

                    return this.lastSendWatch.ElapsedMilliseconds >= KeepAliveMilliseconds;
                }
            }

            public void SendKeepAlive()
            {
                //this.parent.log.Verbose("Sending sACN Keep Alive");
                SendCurrentData();
            }

            private void SendCurrentData()
            {
                // Do nothing
            }

            private void ActuallySendCurrentData()
            {
                lock (this.lockObject)
                {
                    this.parent.acnSender.Send(this.universe, this.currentData, this.priority);
                }

                this.lastSendWatch.Restart();
            }

            public SendStatus SendDimmerValue(int channel, byte value)
            {
                lock (this.lockObject)
                {
                    this.currentData[channel - 1] = value;
                }

                if (!Executor.Current.IsOffline)
                    SendCurrentData();

                return SendStatus.NotSet;
            }

            public SendStatus SendDimmerValues(int firstChannel, byte[] values)
            {
                return SendDimmerValues(firstChannel, values, 0, values.Length);
            }

            public SendStatus SendDimmerValues(int firstChannel, byte[] values, int offset, int length)
            {
                lock (this.lockObject)
                {
                    for (int i = 0; i < length; i++)
                    {
                        int chn = firstChannel + i;
                        if (chn >= 1 && chn <= 512)
                            this.currentData[chn - 1] = values[offset + i];
                    }
                }

                if (!Executor.Current.IsOffline)
                    SendCurrentData();

                return SendStatus.NotSet;
            }
        }

        private object lockObject = new object();
        private kadmium_sacn_core.SACNSender acnSender;
        private Dictionary<int, AcnUniverse> sendingUniverses;
        private int defaultPriority;

        public AcnStream(int priority = 100)
        {
            this.defaultPriority = priority;
            this.log = Log.Logger;
            this.acnSender = new SACNSender(animatrollerAcnId, "Animatroller");

            this.sendingUniverses = new Dictionary<int, AcnUniverse>();

            this.keepAliveTimer = new Timer(KeepAliveTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

            Executor.Current.Register(this);
        }

        private void KeepAliveTimerCallback(object state)
        {
            try
            {
                var list = new List<AcnUniverse>();

                lock (this.lockObject)
                {
                    foreach (var kvp in this.sendingUniverses)
                    {
                        if (kvp.Value.IsDueForKeepAlive)
                            list.Add(kvp.Value);
                    }
                }

                foreach (var universe in list)
                {
                    universe.SendKeepAlive();
                }
            }
            catch (Exception ex)
            {
                this.log.Warning(ex, "Exception in KeepAliveTimerCallback");
            }
        }

        protected AcnUniverse GetSendingUniverse(int universe, int? priority = null)
        {
            AcnUniverse acnUniverse;
            lock (this.lockObject)
            {
                if (!this.sendingUniverses.TryGetValue(universe, out acnUniverse))
                {
                    acnUniverse = new AcnUniverse(this, universe, (byte)(priority ?? this.defaultPriority));

                    this.sendingUniverses.Add(universe, acnUniverse);
                }
            }

            return acnUniverse;
        }

        protected AcnPixelUniverse GetPixelSendingUniverse(int startUniverse, int startDmxChannel)
        {
            return new AcnPixelUniverse(this, startUniverse, startDmxChannel);
        }

        private IPAddress GetAddressFromInterfaceType(NetworkInterfaceType interfaceType)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.SupportsMulticast && adapter.NetworkInterfaceType == interfaceType &&
                    adapter.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();

                    foreach (var ipAddress in ipProperties.UnicastAddresses)
                    {
                        if (ipAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            return ipAddress.Address;
                    }
                }
            }

            return null;
        }

        public AcnStream Connect(PhysicalDevice.INeedsDmxOutput device, int universe)
        {
            device.DmxOutputPort = GetSendingUniverse(universe);

            return this;
        }

        public AcnStream Connect(PhysicalDevice.INeedsPixelOutput device, int startUniverse, int startDmxChannel)
        {
            device.PixelOutputPort = GetPixelSendingUniverse(startUniverse, startDmxChannel);

            return this;
        }

        public AcnStream Connect(PhysicalDevice.INeedsPixel2DOutput device, int startUniverse)
        {
            device.PixelOutputPort = GetPixelSendingUniverse(startUniverse, 1);

            return this;
        }

        public void Start()
        {
            this.keepAliveTimer.Change(1000, 1000);
        }

        public void Stop()
        {
            this.keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);

            lock (this.lockObject)
            {
                this.sendingUniverses.Clear();
            }
        }
    }
}
