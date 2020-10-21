using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Atomex.Guard.Common
{
    public abstract class ShedulerTask<T>
    {
        public enum Status
        {
            Passed,
            Failed,
            Wait
        }

        public class TaskResult<TResult>
        {
            public Status Status { get; set; }
            public TResult Value { get; set; }
            public TimeSpan Delay { get; set; }
        }

        public abstract Task<TaskResult<T>> DoAsync(T value, CancellationToken cancellationToken = default);
    }

    public enum FailureAction
    {
        Return,
        Pass
    }

    public class Sheduler<T>
    {
        private readonly IList<(ShedulerTask<T>, FailureAction, Sheduler<T>)> _tasks;

        public Sheduler()
        {
            _tasks = new List<(ShedulerTask<T>, FailureAction, Sheduler<T>)>();
        }

        public Sheduler<T> AddTask(
            ShedulerTask<T> task,
            FailureAction failureAction = FailureAction.Return,
            Sheduler<T> onFailure = null)
        {
            _tasks.Add((task, failureAction, onFailure));
            return this;
        }

        public async Task RunAsync(T value, CancellationToken cancellationToken = default)
        {
            foreach (var (task, failureAction, onFailure) in _tasks)
            {
                var result = await task.DoAsync(value, cancellationToken);

                if (result.Status == ShedulerTask<T>.Status.Failed)
                {
                    if (onFailure != null)
                        await onFailure.RunAsync(result.Value, cancellationToken);

                    if (failureAction == FailureAction.Return)
                        return; // todo: save info about failed task
                }

                value = result.Value;
            }
        }
    }
}