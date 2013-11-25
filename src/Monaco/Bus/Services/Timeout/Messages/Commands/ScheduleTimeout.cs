using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.Timeout.Messages.Commands
{
	[Serializable]
	public class ScheduleTimeout : IAdminMessage
	{
		/// <summary>
		/// Default .ctor
		/// </summary>
		public ScheduleTimeout()
		{
		}

		/// <summary>
		/// Default .ctor
		/// </summary>
		public ScheduleTimeout(Guid instanceId, TimeSpan duration)
			: this(instanceId, duration, null)
		{
		}

		/// <summary>
		/// .ctor
		/// </summary>
		/// <param name="duration">Time that the message should be held from processing</param>
		/// <param name="messageToDeliver">Message to hold from processing.</param>
		public ScheduleTimeout(TimeSpan duration, IMessage messageToDeliver)
			: this(Guid.NewGuid(), duration, messageToDeliver)
		{
		}

		/// <summary>
		/// .ctor
		/// </summary>
		/// <param name="id">The unique instance given to the scheduled timeout</param>
		/// <param name="duration">Time that the message should be held from processing</param>
		/// <param name="messageToDeliver">Message to hold from processing.</param>
		public ScheduleTimeout(Guid id, TimeSpan duration, IMessage messageToDeliver)
		{
			Id = id;
			Duration = duration;
			MessageToDeliver = messageToDeliver;
			CreatedOn = DateTime.Now;
			At = CreateDateTimeFromTimespan(duration);
		}

		/// <summary>
		/// Gets or sets the instance of the identifier for the timeout message.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the time for the message to be singled that it has "timed-out".
		/// </summary>
		public DateTime At { get; set; }

		/// <summary>
		/// Gets or sets the date/time the timeout message was requested.
		/// </summary>
		public DateTime CreatedOn { get; set; }

		/// <summary>
		/// Gets or sets the duration to wait before delivering the message.
		/// </summary>
		public TimeSpan Duration { get; set; }

		/// <summary>
		/// Gets or sets the message to deliver after the given interval.
		/// </summary>
		public object MessageToDeliver { get; set; }

		/// <summary>
		/// Gets or sets the bus endpoint that generated the timeout message.
		/// </summary>
		public string Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the identifier of the requestor (state machine) that requested the timeout
		/// </summary>
		public Guid? RequestorId { get; set; }

		/// <summary>
		/// Gets or sets the message consumer that requested the timeout
		/// </summary>
		public string Requestor { get; set; }

		/// <summary>
		/// This will produce the cancellation message 
		/// for the currently scheduled timeout message.
		/// </summary>
		/// <returns></returns>
		public CancelTimeout CreateCancelMessage()
		{
			return new CancelTimeout(Id);
		}

		public bool HasExpired()
		{
			var span = DateTime.Now - this.At;
			return span.Seconds > 0;
		}

		private DateTime CreateDateTimeFromTimespan(TimeSpan span)
		{
			DateTime dateTime = CreatedOn;
			dateTime = dateTime.AddDays(span.Days);
			dateTime = dateTime.AddHours(span.Hours);
			dateTime = dateTime.AddMinutes(span.Minutes);
			dateTime = dateTime.AddSeconds(span.Seconds);
			dateTime = dateTime.AddMilliseconds(span.Milliseconds);
			return dateTime;
		}
	}
}