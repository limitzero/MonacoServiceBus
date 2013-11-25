using System;

namespace Monaco.Bus.Internals.Disposable
{
	/// <summary>
	/// Abstract class to represent the Disposable pattern
	/// </summary>
	public abstract class BaseDisposable : IDisposable
	{
		public bool Disposed { get; private set; }

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		public void Dispose(bool disposing)
		{
			if (!Disposed)
			{
				if (disposing)
				{
					ReleaseManagedResources();
				}

				CallBaseObjectDispose();

				ReleaseUnManagedResources();

				Disposed = true;
			}
		}

		/// <summary>
		/// This is the point in the Disposable pattern where all managed resources are released from use.
		/// </summary>
		public virtual void ReleaseManagedResources()
		{
		}

		/// <summary>
		/// This is the point in the Disposable pattern where the call to the Dispose method on the base object 
		/// is called (if needed).
		/// </summary>
		public virtual void CallBaseObjectDispose()
		{
		}

		/// <summary>
		/// This is the point in the Disposable pattern where all un-managed resources are released from use.
		/// </summary>
		public virtual void ReleaseUnManagedResources()
		{
		}

		public virtual void GuardOnDisposed()
		{
			if(this.Disposed)
				throw new ObjectDisposedException("Can not access a disposed object.");
		}
	}
}