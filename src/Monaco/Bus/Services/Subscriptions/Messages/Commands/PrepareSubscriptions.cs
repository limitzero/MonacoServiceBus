using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.Subscriptions.Messages.Commands
{
	public class PrepareSubscriptions : IAdminMessage
	{
		public DateTime At { get; set; }
		public string Endpoint { get; set; }
	}
}