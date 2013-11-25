using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Extensibility.Storage.Timeouts;

namespace Monaco.Extensibility.Storage.Impl.Volatile
{
	public class InMemoryTimeoutsRepository : ITimeoutsRepository, IDisposable
	{
		private static readonly object TimeoutsLock = new object();
		private static IThreadSafeList<ScheduleTimeout> timeouts;
		private bool disposed;

		public InMemoryTimeoutsRepository()
		{
			if (timeouts == null)
			{
				timeouts = new ThreadSafeList<ScheduleTimeout>();
			}
		}

		public void Dispose()
		{
			this.Disposing(true);
			GC.SuppressFinalize(this);
		}

		private void Disposing(bool disposing)
		{
			if(this.disposed == false)
			{
				if(disposing == true)
				{
					if (timeouts != null)
					{
						timeouts.Clear();
						timeouts = null;
					}
				}
			}
			this.disposed = true;
		}

		public ICollection<ScheduleTimeout> FindAll(string endpoint)
		{
			if (disposed) return null;

			lock (TimeoutsLock)
			{
				// only return the timeouts for the current bus instance, do not get them all
				// the reason is that the timeouts repository can be filtered by endpoint 
				// instance to reduce the possibility of handing back a timeout that the 
				// bus instance should not be processing:
				return timeouts.Where(x => x.Endpoint.Equals(endpoint)).ToList();
				//return _timeouts;
			}
		}

		public ICollection<ScheduleTimeout> FindAll()
		{
			if (disposed) return null;

			lock (TimeoutsLock)
			{
				// only return the timeouts for the current bus instance, do not get them all
				// the reason is that the timeouts repository can be filtered by endpoint 
				// instance to reduce the possibility of handing back a timeout that the 
				// bus instance should not be processing (in this case just hand them all back):
				return timeouts;
			}
		}

		public void Add(ScheduleTimeout timeout)
		{
			if (disposed) return;

			lock (TimeoutsLock)
			{
				timeouts.AddUnique(timeout);
			}
		}

		public void Remove(ScheduleTimeout timeout)
		{
			if (disposed) return;

			lock (TimeoutsLock)
			{
				timeouts.Remove(timeout);
			}
		}

		public void Remove(Guid timeoutId)
		{
			if (disposed) return;

			lock (TimeoutsLock)
			{
				ScheduleTimeout tm = (from timeout in timeouts
									  where timeout.Id == timeoutId
									  select timeout).FirstOrDefault();

				if (tm != null)
				{
					timeouts.Remove(tm);
				}
			}
		}

		public void RemoveRequestedTimeouts(Guid requestorId)
		{
			if (disposed) return;

			lock (TimeoutsLock)
			{
				var timeouts = (from timeout in InMemoryTimeoutsRepository.timeouts
								where timeout.RequestorId == requestorId
								select timeout).ToList();

				if (timeouts.Count > 0)
				{
					timeouts.ForEach(timeout => InMemoryTimeoutsRepository.timeouts.Remove(timeout));
				}
			}
		}
	}
}