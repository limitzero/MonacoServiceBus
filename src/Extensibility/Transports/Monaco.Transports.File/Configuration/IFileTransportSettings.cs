namespace Monaco.Transports.File.Configuration
{
	public interface IFileTransportConfiguration
	{
		string ProcessedFileExtension { get; set; }
		string SendFileExtension { get; set; }
		string ReceiveFileExtension { get; set; }
		string MoveToDirectory { get; set;}
		bool AutoDelete { get; set; }
	}
}