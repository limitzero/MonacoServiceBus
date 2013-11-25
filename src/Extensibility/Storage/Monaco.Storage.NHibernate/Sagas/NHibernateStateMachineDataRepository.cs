using System;
using System.Collections.Generic;
using Monaco.StateMachine;
using Monaco.StateMachine.Persistance;
using NHibernate;

namespace Monaco.Storage.NHibernate.Sagas
{
	public class NHibernateStateMachineDataRepository<TStateMachineData>
		: BaseStateMachineDataRepository<TStateMachineData> 
		where TStateMachineData : class, IStateMachineData, new() 
	{
		private readonly ISession session;

		public NHibernateStateMachineDataRepository(ISession session) 
		{
			this.session = session;
		}

		public override IEnumerable<TStateMachineData> FindAll()
		{
			return session.CreateCriteria<TStateMachineData>().List<TStateMachineData>();
		}

		public override TStateMachineData DoFindStateMachineData(Guid instanceId)
		{
			return session.Get<TStateMachineData>(instanceId);
		}

		public override void Save(TStateMachineData stateMachineData)
		{
			using(var txn = session.BeginTransaction())
			{
				try
				{
					session.SaveOrUpdate(stateMachineData);
					txn.Commit();
				}
				catch
				{
					txn.Rollback();
					throw;
				}
			}
		}

		public override void Remove(TStateMachineData stateMachineData)
		{
			using (var txn = session.BeginTransaction())
			{
				try
				{
					session.Delete(stateMachineData);
					txn.Commit();
				}
				catch 
				{
					txn.Rollback();
					throw;
				}
			}
		}

		public override void Remove(Guid instanceId)
		{
			using (var txn = session.BeginTransaction())
			{
				try
				{
					var stateMachineData = this.Find(instanceId);

					if (stateMachineData != null)
					{
						session.Delete(stateMachineData);
						txn.Commit();
					}
				}
				catch
				{
					txn.Rollback();
					throw;
				}
			}
		}
	}
}