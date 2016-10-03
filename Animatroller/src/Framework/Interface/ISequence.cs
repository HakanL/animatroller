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

        ISequenceInstance WaitFor(TimeSpan value);

        ISequenceInstance WaitFor(TimeSpan value, bool throwExceptionIfCanceled);

        ISequenceInstance WaitUntilCancel();

        ISequenceInstance WaitUntilCancel(bool throwExceptionIfCanceled);

        IControlToken Token { get; }
    }

    public interface ISequenceInstance2 : ISequenceInstance
    {
    }

    public interface IRunnableState
    {
        IRunnableState SetUp(Action<ISequenceInstance> action);

        IRunnableState TearDown(Action<ISequenceInstance> action);

        IRunnableState Execute(Action<ISequenceInstance> action);
    }
}
