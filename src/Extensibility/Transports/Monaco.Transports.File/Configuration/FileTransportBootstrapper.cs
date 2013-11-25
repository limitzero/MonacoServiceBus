using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Monaco.Extensibility.Transports;

namespace Monaco.Transports.File.Configuration
{
	/// <summary>
	/// Transport boot-strapper for receiving and sending messages via the file system.
	/// </summary>
	public class FileTransportBootstrapper : BaseTransportBootstrapper
	{
		private const string elementName = "file.transport";

		public FileTransportBootstrapper()
		{
			this.ElementName = elementName;
		}

	}
}