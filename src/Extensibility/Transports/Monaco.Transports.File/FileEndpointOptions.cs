namespace Monaco.Transports.File
{
	public enum FileEndpointOptions
	{
		/// <summary>
		/// Query string parameter to retreive file in the source directory 
		/// with a specified file extention for a receive operation. Default is *.txt
		/// </summary>
		ReceiveExtension,

		/// <summary>
		/// Query string parameter to create file in the target directory 
		/// with a specified file extention for a send operation. Default is *.txt
		/// </summary>
		SendExtension,

		/// <summary>
		/// Query string parameter to rename file in the source directory 
		/// with a specified file extention for a completed receive operation. Default is *.processed
		/// </summary>
		ProcessedExtension,

		/// <summary>
		/// Query string with sub-directory to move the file to after successful processing
		/// </summary>
		MoveTo
	}
}