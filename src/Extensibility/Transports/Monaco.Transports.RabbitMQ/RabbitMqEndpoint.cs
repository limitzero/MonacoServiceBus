using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Monaco.Endpoint;

namespace Monaco.Transports.RabbitMQ
{
	public class RabbitMqEndpoint : BaseEndpoint
	{
		private const string scheme = "rabbitmq";
		private readonly string AddressFormat = string.Concat(scheme, "://{queue}");

		public string EndpointAddressFormat { get; private set; }

		public RabbitMqEndpoint() :
			base(scheme)
		{
			this.EndpointAddressFormat = AddressFormat;
		}

		public override void DoLocalize()
		{
			// transform the rabbit web endpoint to a
			// physical representation for accessing Rabbit MQ:
			string thePath = this.EndpointUri.OriginalString.Replace(this.Protocol, string.Empty);
			
			// drop the query string parameters (if supplied):
			thePath = thePath.Replace(string.Concat("?", this.EndpointUri.Query), string.Empty);

			// drop the trailing "/" character (if found):
			if (thePath.EndsWith("/"))
				thePath.TrimEnd(new char[] { '/' });

			string[] theParts = thePath.Trim().Split(new char[] { '/' });

			if(theParts.Length != 1)
				throw new UriFormatException(string.Format("The addressing scheme for the RabbitMQ endpoint is  '{0}' but was expecting '{1}'", 
					this.EndpointUri.OriginalString, this.EndpointAddressFormat));

			if(string.IsNullOrEmpty(theParts[0]) == true)
				throw new UriFormatException(string.Format("The addressing scheme for the RabbitMQ endpoint is  '{0}' but was expecting '{1}'. No queue name was specified.", 
					this.EndpointUri.OriginalString, this.EndpointAddressFormat));

			string queue = theParts[0].Trim();

			// queue name:
			this.LocalizedEndpointUri = queue;

		}
		
	
	}
}
