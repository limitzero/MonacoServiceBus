using System;

namespace Monaco.Bus.Messages.For.Endpoints
{
	/// <summary>
	/// Message to inquire about the status of a particular endpoint.
	/// </summary>
	[Serializable]
	public class EndpointStatusInquiryMessage : IAdminMessage
	{
		public EndpointStatusInquiryMessage()
		{
		}

		public EndpointStatusInquiryMessage(string endpointName, string endpointUri)
		{
			EndpointName = endpointName;
			EndpointUri = endpointUri;
		}

		public string EndpointName { get; set; }
		public string EndpointUri { get; set; }
	}
}