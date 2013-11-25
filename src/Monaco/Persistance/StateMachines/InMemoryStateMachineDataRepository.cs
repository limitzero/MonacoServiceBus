using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Monaco.Sagas;

namespace Monaco.Persistance.StateMachines
{
	/// <summary>
	/// State machine repository to store instance data for the state machine in-memory (volitile storage).
	/// </summary>
	/// <typeparam name="TStateMachineData">Type of the data for the state machine</typeparam>
	public class InMemoryStateMachineDataRepository<TStateMachineData> :
		BaseStateMachineDataRepository<TStateMachineData>, IDisposable
		where TStateMachineData : class, IStateMachineData, new()
	{
		private static readonly object _lock = new object();
		private static ConcurrentDictionary<Guid, object> _instanceStore;
		private bool _disposed;

		public InMemoryStateMachineDataRepository()
		{
			if (_instanceStore == null)
				_instanceStore = new ConcurrentDictionary<Guid, object>();
		}

		public override IEnumerable<TStateMachineData> FindAll()
		{
			IEnumerable<TStateMachineData> instances = new List<TStateMachineData>();

			lock(_lock)
			{
				instances = (from match in _instanceStore.Values
				             where typeof (TStateMachineData).IsAssignableFrom(match.GetType())
				             select match as TStateMachineData).ToList().Distinct();
			}

			return instances;
		}

		public override TStateMachineData DoFindStateMachineData(Guid instanceId)
		{
			TStateMachineData data = default(TStateMachineData);

			if (this._disposed) return data;

			object result = null;
			_instanceStore.TryGetValue(instanceId, out result);

			if (result == null ||
				typeof(TStateMachineData).IsAssignableFrom(result.GetType()) == false) return data;

			return result as TStateMachineData;
		}

		public override void Save(TStateMachineData stateMachineData)
		{
			if (this._disposed || stateMachineData == null) return;

			lock (_lock)
			{
				if (this.Find(stateMachineData.CorrelationId) != null)
				{
					this.Remove(stateMachineData.CorrelationId);

				}

				_instanceStore.TryAdd(stateMachineData.CorrelationId, stateMachineData);
			}
		}

		public override void Remove(Guid instanceId)
		{
			if (this._disposed) return;
			object data = null;

			lock (_lock)
				_instanceStore.TryRemove(instanceId, out data);
		}

		public void Dispose()
		{
			Disposing(true);
			GC.SuppressFinalize(this);
		}

		private void Disposing(bool disposing)
		{
			if (disposing == true)
			{
				if (_instanceStore != null)
				{
					_instanceStore.Clear();
				}
				_instanceStore = null;


				this._disposed = true;
			}
		}
	}
}