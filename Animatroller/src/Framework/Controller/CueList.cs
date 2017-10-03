using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice;
using Serilog;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Controller
{
    public class CueList : IRunnable
    {
        internal class CueInstance
        {
            public Action AllTasksDone { get; set; }

            private Dictionary<Tuple<ILogicalDevice, Cue.CueParts>, Task> tasks;

            public CueInstance()
            {
                this.tasks = new Dictionary<Tuple<ILogicalDevice, Cue.CueParts>, Task>();
            }

            public void StopExistingTask(ILogicalDevice device, Cue.CueParts cuePart)
            {
                var key = Tuple.Create(device, cuePart);

                Task existingTask;
                if (this.tasks.TryGetValue(key, out existingTask))
                {
                    // Stop existing
                    Executor.Current.StopManagedTask(existingTask);
                }
            }

            public void AddNewTask(ILogicalDevice device, Cue.CueParts cuePart, Task task, Action mibCheck = null)
            {
                var key = Tuple.Create(device, cuePart);

                this.tasks[key] = task;

                task.ContinueWith(x =>
                    {
                        this.tasks.Remove(key);
                    });

                if (mibCheck != null)
                {
                    task.ContinueWith(x =>
                        {
                            if (!x.IsCanceled)
                                mibCheck();
                        });
                }
            }

            public Task[] GetAllTasks()
            {
                return this.tasks.Values.ToList().Where(x => x != null).ToArray();
            }

            public void StopAllExistingTasks()
            {
                foreach (var task in this.tasks.Values)
                {
                    Executor.Current.StopManagedTask(task);
                }
            }
        }

        protected ILogger log;
        //        private Effect.Transformer.EaseInOut easeTransform = new Effect.Transformer.EaseInOut();

        private int? iterations;
        private int? iterationsLeft;
        private List<Cue> cues;
        private Dictionary<IReceivesBrightness, ControlledObserver<double>> deviceObserversBrightness;
        private Dictionary<IReceivesColor, ControlledObserver<Color>> deviceObserversColor;
        private Dictionary<IReceivesPanTilt, ControlledObserver<double>> deviceObserversPan;
        private Dictionary<IReceivesPanTilt, ControlledObserver<double>> deviceObserversTilt;
        private int currentCuePos;
        private int? nextCuePos;
        private int direction;
        private bool bounce;
        private Dictionary<IOwnedDevice, IControlToken> heldLocks;
        private string name;
        private Task cueListExecutor;
        private CancellationTokenSource cancelSource;
        private ManualResetEvent triggerNext;
        private CueInstance currentInstance;
        private Dictionary<Cue, CancellationTokenSource> triggerCancelSources;
        private Stopwatch cueListTime;
        private int? requestedCueId;
        private ReplaySubject<int?> currentCueId;
        private Subject<Tuple<int, long>> cueCompleted;

        public int LockPriority { get; set; }

        public string Name
        {
            get { return this.name; }
        }

        public IObservable<int?> CurrentCueId
        {
            get { return this.currentCueId.AsObservable(); }
        }

        public IObservable<Tuple<int, long>> CueCompleted
        {
            get { return this.cueCompleted.AsObservable(); }
        }

        private void LockAllUsedDevices()
        {
            lock (this.heldLocks)
            {
                foreach (var device in this.cues.SelectMany(x => x.Devices).Distinct())
                {
                    var ownedDevice = device as IOwnedDevice;
                    if (ownedDevice == null)
                        continue;

                    if (this.heldLocks.ContainsKey(ownedDevice))
                        // Already locked
                        continue;

                    var token = ownedDevice.TakeControl(LockPriority, false, Name);

                    this.heldLocks.Add(ownedDevice, token);
                }
            }
        }

        private void ReleaseLocks()
        {
            lock (this.heldLocks)
            {
                foreach (var kvp in this.heldLocks)
                {
                    var brightnessDevice = kvp.Key as IReceivesBrightness;
                    if (brightnessDevice != null)
                        this.deviceObserversBrightness.Remove(brightnessDevice);

                    var colorDevice = kvp.Key as IReceivesColor;
                    if (colorDevice != null)
                        this.deviceObserversColor.Remove(colorDevice);

                    var panTiltDevice = kvp.Key as IReceivesPanTilt;
                    if (panTiltDevice != null)
                    {
                        this.deviceObserversPan.Remove(panTiltDevice);
                        this.deviceObserversTilt.Remove(panTiltDevice);
                    }

                    kvp.Value.Dispose();
                }

                this.heldLocks.Clear();
            }
        }

        private Cue AdvanceCue()
        {
            this.currentCuePos += direction;

            if (this.currentCuePos < 0)
            {
                if (this.cues.Count == 1)
                    // Nothing to go to
                    return null;

                if (!this.iterationsLeft.HasValue || this.iterationsLeft.GetValueOrDefault() > 0)
                {
                    // Start new
                    this.currentCuePos = 1;
                    this.direction = 1;
                    this.cueListTime.Restart();
                }
                else
                    return null;
            }

            if (this.currentCuePos >= this.cues.Count)
            {
                if (this.cues.Count == 1)
                    // Nothing to go to
                    return null;

                // Check for bounce/iteration/loop
                if (this.bounce)
                {
                    this.direction = -1;
                    this.currentCuePos = this.cues.Count - 2;
                }
                else
                {
                    if (this.iterationsLeft.HasValue)
                    {
                        if (this.iterationsLeft.GetValueOrDefault() <= 1)
                            return null;
                    }

                    this.currentCuePos = 0;
                    this.cueListTime.Restart();
                }
            }

            var cue = this.cues[this.currentCuePos];

            return cue;
        }

        public void Go()
        {
            if (this.currentInstance == null)
                Restart();

            this.triggerNext.Set();
        }

        public void Goto(int cueId)
        {
            if (cueId < 0 || cueId > this.cues.Count - 1)
                throw new ArgumentOutOfRangeException();

            this.requestedCueId = cueId;
            this.triggerNext.Set();
        }

        public void Abort()
        {
            if (this.currentInstance != null)
            {
                // Stop all
                this.currentInstance.StopAllExistingTasks();
            }

            this.currentCueId.OnNext(null);
            this.currentInstance = null;

            ReleaseLocks();
        }

        public void Restart()
        {
            Abort();

            this.iterationsLeft = this.iterations;
            this.currentCuePos = -1;
            this.direction = 1;
        }

        private void NextCue()
        {
            Cue cue;

            if (this.requestedCueId.HasValue)
            {
                cue = this.cues[this.requestedCueId.Value];
                this.requestedCueId = null;

                RunCue(this.requestedCueId.Value, cue, true, false);

                return;
            }

            if (this.cues.Count <= 0)
                // Nothing to do
                return;

            if (this.iterationsLeft.HasValue && this.iterationsLeft.Value == 0)
                // Done
                return;

            LockAllUsedDevices();

            cue = AdvanceCue();

            if (cue == null)
            {
                // Done
                this.currentCueId.OnNext(null);
                this.currentInstance = null;

                ReleaseLocks();
                return;
            }

            this.nextCuePos = GetNextCuePosition();

            if (this.currentCuePos == this.cues.Count - 1)
            {
                // Last cue
                if (this.iterationsLeft.HasValue)
                    this.iterationsLeft = this.iterationsLeft.Value - 1;
            }

            RunCue(this.currentCuePos, cue, false, !this.nextCuePos.HasValue);

            if (this.nextCuePos.HasValue)
            {
                var nextCue = this.cues[this.nextCuePos.Value];

                if (nextCue.Trigger == Cue.Triggers.Follow)
                {
                    CancellationTokenSource triggerCancelSource;
                    if (this.triggerCancelSources.TryGetValue(nextCue, out triggerCancelSource))
                    {
                        triggerCancelSource.Cancel();
                    }

                    triggerCancelSource = new CancellationTokenSource();
                    this.triggerCancelSources[nextCue] = triggerCancelSource;

                    Task.Delay(nextCue.TriggerTimeMs, triggerCancelSource.Token).ContinueWith(x =>
                        {
                            if (!x.IsCanceled)
                                this.triggerNext.Set();
                        });
                }
                else if (nextCue.Trigger == Cue.Triggers.CueListTime)
                {
                    //FIXME: How do we deal with this when we're playing the cuelist backwards?
                    int triggerDelay = nextCue.TriggerTimeMs - (int)this.cueListTime.ElapsedMilliseconds;

                    if (triggerDelay > 0)
                    {
                        CancellationTokenSource triggerCancelSource;
                        if (this.triggerCancelSources.TryGetValue(nextCue, out triggerCancelSource))
                        {
                            triggerCancelSource.Cancel();
                        }

                        triggerCancelSource = new CancellationTokenSource();
                        this.triggerCancelSources[nextCue] = triggerCancelSource;

                        Task.Delay(triggerDelay, triggerCancelSource.Token).ContinueWith(x =>
                        {
                            if (!x.IsCanceled)
                                this.triggerNext.Set();
                        });
                    }
                }
            }
        }

        private int? GetNextCuePosition()
        {
            if (this.cues.Count <= 0)
                // Nothing to do
                return null;

            int newPos = this.currentCuePos + direction;

            if (newPos < 0)
            {
                if (this.cues.Count == 1)
                    // Nothing to go to
                    return null;

                if (!this.iterationsLeft.HasValue || this.iterationsLeft.GetValueOrDefault() > 0)
                {
                    // Start new
                    newPos = 1;
                }
                else
                    return null;
            }

            if (newPos >= this.cues.Count)
            {
                if (this.cues.Count == 1)
                    // Nothing to go to
                    return null;

                // Check for bounce/iteration/loop
                if (this.bounce)
                {
                    newPos = this.cues.Count - 2;
                }
                else
                {
                    if (this.iterationsLeft.HasValue)
                    {
                        if (this.iterationsLeft.GetValueOrDefault() <= 1)
                            return null;
                    }

                    newPos = 0;
                }
            }

            return newPos;
        }

        private void MibCheck(int cueId, ILogicalDevice device)
        {
            //FIXME: Doesn't check for bounce/loop

            var brightnessDevice = device as IReceivesBrightness;
            if (brightnessDevice == null)
                return;

            if (brightnessDevice.Brightness > 0)
                // Not black
                return;

            for (int i = cueId + 1; i < this.cues.Count; i++)
            {
                var cue = this.cues[i];

                var panTiltDevice = device as IReceivesPanTilt;

                bool moved = false;
                if (panTiltDevice != null)
                {
                    if (cue.PartPan != null && cue.PartPan.MoveInBlack)
                    {
                        GetControlledObserverPan(panTiltDevice).OnNext(cue.PartPan.Destination);
                        moved = true;
                    }

                    if (cue.PartTilt != null && cue.PartTilt.MoveInBlack)
                    {
                        GetControlledObserverTilt(panTiltDevice).OnNext(cue.PartTilt.Destination);
                        moved = true;
                    }
                }

                if (moved)
                    break;
            }
        }

        private ControlledObserver<double> GetControlledObserverBrightness(IReceivesBrightness device)
        {
            ControlledObserver<double> observer;

            if (!this.deviceObserversBrightness.TryGetValue(device, out observer))
            {
                //FIXME
                //observer = device.GetBrightnessObserver();

                //this.deviceObserversBrightness.Add(device, observer);
            }

            return observer;
        }

        private ControlledObserver<Color> GetControlledObserverColor(IReceivesColor device)
        {
            ControlledObserver<Color> observer;

            if (!this.deviceObserversColor.TryGetValue(device, out observer))
            {
                observer = device.GetColorObserver();

                this.deviceObserversColor.Add(device, observer);
            }

            return observer;
        }

        private ControlledObserver<double> GetControlledObserverPan(IReceivesPanTilt device)
        {
            ControlledObserver<double> observer;

            if (!this.deviceObserversPan.TryGetValue(device, out observer))
            {
                observer = device.GetPanObserver();

                this.deviceObserversPan.Add(device, observer);
            }

            return observer;
        }

        private ControlledObserver<double> GetControlledObserverTilt(IReceivesPanTilt device)
        {
            ControlledObserver<double> observer;

            if (!this.deviceObserversTilt.TryGetValue(device, out observer))
            {
                observer = device.GetTiltObserver();

                this.deviceObserversTilt.Add(device, observer);
            }

            return observer;
        }

        private void RunCue(int cueId, Cue cue, bool snap, bool lastCue)
        {
            var watch = Stopwatch.StartNew();

            CancellationTokenSource cancelSource;
            if (this.triggerCancelSources.TryGetValue(cue, out cancelSource))
            {
                cancelSource.Cancel();
            }

            this.currentCueId.OnNext(cueId);

            var cueInstance = new CueInstance();

            if (cue.PartIntensity != null)
            {
                foreach (var device in cue.Devices)
                {
                    var brightnessDevice = device as IReceivesBrightness;
                    if (brightnessDevice == null)
                        continue;

                    var observer = GetControlledObserverBrightness(brightnessDevice);

                    StopCurrentTask(device, Cue.CueParts.Intensity);

                    var fadeTask = Executor.Current.MasterEffect.Fade(
                        observer,
                        brightnessDevice.Brightness,
                        cue.PartIntensity.Destination,
                        snap ? 0 : cue.PartIntensity.FadeMs);

                    cueInstance.AddNewTask(device, Cue.CueParts.Intensity, fadeTask, () => MibCheck(cueId, device));
                }
            }

            if (cue.PartColor != null)
            {
                foreach (var device in cue.Devices)
                {
                    var colorDevice = device as IReceivesColor;
                    if (colorDevice == null)
                        continue;

                    var observer = GetControlledObserverColor(colorDevice);

                    StopCurrentTask(device, Cue.CueParts.Color);

                    var fadeTask = Executor.Current.MasterEffect.Fade(
                        observer,
                        colorDevice.Color,
                        cue.PartColor.Destination,
                        snap ? 0 : cue.PartColor.FadeMs);

                    cueInstance.AddNewTask(device, Cue.CueParts.Color, fadeTask);
                }
            }

            if (cue.PartPan != null)
            {
                foreach (var device in cue.Devices)
                {
                    var panTiltDevice = device as IReceivesPanTilt;
                    if (panTiltDevice == null)
                        continue;

                    var observer = GetControlledObserverPan(panTiltDevice);

                    StopCurrentTask(device, Cue.CueParts.Pan);

                    var fadeTask = Executor.Current.MasterEffect.Fade(
                        observer,
                        panTiltDevice.Pan,
                        cue.PartPan.Destination,
                        snap ? 0 : cue.PartPan.FadeMs);

                    cueInstance.AddNewTask(device, Cue.CueParts.Pan, fadeTask);
                }
            }

            if (cue.PartTilt != null)
            {
                foreach (var device in cue.Devices)
                {
                    var panTiltDevice = device as IReceivesPanTilt;
                    if (panTiltDevice == null)
                        continue;

                    var observer = GetControlledObserverTilt(panTiltDevice);

                    StopCurrentTask(device, Cue.CueParts.Tilt);

                    var fadeTask = Executor.Current.MasterEffect.Fade(
                        observer, panTiltDevice.Tilt,
                        cue.PartTilt.Destination,
                        snap ? 0 : cue.PartTilt.FadeMs);

                    cueInstance.AddNewTask(device, Cue.CueParts.Tilt, fadeTask);
                }
            }

            this.currentInstance = cueInstance;

            var allTasks = cueInstance.GetAllTasks();

            if (allTasks.Length > 0)
            {
                Task.WhenAll(allTasks).ContinueWith(x =>
                {
                    watch.Stop();

                    this.cueCompleted.OnNext(Tuple.Create(cueId, watch.ElapsedMilliseconds));

                    if (!snap && lastCue)
                    {
                        this.currentCueId.OnNext(null);
                        this.currentInstance = null;

                        ReleaseLocks();
                    }
                });
            }
            else
            {
                watch.Stop();

                this.cueCompleted.OnNext(Tuple.Create(cueId, watch.ElapsedMilliseconds));
            }
        }

        private void StopCurrentTask(ILogicalDevice device, Cue.CueParts cuePart)
        {
            if (this.currentInstance == null)
                return;

            this.currentInstance.StopExistingTask(device, cuePart);
        }

        public CueList(int? iterations = null, bool bounce = false, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.log = Log.Logger;
            this.iterations = iterations;
            this.iterationsLeft = iterations;
            this.bounce = bounce;
            this.name = name;

            this.cues = new List<Cue>();
            this.deviceObserversBrightness = new Dictionary<IReceivesBrightness, ControlledObserver<double>>();
            this.deviceObserversColor = new Dictionary<IReceivesColor, ControlledObserver<Color>>();
            this.deviceObserversPan = new Dictionary<IReceivesPanTilt, ControlledObserver<double>>();
            this.deviceObserversTilt = new Dictionary<IReceivesPanTilt, ControlledObserver<double>>();
            this.direction = 1;
            this.currentCuePos = -1;
            this.heldLocks = new Dictionary<IOwnedDevice, IControlToken>();
            this.triggerNext = new ManualResetEvent(false);
            this.triggerCancelSources = new Dictionary<Cue, CancellationTokenSource>();
            this.cueListTime = new Stopwatch();
            this.currentCueId = new ReplaySubject<int?>(1);
            this.currentCueId.OnNext(null);
            this.cueCompleted = new Subject<Tuple<int, long>>();

            this.LockPriority = 1;

            this.cancelSource = new CancellationTokenSource();
            this.cueListExecutor = new Task(() =>
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        WaitHandle.WaitAny(new WaitHandle[] {
                            this.cancelSource.Token.WaitHandle,
                            this.triggerNext
                        });

                        if (this.cancelSource.IsCancellationRequested)
                            break;

                        this.triggerNext.Reset();

                        NextCue();
                    }
                },
                cancelSource.Token,
                TaskCreationOptions.LongRunning);

            Executor.Current.Register(this);
        }

        public CueList AddCue(Cue cue)
        {
            this.cues.Add(cue);

            return this;
        }

        public CueList AddCue(Cue cue, params ILogicalDevice[] devices)
        {
            cue.Devices.AddRange(devices);

            this.cues.Add(cue);

            return this;
        }

        public long CueListTimeMs
        {
            get { return this.cueListTime.ElapsedMilliseconds; }
        }

        public void Start()
        {
            this.cueListTime.Start();
            this.cueListExecutor.Start();
        }

        public void Stop()
        {
            this.cancelSource.Cancel();
        }
    }
}
