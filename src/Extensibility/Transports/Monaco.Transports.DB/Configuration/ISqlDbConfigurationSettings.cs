namespace Monaco.Transports.DB.Configuration
{
	public interface ISqlDbConfigurationSettings
	{
		string UserName { get; }
		string Password { get; }
		string Server { get; }
		string Catalog { get; set; }
		bool AutoDelete { get; set; }
	}
}