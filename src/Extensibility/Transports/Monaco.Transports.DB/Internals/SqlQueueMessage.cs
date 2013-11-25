using System;

namespace Monaco.Transports.DB.Internals
{
	public class SqlQueueMessage
	{
		public int Id { get; private set; }
		public string QueueName { get; private set; }
		public string Message { get; private set; }
		public DateTime? CreatedDate { get; private set; }
		public DateTime? ProcessedDate { get; private set; }
		public string MessageLabel { get; set; }

		public SqlQueueMessage(int id, string queueName, string message, DateTime? createdDate, DateTime? processedDate)
		{
			Id = id;
			QueueName = queueName;
			Message = message;
			CreatedDate = createdDate;
			ProcessedDate = processedDate;
		}

		public void SetId(int id)
		{
			this.Id = id;
		}
	}
}