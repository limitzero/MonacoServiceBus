using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Monaco.Bus.Internals.Collections;

namespace Monaco.Bus.Internals.Threading
{
	public class WaitQueue : IDisposable
	{
		private CancellationToken _cancellationToken;
		private CancellationTokenSource _cancellationTokenSource;
		private bool _disposed;

		private IThreadSafeDictionary<Action, ManualResetEvent> _queue;
		private Task _task;

		public WaitQueue()
		{
			_queue = new ThreadSafeDictionary<Action, ManualResetEvent>();
		}

		#region IDisposable Members

		public void Dispose()
		{
			Disposing(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		public WaitQueue Enqueue(Action action)
		{
			if (_disposed) return null;
			_queue.Add(action, new ManualResetEvent(false));
			return this;
		}

		public void Wait()
		{
			if (_disposed) return;

			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;
			ManualResetEvent[] resetEvents = new List<ManualResetEvent>(_queue.Values).ToArray();

			_task = Task.Factory.StartNew(() =>
			                              	{
			                              		_cancellationToken.ThrowIfCancellationRequested();

			                              		foreach (var pair in _queue)
			                              		{
			                              			InvokeAction(pair.Key, pair.Value);

			                              			if (_cancellationToken.IsCancellationRequested)
			                              			{
			                              				_cancellationToken.ThrowIfCancellationRequested();
			                              			}
			                              		}
			                              	}, _cancellationToken);

			try
			{
				//_task.Wait();
				WaitHandle.WaitAll(resetEvents.ToArray());
			}
			catch (AggregateException ae)
			{
				_cancellationTokenSource.Cancel();
				throw ae;
			}
		}

		public void Cancel()
		{
			if (_disposed) return;
			if (_cancellationTokenSource != null)
			{
				_cancellationTokenSource.Cancel();
			}
		}

		private void Disposing(bool disposing)
		{
			if (disposing)
			{
				_disposed = true;

				if (_queue != null)
				{
					_queue.Clear();
				}
				_queue = null;

				if (_task != null)
				{
					if (!_task.IsCompleted || !_task.IsCanceled)
					{
						if (_cancellationTokenSource != null)
						{
							_cancellationTokenSource.Cancel();
						}
					}
				}

				_task = null;
				_cancellationTokenSource = null;
			}
		}

		private void InvokeAction(Action theAction, ManualResetEvent theEvent)
		{
			try
			{
				theAction();
			}
			catch (Exception e)
			{
				throw e;
			}
			finally
			{
				theEvent.Set();
			}
		}
	}
}