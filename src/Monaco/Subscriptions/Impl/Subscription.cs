using System;
using System.Text;

namespace Monaco.Subscriptions.Impl
{
	/// <summary>
	/// Concrete instance of a message subscription.
	/// </summary>
	[Serializable]
	public class Subscription : ISubscription
	{
		public Subscription()
			: this(true, string.Empty, string.Empty, string.Empty)
		{
		}

		public Subscription(bool isActive, string uri, string component, string message)
		{
			Id = CombGuid.NewGuid();
			IsActive = isActive;
			Uri = uri;
			Component = component;
			Message = message;
		}

		#region ISubscription Members

		public virtual Guid Id { get; set; }
		public virtual bool IsActive { get; set; }
		public virtual string Uri { get; set; }
		public virtual string Component { get; set; }
		public virtual string Message { get; set; }

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is Subscription))
			{
				return false;
			}

			var that = (Subscription) obj;
			if (Id == null || that.Id == null || !Id.Equals(Id))
			{
				return false;
			}
			return true;
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.Append("Subscription {")
				.Append("Id=").Append(Id.ToString()).Append(", ")
				.Append("IsActive=").Append(IsActive.ToString()).Append(", ")
				.Append("Uri=").Append(Uri).Append(", ")
				.Append("Component=").Append(Component).Append(", ")
				.Append("Message=").Append(Message)
				.Append("}");
			return builder.ToString();
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			hashCode = 107*hashCode + (Id == null ? 0 : Id.GetHashCode());
			return hashCode;
		}
	}
}