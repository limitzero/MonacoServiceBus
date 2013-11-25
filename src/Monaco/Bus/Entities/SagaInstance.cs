using System;

namespace Monaco.Bus.Entities
{
	[Serializable]
	public class SagaInstance : IMessage, IMonacoEntity
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
		/// Gets or sets the instance of the saga fully qualified type name.
		/// </summary>
		public virtual string SagaName { get; set; }

		/// <summary>
		/// Gets or sets the flag to denote whether or not the current saga is suspended.
		/// </summary>
		public virtual bool IsSuspended { get; set; }

		/// <summary>
		/// Gets or sets the current state of the saga.
		/// </summary>
		public virtual string State { get; set; }

		/// <summary>
		/// Gets or sets the binary form of the saga instance.
		/// </summary>
		public virtual byte[] Instance { get; set; }

		#region IMonacoEntity Members

		/// <summary>
		/// Gets or sets the instance identifier of the saga
		/// </summary>
		public virtual Guid Id { get; set; }

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is SagaInstance))
			{
				return false;
			}

			var that = (SagaInstance) obj;

			if (Id == null ||
			    that.Id == null ||
			    !Id.Equals(Id))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			hashCode = 29*hashCode + (Id == null ? 0 : Id.GetHashCode());
			return hashCode;
		}
	}
}