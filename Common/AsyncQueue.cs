using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Atomex.Common
{
    public interface IAsyncCollection<T> : IEnumerable<T>
    {
        int Count { get; }
        void Add(T item);
        Task<T> TakeAsync(CancellationToken cancellationToken = default);
    }

    public class AsyncQueue<T> : IAsyncCollection<T>
    {
        private readonly ConcurrentQueue<T> _itemQueue = new ConcurrentQueue<T>();
        private readonly ConcurrentQueue<TaskCompletionSource<T>> _awaiterQueue = new ConcurrentQueue<TaskCompletionSource<T>>();

        private long _queueBalance = 0;

        public int Count => _itemQueue.Count;

        public IEnumerator<T> GetEnumerator() =>  _itemQueue.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Add(T item)
		{
			while (!TryAdd(item)) ;
		}

		public void Clear()
        {
			_itemQueue.Clear();
		}

		public Task<T> TakeAsync(CancellationToken cancellationToken = default)
		{
			var balanceAfterCurrentAwaiter = Interlocked.Decrement(ref _queueBalance);

			if (balanceAfterCurrentAwaiter < 0)
			{
				var taskSource = new TaskCompletionSource<T>();
				_awaiterQueue.Enqueue(taskSource);

				cancellationToken.Register(
					state =>
					{
						var awaiter = state as TaskCompletionSource<T>;
						awaiter.TrySetCanceled();
					},
					taskSource,
					useSynchronizationContext: false);

				return taskSource.Task;
			}
			else
			{
				T item;
				var spin = new SpinWait();

				while (!_itemQueue.TryDequeue(out item))
					spin.SpinOnce();

				return Task.FromResult(item);
			}
		}

        private bool TryAdd(T item)
		{
			var balanceAfterCurrentItem = Interlocked.Increment(ref _queueBalance);

			if (balanceAfterCurrentItem > 0)
			{
				_itemQueue.Enqueue(item);
				return true;
			}
			else
			{
				TaskCompletionSource<T> awaiter;
				var spin = new SpinWait();

				while (!_awaiterQueue.TryDequeue(out awaiter))
					spin.SpinOnce();

				return awaiter.TrySetResult(item);
			}
		}
	}
}