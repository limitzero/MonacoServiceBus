using System;

namespace Monaco.Storage.NHibernate.Tests
{
    public class SchemaManager : IDisposable
    {
    	private readonly global::NHibernate.Cfg.Configuration configuration;

		public SchemaManager(global::NHibernate.Cfg.Configuration configuration)
		{
			this.configuration = configuration;
		}

    	public void Dispose()
        {
            this.DropSchema();
        }

        public  void CreateSchema()
        {
            var exporter = new global::NHibernate.Tool.hbm2ddl.SchemaExport(this.configuration);
            exporter.Execute(true, true, false);
        }

		public void DropSchema()
        {
            var exporter = new global::NHibernate.Tool.hbm2ddl.SchemaExport(this.configuration);
            exporter.Execute(true, true, true);
        }

    }
}