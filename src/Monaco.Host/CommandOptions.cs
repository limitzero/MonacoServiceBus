namespace Monaco.Host
{
	public enum CommandOptions
	{
		/// <summary>
		/// The file name of the assembly to host in the bus
		/// </summary>
		Assembly, 

		/// <summary>
		/// The name of the configuration file (if the configuration is not present in app.config):
		/// </summary>
		Configuration,

		/// <summary>
		/// The .NET type name of the endpoint configuration that defines the components for the hosted assembly.
		/// </summary>
		Endpoint, 

		/// <summary>
		/// The name of the service in the service control applet
		/// </summary>
		Service, 

		/// <summary>
		/// The description of the service in the service control applet
		/// </summary>
		Description
	}
}