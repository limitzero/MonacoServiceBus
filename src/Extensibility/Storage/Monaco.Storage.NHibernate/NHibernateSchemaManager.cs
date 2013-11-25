namespace Monaco.Storage.NHibernate
{
	public class NHibernateSchemaManager : INHibernateSchemaManager
	{
		private readonly global::NHibernate.Cfg.Configuration _configuration;

		public NHibernateSchemaManager(global::NHibernate.Cfg.Configuration configuration)
		{
			_configuration = configuration;
		}

		public void Dispose()
		{
			this.DropSchema();
		}

		public  void CreateSchema()
		{
			var exporter = new global::NHibernate.Tool.hbm2ddl.SchemaExport(_configuration);
			exporter.Execute(true, true, false);
		}

		public void DropSchema()
		{
			var exporter = new global::NHibernate.Tool.hbm2ddl.SchemaExport(_configuration);
			exporter.Execute(true, true, true);
		}
	}
}