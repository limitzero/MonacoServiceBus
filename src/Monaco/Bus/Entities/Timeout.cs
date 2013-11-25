using System;

namespace Monaco.Bus.Entities
{
	[Serializable]
	public class Timeout : IMessage, IMonacoEntity
	{
		/// <summary>
		/// Gets or sets the instance identifier.
		/// </summary>
		public virtual Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the date and time the thread was created.
		/// </summary>
		public virtual DateTime? CreatedOn { get; set; }

		/// <summary>
		/// Gets or sets the date and time the thread was modified.
		/// </summary>
		public virtual DateTime? ModifiedOn { get; set; }

		/// <summary>
		/// Gets or sets the date and time the the timeout will be delivered.
		/// </summary>
		public virtual DateTime Invocation { get; set; }

		/// <summary>
		/// Gets or sets the instance of the message to be delivered after the timeout period
		/// </summary>
		public virtual string Message { get; set; }

		/// <summary>
		/// Gets or sets the binary form of the scheduled timeout instance.
		/// </summary>
		public virtual byte[] Instance { get; set; }

		/// <summary>
		/// Gets or sets the endpoint that generated the timeout request.
		/// </summary>
		public virtual string Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the identifier of the requestor (state machine) that requested the timeout
		/// </summary>
		public virtual Guid? RequestorId { get; set; }

		/// <summary>
		/// Gets or sets the message consumer that requested the timeout
		/// </summary>
		public virtual string Requestor { get; set; }

		public override bool Equals(object obj)
		{
			if (!(obj is Timeout))
			{
				return false;
			}

			var that = (Timeout) obj;
			if (Id == null || that.Id == null || !Id.Equals(Id))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			hashCode = 110*hashCode + (Id == null ? 0 : Id.GetHashCode());
			return hashCode;
		}
	}
}