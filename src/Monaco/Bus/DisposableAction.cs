using System;

namespace Monaco.Bus
{
	public class DisposableAction : IDisposableAction
	{
		private readonly Action _action;

		public DisposableAction(Action action)
		{
			_action = action;
		}

		public bool IsDisposed { get; private set; }

		public void Dispose()
		{
			Disposing(true);
			GC.SuppressFinalize(this);
		}

		public void Disposing(bool disposing)
		{
			if (IsDisposed) return;

			if (_action != null)
				_action();

			IsDisposed = true;
		}

		~DisposableAction()
		{
			this.Disposing(true);
		}

	}
}