﻿using System;
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
        System.Threading.CancellationToken CancelToken { get; }
        bool IsCancellationRequested { get; }
        ISequenceInstance WaitFor(TimeSpan value);
        ISequenceInstance WaitFor(TimeSpan value, bool throwExceptionIfCanceled);
        ISequenceInstance WaitUntilCancel();
        ISequenceInstance WaitUntilCancel(bool throwExceptionIfCanceled);
    }

    public interface IRunnableState
    {
        IRunnableState SetUp(Action action);
        IRunnableState TearDown(Action action);
        IRunnableState Execute(Action<ISequenceInstance> action);
    }
}
