using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.StateMachine;

namespace Monaco.Extensibility.Storage.Impl.Volatile
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

		#region IDisposable Members

		public void Dispose()
		{
			Disposing(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		public override IEnumerable<TStateMachineData> FindAll()
		{
			IEnumerable<TStateMachineData> instances = new List<TStateMachineData>();

			lock (_lock)
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

			if (_disposed) return data;

			object result = null;
			_instanceStore.TryGetValue(instanceId, out result);

			if (result == null ||
			    typeof (TStateMachineData).IsAssignableFrom(result.GetType()) == false) return data;

			return result as TStateMachineData;
		}

		public override void Save(TStateMachineData stateMachineData)
		{
			if (_disposed || stateMachineData == null) return;

			lock (_lock)
			{
				if (this.Find(stateMachineData.Id) != null)
				{
					Remove(stateMachineData.Id);
				}

				_instanceStore.TryAdd(stateMachineData.Id, stateMachineData);
			}
		}

		public override void Remove(Guid instanceId)
		{
			if (_disposed) return;
			object data = null;

			lock (_lock)
				_instanceStore.TryRemove(instanceId, out data);
		}

		private void Disposing(bool disposing)
		{
			if (disposing)
			{
				if (_instanceStore != null)
				{
					_instanceStore.Clear();
				}
				_instanceStore = null;


				_disposed = true;
			}
		}
	}
}