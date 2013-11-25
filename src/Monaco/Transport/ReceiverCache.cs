using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Monaco.Bus;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Collections;
using Timer = System.Timers.Timer;

namespace Monaco.Transport
{
	public class ReceiverCache : IStartable
	{
		private static object cacheLock = new object();
		private IDictionary<string, IEnvelope> _cache;
		private bool _disposing;
		private bool _isBusy;
		private const double _sweep_interval = 1000 * 60 * 3; // Three minutes 
		private Timer _timer;

		#region IStartable Members

		public void Dispose()
		{
			IsRunning = false;
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public bool IsRunning { get; private set; }

		public void Start()
		{
			_cache = new Dictionary<string, IEnvelope>();
			_timer = new Timer(_sweep_interval);
			_timer.Elapsed += OnTimerElapsed;
			_timer.Start();
		}

		public void Stop()
		{
			Dispose();
		}

		#endregion

		public IEnvelope Receive(IEnvelope envelope)
		{
			IEnvelope cachedMessage = null;

			if (_disposing) return cachedMessage;

			while (_isBusy)
			{
			}

			if (CachePeek(envelope))
			{
				return cachedMessage;
			}
			else
			{
				cachedMessage = Cache(envelope);
			}

			return cachedMessage;
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			_isBusy = true;
			CleanCache();
		}

		private void Dispose(bool disposing)
		{
			_disposing = disposing;

			if (_disposing)
			{
				if (_timer != null)
				{
					_timer.Stop();
					_timer.Elapsed -= OnTimerElapsed;
					_timer.Dispose();
				}

				_timer = null;

				if (_cache != null)
				{
					_cache.Clear();
				}

				_cache = null;
			}
		}

		private bool CachePeek(IEnvelope envelope)
		{
			lock (cacheLock)
			{
				return _cache.ContainsKey(envelope.Header.MessageId);
			}
		}

		private IEnvelope Cache(IEnvelope envelope)
		{
			lock (cacheLock)
			{
				try
				{
					_cache.Add(envelope.Header.MessageId, envelope);
				}
				catch
				{
					// already there..move on
				}
			}
			return envelope;
		}

		private void CleanCache()
		{
			lock (cacheLock)
			{
				try
				{
					_cache.Clear();
					_isBusy = false;
				}
				finally
				{
				}
			}
		}
	}
}