using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Controller
{
    public class StateChangedStringEventArgs : EventArgs
    {
        public string NewState { get; private set; }

        public StateChangedStringEventArgs(string newState)
        {
            this.NewState = newState;
        }
    }

    public interface IStateMachine
    {
        event EventHandler<StateChangedStringEventArgs> StateChangedString;
        string CurrentStateString { get; }
        string Name { get; }
    }

    public class EnumStateMachine<T> : IRunnable, IStateMachine where T : struct, IConvertible
    {
        public class StateChangedEventArgs : EventArgs
        {
            public T? NewState { get; private set; }

            public StateChangedEventArgs(T? newState)
            {
                this.NewState = newState;
            }
        }

        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<StateChangedStringEventArgs> StateChangedString;

        protected static Logger log = LogManager.GetCurrentClassLogger();
        private string name;
        private Tuple<System.Threading.CancellationTokenSource, Task> currentJob;
        protected object lockObject = new object();
        protected Dictionary<T, Sequence.SequenceJob> stateConfigs;
        protected T? currentState;
        protected T? nextState;
        private Stack<T> momentaryStates;
        private T? backgroundState;

        public EnumStateMachine(string name)
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            this.name = name;
            this.stateConfigs = new Dictionary<T, Sequence.SequenceJob>();
            this.currentJob = null;
            this.momentaryStates = new Stack<T>();

            Executor.Current.Register(this);
        }

        private void RaiseStateChanged()
        {
            var handler = StateChanged;
            if (handler != null)
                handler(this, new StateChangedEventArgs(this.CurrentState));

            var handlerString = StateChangedString;
            if(handlerString != null)
                handlerString(this, new StateChangedStringEventArgs(this.CurrentStateString));
        }

        public EnumStateMachine<T> SetBackgroundState(T? backgroundState)
        {
            this.backgroundState = backgroundState;

            return this;
        }

        public string CurrentStateString
        {
            get { return this.CurrentState == null ? null : this.CurrentState.Value.ToString(); }
        }

        public EnumStateMachine<T> NextState()
        {
            var values = Enum.GetValues(typeof(T));
            for (int i = 0; i < values.Length; i++)
            {
                if (values.GetValue(i).Equals(CurrentState))
                {
                    i++;
                    if (i < values.Length)
                    {
                        this.nextState = (T)values.GetValue(i);
                        if (IsIdle)
                            SetState(this.nextState.Value);
                    }
                    else
                    {
                        this.nextState = this.backgroundState;
                        if (IsIdle)
                        {
                            if (this.nextState.HasValue)
                                SetState(this.nextState.Value);
                            else
                                Hold();
                        }
                    }
                    break;
                }
            }

            return this;
        }

        /// <summary>
        /// Don't call this from within a sequence (running job)
        /// </summary>
        /// <returns></returns>
        public EnumStateMachine<T> StopAndNextState()
        {
            var values = Enum.GetValues(typeof(T));
            for (int i = 0; i < values.Length; i++)
            {
                if (values.GetValue(i).Equals(CurrentState))
                {
                    i++;
                    if (i < values.Length)
                    {
                        SetState((T)values.GetValue(i));
                    }
                    else
                    {
                        if (this.backgroundState.HasValue)
                            SetState(this.nextState.Value);
                        else
                            Hold();
                    }
                    break;
                }
            }

            return this;
        }

        public T? CurrentState
        {
            get { return this.currentState; }
        }

        public IRunnableState For(T state)
        {
            Sequence.SequenceJob stateConfig;
            if (!this.stateConfigs.TryGetValue(state, out stateConfig))
            {
                stateConfig = new Sequence.SequenceJob(this.name);
                this.stateConfigs.Add(state, stateConfig);
            }

            return stateConfig;
        }

        public EnumStateMachine<T> ForFromSequence(T state, Sequence sequence)
        {
            var seqJob = (sequence.WhenExecuted as Sequence.SequenceJob);
            this.stateConfigs[state] = seqJob;

            return this;
        }

        public EnumStateMachine<T> SetMomentaryState(T newState)
        {
            if (IsIdle)
                this.InternalSetState(newState);
            else
            {
                lock (lockObject)
                {
                    if(this.currentState.HasValue)
                        this.momentaryStates.Push(this.currentState.Value);
                }

                this.InternalSetState(newState);
            }

            return this;
        }

        public EnumStateMachine<T> SetState(T newState)
        {
            lock (lockObject)
            {
                // Kill queue of momentary states
                this.momentaryStates.Clear();
            }

            InternalSetState(newState);

            return this;
        }

        public bool IsIdle
        {
            get { return this.currentJob == null; }
        }

        protected void InternalSetState(T newState)
        {
            lock (lockObject)
            {
                if (this.nextState.HasValue)
                    this.nextState = null;

                if (!IsIdle)
                {
                    if (this.currentState.Equals(newState))
                    {
                        // Already in this state
                        log.Info("Already in state {0}", newState);
                        return;
                    }
                }
            }

            InternalHold();

            Sequence.SequenceJob sequenceJob;
            lock (lockObject)
            {
                this.stateConfigs.TryGetValue(newState, out sequenceJob);
            }

            if (sequenceJob != null)
            {
                Task jobTask;
                System.Threading.CancellationTokenSource cancelSource;

                cancelSource = Executor.Current.Execute(jobCancelToken =>
                    {
                        sequenceJob.Run(jobCancelToken, false);

                        lock (lockObject)
                        {
                            this.currentJob = null;
                        }

                        if (!jobCancelToken.IsCancellationRequested)
                        {
                            T? popState = null;
                            lock (lockObject)
                            {
                                if (this.momentaryStates.Any())
                                {
                                    popState = this.momentaryStates.Pop();
                                }
                            }
                            if (popState.HasValue)
                                InternalSetState(popState.Value);
                            else if (nextState.HasValue)
                                InternalSetState(this.nextState.Value);
                            else if (this.backgroundState.HasValue)
                                InternalSetState(this.backgroundState.Value);
                            else
                                Hold();
                        }
                    }, this.name, out jobTask);

                lock (lockObject)
                {
                    this.currentJob = new Tuple<System.Threading.CancellationTokenSource, Task>(
                        cancelSource, jobTask);
                    this.currentState = newState;
                }
            }
            RaiseStateChanged();
        }

        public EnumStateMachine<T> Hold()
        {
            InternalHold();

            RaiseStateChanged();

            return this;
        }

        public void InternalHold()
        {
            Tuple<System.Threading.CancellationTokenSource, Task> jobToCancel = null;
            lock (lockObject)
            {
                // Cancel
                if (this.currentJob != null)
                    jobToCancel = this.currentJob;
                this.currentJob = null;
                this.currentState = null;
            }

            if (jobToCancel != null)
            {
                jobToCancel.Item1.Cancel();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                if (!jobToCancel.Item2.Wait(5000))
                    log.Info("State {0} failed to cancel in time", this.currentState);
                watch.Stop();
                log.Info("State {0} took {1:N1}ms to stop", this.currentState, watch.Elapsed.TotalMilliseconds);
            }
        }

        public void Start()
        {
        }

        public void Stop()
        {
            InternalHold();
        }

        public string Name
        {
            get { return this.name; }
        }
    }
}
