using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Monaco.Bus.MessageManagement.Callbacks;

namespace Monaco.Bus.Internals.Threading
{
	/// <summary>
	/// The result of an asynchronous operation.
	/// 
	/// Credits: http://www.danrigsby.com/blog/index.php/2008/03/26/async-operations-in-wcf-iasyncresult-model-server-side/
	/// 
	/// </summary>
	public class AsyncResult : IAsyncResult, IDisposable
	{
		private AsyncCallback _callback;
		private bool _disposed;
		private object _state;
		private ManualResetEvent _wait;

		public AsyncResult()
			: this(null, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncResult"/> class.
		/// </summary>
		/// <param name="callback">The callback.</param>
		/// <param name="state">The state.</param>
		public AsyncResult(
			AsyncCallback callback,
			object state)
		{
			_callback = callback;
			_state = state;
			_wait = new ManualResetEvent(false);
		}

		public ICallback ServiceBusCallback { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance is disposed.
		/// </summary>
		/// <value>
		///     <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
		/// </value>
		[Browsable(false)]
		public bool IsDisposed
		{
			get { return _disposed; }
		}

		#region IAsyncResult Members

		/// <summary>
		/// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
		/// </summary>
		/// <value></value>
		/// <returns>A user-defined object that qualifies or contains information about an asynchronous operation.</returns>
		public object AsyncState
		{
			get { return _state; }
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.</returns>
		public WaitHandle AsyncWaitHandle
		{
			get { return _wait; }
		}

		/// <summary>
		/// Gets an indication of whether the asynchronous operation completed synchronously.
		/// </summary>
		/// <value></value>
		/// <returns>true if the asynchronous operation completed synchronously; otherwise, false.</returns>
		public bool CompletedSynchronously
		{
			get { return false; }
		}

		/// <summary>
		/// Gets an indication whether the asynchronous operation has completed.
		/// </summary>
		/// <value></value>
		/// <returns>true if the operation is complete; otherwise, false.</returns>
		public bool IsCompleted
		{
			get { return _wait.WaitOne(0, false); }
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		public void Dispose()
		{
			if (!_disposed)
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		#endregion

		private event EventHandler _disposedEventHandler;

		public void InitializeCallbackAndState(AsyncCallback callback, object state)
		{
			_callback = callback;
			_state = state;
		}

		/// <summary>
		/// Completes this instance.
		/// </summary>
		public virtual void OnCompleted(IAsyncResult asyncResult = null)
		{
			_wait.Set();
			if (_callback != null)
			{
				if (asyncResult != null)
				{
					_callback(asyncResult);
				}
				else
				{
					_callback(this);
				}
			}
		}

		/// <summary>
		/// Occurs when this instance is disposed.
		/// </summary>
		public event EventHandler Disposed
		{
			add { _disposedEventHandler += value; }
			remove { _disposedEventHandler -= value; }
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					_wait.Close();
					_wait = null;
					_state = null;
					_callback = null;

					EventHandler handler = _disposedEventHandler;
					if (handler != null)
					{
						handler(this, EventArgs.Empty);
						handler = null;
					}
				}
			}
			finally
			{
				_disposed = true;
			}
		}

		/// <summary>
		///    <para>
		///        Checks if the instance has been disposed of, and if it has, throws an <see cref="ObjectDisposedException"/>; otherwise, does nothing.
		///    </para>
		/// </summary>
		/// <exception cref="System.ObjectDisposedException">
		///    The instance has been disposed of.
		///    </exception>
		///    <remarks>
		///    <para>
		///        Derived classes should call this method at the start of all methods and properties that should not be accessed after a call to <see cref="Dispose()"/>.
		///    </para>
		/// </remarks>
		protected void CheckDisposed()
		{
			if (_disposed)
			{
				string typeName = GetType().FullName;

				throw new ObjectDisposedException(
					typeName,
					String.Format(CultureInfo.InvariantCulture,
					              "Cannot access a disposed {0}.",
					              typeName));
			}
		}
	}
}