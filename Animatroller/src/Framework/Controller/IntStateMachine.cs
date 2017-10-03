using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Animatroller.Framework.Controller
{
    public class IntStateMachine : IRunnable, IStateMachine
    {
        public class StateChangedEventArgs : EventArgs
        {
            public int? NewState { get; private set; }

            public StateChangedEventArgs(int? newState)
            {
                this.NewState = newState;
            }
        }

        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<StateChangedStringEventArgs> StateChangedString;

        protected ILogger log;
        private string name;
        private int length;
        private Tuple<System.Threading.CancellationTokenSource, Task> currentJob;
        protected object lockObject = new object();
        protected Dictionary<int, Sequence.SequenceJob> stateConfigs;
        protected int currentState;
        protected bool hold;
        protected int? nextState;

        public IntStateMachine(string name)
        {
            this.log = Log.Logger;
            this.name = name;
            this.stateConfigs = new Dictionary<int, Sequence.SequenceJob>();
            this.currentJob = null;
            this.currentState = -1;
            this.hold = true;
            this.length = 0;

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

        public string CurrentStateString
        {
            get { return this.CurrentState == null ? null : this.CurrentState.Value.ToString(); }
        }

        public IntStateMachine NextState()
        {
            int state = this.currentState + 1;
            if (state >= this.length)
                state = 0;
            this.nextState = state;
            if(IsIdle)
                SetState(state);

            return this;
        }

        /// <summary>
        /// Don't call this from within a sequence (running job)
        /// </summary>
        /// <returns></returns>
        public IntStateMachine StopAndNextState()
        {
            int state = this.currentState + 1;
            if (state >= this.length)
                state = 0;
            SetState(state);

            return this;
        }

        public int? CurrentState
        {
            get { return this.hold ? (int?)null : this.currentState; }
        }

        public IRunnableState For(int state)
        {
            Sequence.SequenceJob stateConfig;
            if (!this.stateConfigs.TryGetValue(state, out stateConfig))
            {
                stateConfig = new Sequence.SequenceJob(this.log, this.name);
                this.stateConfigs.Add(state, stateConfig);
            }

            if (state > length - 1)
                length = state + 1;

            return stateConfig;
        }

        public IntStateMachine ForFromSequence(int state, Sequence sequence)
        {
            var seqJob = (sequence.WhenExecuted as Sequence.SequenceJob);
            this.stateConfigs[state] = seqJob;

            if (state > length - 1)
                length = state + 1;

            return this;
        }

        public IntStateMachine SetState(int newState)
        {
            InternalSetState(newState);

            return this;
        }

        public bool IsIdle
        {
            get { return this.currentJob == null; }
        }

        protected void InternalSetState(int newState)
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
                        this.log.Information("Already in state {0}", newState);
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
                            if (nextState.HasValue)
                                InternalSetState(this.nextState.Value);
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

        public IntStateMachine Hold()
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
                this.hold = true;
            }

            if (jobToCancel != null)
            {
                log.Debug("Cancel 5");
                jobToCancel.Item1.Cancel();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                if (!jobToCancel.Item2.Wait(5000))
                    this.log.Information("State {0} failed to cancel in time", this.currentState);
                watch.Stop();
                this.log.Information("State {0} took {1:N1}ms to stop", this.currentState, watch.Elapsed.TotalMilliseconds);
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
