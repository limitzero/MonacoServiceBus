using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Persistance.Repositories;

namespace Monaco.Persistance.Timeouts
{
    public class InMemoryTimeoutsRepository : ITimeoutsRepository, IDisposable
    {
    	private bool _disposing;
        private static readonly object _timeouts_lock = new object();
        private static IThreadSafeList<ScheduleTimeout> _timeouts;

        public InMemoryTimeoutsRepository()
        {
        	if(_timeouts == null)
            {
                _timeouts = new ThreadSafeList<ScheduleTimeout>();
            }
        }

    	public void Dispose()
        {
            _disposing = true;

            if (_timeouts != null)
            {
                _timeouts.Clear();
                _timeouts = null;
            }

        }

    	public ICollection<ScheduleTimeout> FindAll(string endpoint)
    	{
			if (this._disposing == true) return null;

			lock (_timeouts_lock)
			{
				// only return the timeouts for the current bus instance, do not get them all
				// the reason is that the timeouts repository can be filtered by endpoint 
				// instance to reduce the possibility of handing back a timeout that the 
				// bus instance should not be processing:
				return _timeouts.Where(x => x.Endpoint == endpoint).ToList();
				//return _timeouts;
			}
    	}

    	public ICollection<ScheduleTimeout> FindAll()
        {
			if (this._disposing == true) return null;
          
            lock(_timeouts_lock)
            {
				// only return the timeouts for the current bus instance, do not get them all
				// the reason is that the timeouts repository can be filtered by endpoint 
				// instance to reduce the possibility of handing back a timeout that the 
				// bus instance should not be processing (in this case just hand them all back):
            	return _timeouts;
            }
        }

        public void Add(ScheduleTimeout timeout)
        {
			if (this._disposing == true) return;
            
            lock (_timeouts_lock)
            {
                _timeouts.AddUnique(timeout);
            }
        }

        public void Remove(ScheduleTimeout timeout)
        {
			if (this._disposing == true) return;
            
            lock (_timeouts_lock)
            {
                _timeouts.Remove(timeout);
            }
        }

        public void Remove(Guid timeoutId)
        {
			if (this._disposing == true) return;
           
            lock (_timeouts_lock)
            {
                ScheduleTimeout tm = (from timeout in _timeouts
                                      where timeout.Id == timeoutId
                                      select timeout).FirstOrDefault();

                if (tm != null)
                {
                    _timeouts.Remove(tm);
                }
            }
        }
    }
}