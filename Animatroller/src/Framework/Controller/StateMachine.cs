using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Controller
{
    public class StateMachine<T> : IRunnable where T : struct, IConvertible
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private string name;
        private Tuple<System.Threading.CancellationTokenSource, Task> currentJob;
        protected object lockObject = new object();
        protected Dictionary<T, Sequence.SequenceJob> stateConfigs;
        protected T currentState;
        private Stack<T> momentaryStates;

        public StateMachine(string name)
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            this.name = name;
            this.stateConfigs = new Dictionary<T, Sequence.SequenceJob>();
            this.currentJob = null;
            this.momentaryStates = new Stack<T>();

            Executor.Current.Register(this);
        }

        public T CurrentState
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

        public StateMachine<T> ForFromSequence(T state, Sequence sequence)
        {
            var seqJob = (sequence.WhenExecuted as Sequence.SequenceJob);
            this.stateConfigs[state] = seqJob;

            return this;
        }

        public StateMachine<T> SetMomentaryState(T newState)
        {
            if (IsIdle)
                this.InternalSetState(newState);
            else
            {
                lock (lockObject)
                {
                    this.momentaryStates.Push(this.currentState);
                }

                this.InternalSetState(newState);
            }

            return this;
        }

        public StateMachine<T> SetState(T newState)
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

            Hold();

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
                        }
                    }, this.name, out jobTask);

                lock (lockObject)
                {
                    this.currentJob = new Tuple<System.Threading.CancellationTokenSource, Task>(
                        cancelSource, jobTask);
                    this.currentState = newState;
                }
            }
        }

        public StateMachine<T> Hold()
        {
            Tuple<System.Threading.CancellationTokenSource, Task> jobToCancel = null;
            lock (lockObject)
            {
                // Cancel
                if (this.currentJob != null)
                    jobToCancel = this.currentJob;
                this.currentJob = null;
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

            return this;
        }

        public void Start()
        {
        }

        public void Stop()
        {
            Hold();
        }
    }
}
