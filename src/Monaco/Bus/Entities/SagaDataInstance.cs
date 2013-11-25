using System;

namespace Monaco.Bus.Entities
{
	/// <summary>
	/// Entity that is used to store the data that is used for a particular Saga instance
	/// correlated by the saga instance identifier.
	/// </summary>
	[Serializable]
	public class SagaDataInstance : IMessage, IMonacoEntity
	{
		/// <summary>
		/// Gets or sets the date and time the thread was created.
		/// </summary>
		public virtual DateTime? CreatedOn { get; set; }

		/// <summary>
		/// Gets or sets the date and time the thread was modified.
		/// </summary>
		public virtual DateTime? ModifiedOn { get; set; }

		/// <summary>
		/// Gets or sets the instance of the saga data fully qualified type name.
		/// </summary>
		public virtual string SagaDataName { get; set; }

		/// <summary>
		/// Gets or sets the binary form of the saga data instance.
		/// </summary>
		public virtual byte[] Instance { get; set; }

		/// <summary>
		/// Gets or sets the active correlation of the message to the data in order to 
		/// retreive the specific saga instance.
		/// </summary>
		public virtual string CorrelationId { get; set; }

		#region IMonacoEntity Members

		/// <summary>
		/// Gets or sets the instance identifier of the saga data instance.
		/// </summary>
		public virtual Guid Id { get; set; }

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is SagaDataInstance))
			{
				return false;
			}

			var that = (SagaDataInstance) obj;

			if (Id == null || that.Id == null || !Id.Equals(Id))
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			hashCode = 31*hashCode + (Id == null ? 0 : Id.GetHashCode());
			return hashCode;
		}
	}
}