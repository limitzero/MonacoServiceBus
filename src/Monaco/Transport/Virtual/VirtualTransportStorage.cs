using System.Collections.Concurrent;
using System.Threading;
using Monaco.Bus;
using Monaco.Bus.Internals.Collections;

namespace Monaco.Transport.Virtual
{
	public class VirtualTransportStorage
	{
		private static readonly ReaderWriterLockSlim StorageLock = new ReaderWriterLockSlim();
		private static VirtualTransportStorage instance;
		private static ConcurrentDictionary<string, IThreadSafeList<IEnvelope>> storage;

		private VirtualTransportStorage()
		{
		}

		~VirtualTransportStorage()
		{
			instance = null;

			if (storage != null)
			{
				storage.Clear();
			}
			storage = null;
		}

		public static VirtualTransportStorage GetInstance()
		{
			if (instance == null)
			{
				instance = new VirtualTransportStorage();
				storage = new ConcurrentDictionary<string, IThreadSafeList<IEnvelope>>();
			}
			return instance;
		}

		public void Initialize(string endpoint)
		{
			StorageLock.EnterWriteLock();
			try
			{
				var envelopes = new ThreadSafeList<IEnvelope>();
				storage.TryAdd(endpoint, envelopes);
			}
			finally
			{
				StorageLock.ExitWriteLock();
			}
		}

		public void Enqueue(string endpoint, IEnvelope envelope)
		{
			StorageLock.EnterWriteLock();
			try
			{
				IThreadSafeList<IEnvelope> envelopes = null;
				storage.TryGetValue(endpoint, out envelopes);

				if (envelopes != null)
				{
					envelopes.Add(envelope);
				}
			}
			finally
			{
				StorageLock.ExitWriteLock();
			}
		}

		public IEnvelope Dequeue(string endpoint)
		{
			IEnvelope envelope = null;

			StorageLock.EnterWriteLock();
			try
			{
				IThreadSafeList<IEnvelope> envelopes = null;
				storage.TryGetValue(endpoint, out envelopes);

				if (envelopes != null && envelopes.Count > 0)
				{
					// FIFO:
					envelope = envelopes[0];

					if (envelope != null)
					{
						envelopes.Remove(envelope);
					}
				}
			}
			finally
			{
				StorageLock.ExitWriteLock();
			}

			return envelope;
		}
	}
}