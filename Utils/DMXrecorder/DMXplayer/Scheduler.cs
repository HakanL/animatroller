using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using Haukcode.HighResolutionTimer;
using Haukcode.sACN;

namespace Animatroller.DMXplayer
{
    public class Scheduler : IDisposable
    {
        public enum SyncModes
        {
            Manual = 0,
            RepeatUniverseId,
            Timestamp
        }

        public const int BufferLowMark = 5;
        public const int BufferHighMark = 10;

        public class SendData
        {
            public int UniverseId { get; set; }

            public byte[] DmxData { get; set; }

            public byte? Priority { get; set; }
        }

        private readonly HighResolutionTimer sendTimer;
        private readonly ManualResetEvent fillEvent = new ManualResetEvent(true);
        private readonly ManualResetEvent queueEmpty = new ManualResetEvent(true);
        private readonly ManualResetEvent runningEvent = new ManualResetEvent(false);
        private bool running;
        private readonly Queue<(double TimestampMS, IList<SendData> Data, bool EndOfData)> sendQueue = new Queue<(double TimestampMS, IList<SendData> Data, bool EndOfData)>();
        private readonly Dictionary<int, byte[]> currentData = new Dictionary<int, byte[]>();
        private byte? priority = null;
        private double currentTimestampMS = -1;
        private readonly object lockObject = new object();
        private readonly Stopwatch masterClock = new Stopwatch();
        private readonly ISubject<(double TimestampMS, long PlayedFrames)> progressSubject = new Subject<(double TimestampMS, long PlayedFrames)>();
        private readonly IOutput output;
        private readonly int progressReportPeriodMS;
        private readonly int periodMS;
        private int sendSyncAddress;

        public Scheduler(IOutput output, int periodMS = 25, int sendSyncUniverseId = 0, int progressReportPeriodMS = 1000)
        {
            if (periodMS < 10)
                throw new ArgumentOutOfRangeException(nameof(periodMS));

            this.output = output;
            this.periodMS = periodMS;
            this.sendSyncAddress = sendSyncUniverseId;
            this.progressReportPeriodMS = progressReportPeriodMS;

            this.sendTimer = new HighResolutionTimer();
            this.sendTimer.SetPeriod(this.periodMS);

            this.running = true;

            ThreadPool.QueueUserWorkItem(Sender);
        }

        public int PeriodMS => this.periodMS;

        public double MasterClockMS => this.masterClock.Elapsed.TotalMilliseconds;

        public WaitHandle FillBuffer => this.fillEvent;

        public WaitHandle QueueEmpty => this.queueEmpty;

        public long PlayedFrames { get; private set; } = 0;

        public void StartOutput()
        {
            this.runningEvent.Set();
        }

        public IObservable<(double TimestampMS, long PlayedFrames)> ProgressFrames => this.progressSubject.AsObservable();

        private void Sender(object state)
        {
            var sendDataList = new List<SendData>();

            // Wait until we're ready to run
            this.runningEvent.WaitOne();

            bool done = false;
            double lastReported = 0;

            while (this.running)
            {
                double playTimestamp = MasterClockMS;

                try
                {
                    bool dataSent = false;

                    if (sendDataList.Any())
                    {
                        dataSent = true;

                        foreach (var sendData in sendDataList)
                        {
                            //Console.WriteLine($"Playing frame {PlayedFrames + 1} with priority {sendData.Priority}");

                            this.output.SendDmx(sendData.UniverseId, sendData.DmxData, sendData.Priority, this.sendSyncAddress);

                            PlayedFrames++;
                        }

                        if (this.sendSyncAddress > 0)
                        {
                            this.output.SendSync(this.sendSyncAddress);
                        }

                        sendDataList.Clear();

                        if (playTimestamp - lastReported > this.progressReportPeriodMS)
                        {
                            this.progressSubject.OnNext((playTimestamp, PlayedFrames));
                            lastReported = playTimestamp;
                        }
                    }

                    if (done)
                    {
                        this.sendTimer.WaitForTrigger();
                        break;
                    }

                    double endTimestamp;
                    if (dataSent)
                        endTimestamp = playTimestamp + this.periodMS + this.periodMS / 2;
                    else
                        endTimestamp = playTimestamp + this.periodMS / 2;

                    // Loop until we have everything we need to send
                    lock (this.lockObject)
                    {
                        while (true)
                        {
                            if (!this.sendQueue.TryPeek(out var result))
                                break;

                            if (result.TimestampMS > endTimestamp)
                                // Wait until next period with some tolerance
                                break;

                            if (!this.sendQueue.TryDequeue(out var sendDataResult))
                                break;

                            sendDataList.AddRange(sendDataResult.Data);

                            this.queueEmpty.Reset();

                            if (sendDataResult.EndOfData)
                                done = true;
                        }
                    }

                    if (this.sendQueue.Count < BufferLowMark)
                        this.fillEvent.Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error when sending: {ex.Message}");
                }

                if (!this.masterClock.IsRunning)
                {
                    Console.WriteLine("Start timer");

                    this.sendTimer.Start();
                    this.masterClock.Restart();
                }
                else
                {
                    this.sendTimer.WaitForTrigger();

                    //Console.WriteLine($"Test timer {this.masterClock.Elapsed.TotalMilliseconds:N2}");
                }
            }

            // Output completed
            this.queueEmpty.Set();
        }

        public bool IsQueueFilled => this.sendQueue.Count >= BufferLowMark;

        public bool IsClockRunning => this.masterClock.IsRunning;

        public void AddData(double timestampMS, int universeId, byte[] dmxData, SyncModes syncMode = SyncModes.Timestamp, byte? priority = null)
        {
            switch (syncMode)
            {
                case SyncModes.RepeatUniverseId:
                    if (this.currentData.ContainsKey(universeId))
                        SendCurrentData(false);
                    break;

                case SyncModes.Timestamp:
                    if (Math.Abs(timestampMS - this.currentTimestampMS) > (this.periodMS / 3))
                        SendCurrentData(false);
                    break;
            }

            this.currentData[universeId] = dmxData;
            this.priority = priority;
            if (this.currentTimestampMS == -1)
                this.currentTimestampMS = timestampMS;
        }

        public void SendCurrentData(bool endOfData)
        {
            var queueData = new List<SendData>();
            foreach (var kvp in this.currentData)
            {
                queueData.Add(new SendData
                {
                    UniverseId = kvp.Key,
                    DmxData = kvp.Value,
                    Priority = this.priority
                });
            }

            lock (this.lockObject)
            {
                if (queueData.Any())
                    this.sendQueue.Enqueue((this.currentTimestampMS, queueData.OrderBy(x => x.UniverseId).ToList(), endOfData));

                if (this.sendQueue.Count > BufferHighMark)
                    // Don't need anything more in the queue at the moment
                    this.fillEvent.Reset();
            }

            this.currentData.Clear();
            this.currentTimestampMS = -1;
        }

        public void EndOfData()
        {
            if (this.currentData.Any())
            {
                SendCurrentData(true);
            }
        }

        public void Dispose()
        {
            this.running = false;
            this.runningEvent.Set();
            this.fillEvent.Set();
            this.sendTimer.Dispose();
        }
    }
}
