using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atomex.Guard.Common
{
    public class RequestLimitControl : IDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        private readonly long _delayMs;
        private long _lastTimeStampMs;

        public RequestLimitControl(long delayMs)
        {
            _delayMs = delayMs;
        }

        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            var isCompleted = false;

            while (!isCompleted)
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                await _semaphoreSlim.WaitAsync();

                var timeStampMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                var differenceMs = timeStampMs - _lastTimeStampMs;

                if (differenceMs < _delayMs)
                {
                    _semaphoreSlim.Release();

                    await Task.Delay((int)(_delayMs - differenceMs), cancellationToken);
                }
                else
                {
                    _lastTimeStampMs = timeStampMs;

                    _semaphoreSlim.Release();

                    isCompleted = true;
                }
            }
        }

        public void Dispose()
        {
            _semaphoreSlim.Dispose();
        }
    }
}