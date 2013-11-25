namespace Monaco.Configuration
{
	/// <summary>
	/// Marker point to indicate that this is the central location where the 
	/// configuration options for the messaging endpoint will be defined.
	/// </summary>
	public interface ICanConfigureEndpoint
	{
		void Configure(IConfiguration configuration);
	}
}