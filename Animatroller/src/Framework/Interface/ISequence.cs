using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public interface ISequence : ICanExecute
    {
        ISequence MultiInstance { get; }
        ISequence Loop { get; }
        IRunnableState WhenExecuted { get; }
    }


    public interface ISequenceInstance
    {
        string Id { get; }

        System.Threading.CancellationToken CancelToken { get; }

        bool IsCancellationRequested { get; }

        TimeSpan Runtime { get; }

        void AbortIfCanceled();

        ISequenceInstance WaitFor(TimeSpan value, bool abortImmediatelyIfCanceled = false);

        ISequenceInstance WaitUntilCancel(bool throwExceptionIfCanceled = true);

        IControlToken Token { get; }

        void Stop();

        int IterationCounter { get; }
    }

    public interface ISequenceInstance2 : ISequenceInstance
    {
    }

    public interface IRunnableState
    {
        IRunnableState SetUp(Action<ISequenceInstance> action);

        IRunnableState TearDown(Action<ISequenceInstance> action);

        IRunnableState Execute(Action<ISequenceInstance> action);

        IRunnableState Controls(int priority = 1, params Effect.IEffect[] effects);
    }
}
