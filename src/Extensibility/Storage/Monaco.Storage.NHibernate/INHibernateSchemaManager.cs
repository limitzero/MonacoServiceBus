using System;

namespace Monaco.Storage.NHibernate
{
	/// <summary>
	/// Contract for creating and removing the persistance store using NHibernate.
	/// </summary>
	public interface INHibernateSchemaManager : IDisposable
	{
		void CreateSchema();
		void DropSchema();
	}
}