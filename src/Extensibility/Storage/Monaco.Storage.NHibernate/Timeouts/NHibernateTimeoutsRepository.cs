using System;
using System.Collections.Generic;
using Monaco.Bus.Entities;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Transport;
using NHibernate;
using NHibernate.Criterion;

namespace Monaco.Storage.NHibernate.Timeouts
{
	public class NHibernateTimeoutsRepository : ITimeoutsRepository
	{
		private readonly ISession _session;
		private readonly ISerializationProvider _serialization;
		private readonly ITransport _transport;

		public NHibernateTimeoutsRepository(ISession session, ISerializationProvider serialization, ITransport transport)
		{
			_session = session;
			_serialization = serialization;
			_transport = transport;
		}

		public ICollection<ScheduleTimeout> FindAll(string endpoint)
		{
			ICollection<ScheduleTimeout> timeouts = new List<ScheduleTimeout>();

			try
			{
				DetachedCriteria criteria = DetachedCriteria.For<Timeout>().SetMaxResults(50)
					.Add(Expression.Eq("Endpoint", endpoint));

				var result = criteria.GetExecutableCriteria(_session).List<Timeout>();
				timeouts = FromThreads(result);
			}
			catch
			{
				throw;
			}

			return timeouts;
		}

		public ICollection<ScheduleTimeout> FindAll()
		{
			// Note: this should not be implemented
			ICollection<ScheduleTimeout> timeouts = new List<ScheduleTimeout>();
			return timeouts;
		}

		public void Add(ScheduleTimeout timeout)
		{
			Timeout previousThread = this.FindThread(timeout.Id);
			Timeout theThread = this.CreateThread(timeout, previousThread);

			using (var txn = _session.BeginTransaction())
			{
				try
				{
					_session.Save(theThread);
					txn.Commit();
				}
				catch
				{
					txn.Rollback();
					throw;
				}
			}
		}

		public void Remove(ScheduleTimeout timeout)
		{
			Timeout theThread = this.FindThread(timeout.Id);

			if (theThread != null)
			{
				using (var txn = _session.BeginTransaction())
				{
					try
					{
						_session.Delete(theThread);
						txn.Commit();
					}
					catch
					{
						txn.Rollback();
						throw;
					}
				}
			}
		}

		public void Remove(Guid timeoutId)
		{
			Timeout theThread = this.FindThread(timeoutId);

			if (theThread != null)
			{
				using (var txn = _session.BeginTransaction())
				{
					try
					{
						_session.Delete(theThread);
						txn.Commit();
					}
					catch
					{
						txn.Rollback();
						throw;
					}
				}
			}
		}

		public void RemoveRequestedTimeouts(Guid requestorId)
		{
			using (var txn = this._session.BeginTransaction())
			{
				try
				{
					var query = _session.CreateSQLQuery("DELETE FROM Timeouts WHERE RequestorId = :requestorId")
						.SetGuid("requestorId", requestorId);
					query.UniqueResult();
					txn.Commit();
				}
				catch 
				{
					txn.Rollback();
					throw;
				}
			}
		}

		private Timeout FindThread(Guid instanceId)
		{
			Timeout thread = null;

			try
			{
				thread = _session.Get<Timeout>(instanceId);
			}
			catch
			{
				throw;
			}

			return thread;
		}

		private Timeout CreateThread(ScheduleTimeout timeout, Timeout previousThread)
		{
			Timeout theThread = new Timeout();

			if (previousThread != null)
			{
				theThread.CreatedOn = previousThread.CreatedOn;
			}
			else
			{
				theThread.CreatedOn = DateTime.Now;
			}

			theThread.Id = timeout.Id;
			theThread.Invocation = timeout.At;
			theThread.Message = timeout.MessageToDeliver.GetType().FullName;
			theThread.Instance = _serialization.SerializeToBytes(timeout);
			theThread.Message = timeout.MessageToDeliver.GetType().FullName;
			theThread.Endpoint = timeout.Endpoint;

			return theThread;
		}

		private ICollection<ScheduleTimeout> FromThreads(IEnumerable<Timeout> threads)
		{
			List<ScheduleTimeout> timeouts = new List<ScheduleTimeout>();

			foreach (Timeout thread in threads)
			{
				try
				{
					object aThread = _serialization.Deserialize(thread.Instance);
					if (aThread != null)
					{
						timeouts.Add(aThread as ScheduleTimeout);
					}
				}
				catch (Exception)
				{
					continue;
				}
			}

			return timeouts;
		}
	}
}
