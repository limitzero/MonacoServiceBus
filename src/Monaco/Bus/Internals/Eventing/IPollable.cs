namespace Monaco.Bus.Internals.Eventing
{
	public interface IPollable
	{
		/// <summary>
		/// (Read-Write). The number of active threads polling the location looking for new messages.
		/// </summary>
		int Concurrency { get; set; }

		/// <summary>
		/// (Read-Write). The interval, in seconds, that each thread should wait before polling the location for messages.
		/// </summary>
		int Frequency { get; set; }

		/// <summary>
		/// (Read-Write). The interval or "schedule", in seconds, that the location will be polled looking for new messages.
		/// </summary>
		int Interval { get; set; }
	}
}