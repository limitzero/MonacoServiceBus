using System;
using System.Collections.Generic;

namespace Monaco.Configuration.Impl
{
	public interface IMessageConfiguration
	{
		/// <summary>
		/// Gets or sets the filter expression for the messages that 
		/// are associated to a remote endpoint (by name) which 
		/// will be involved in the publication/subscription model for the local service bus
		/// </summary>
		IDictionary<string,  Func<Type, bool>> MessagesDefinition { get; set; }
	}

	public class MessageConfiguration : IMessageConfiguration
	{
		public MessageConfiguration()
		{
			this.MessagesDefinition = new Dictionary<string, Func<Type, bool>>();
		}

		public IMessageConfiguration ContainingMessagesFrom(string endpointName, Func<Type, bool> messagesFilterExpression)
		{
			this.MessagesDefinition.Add(endpointName, messagesFilterExpression);
			return this;
		}

		public IDictionary<string, Func<Type, bool>>  MessagesDefinition { get; set; }
	}
}