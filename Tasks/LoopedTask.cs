using System.Threading;
using System.Threading.Tasks;

using Atomex.Guard.Common;

namespace Atomex.Guard.Tasks
{
    public abstract class LoopedTask<T> : ShedulerTask<T>
    {
        public override async Task<TaskResult<T>> DoAsync(
            T value,
            CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await DoInLoopAsync(value, cancellationToken);

                if (result.Status == Status.Passed || result.Status == Status.Failed)
                    return result;

                await Task.Delay(result.Delay);

                value = await UpdateValueAsync(value);
            }

            return new TaskResult<T> { Status = Status.Failed, Value = value };
        }

        protected abstract Task<T> UpdateValueAsync(
            T value,
            CancellationToken cancellationToken = default);

        protected abstract Task<TaskResult<T>> DoInLoopAsync(
            T value,
            CancellationToken cancellationToken = default);
    }
}