using System;
using Monaco.Endpoint;

namespace Monaco.Transports.Msmq
{
	public class MsmqEndpoint : BaseEndpoint
	{
		private const string _endpointAddressFormat = "msmq://{server or ip address}/{msmq name}";

		public MsmqEndpoint()
			: base("msmq")
		{
		}

		public override void DoLocalize()
		{
			// transform the msmq web endpoint to a
			// physical representation for accessing MSMQ:

			string path = string.Empty;

			string thePath = this.EndpointUri.OriginalString.Replace(this.Protocol, string.Empty);

			// drop the query string parameters (if suppllied):
			thePath = thePath.Replace(string.Concat("?", this.EndpointUri.Query), string.Empty);

			// drop the trailing "/" character (if found):
			if (thePath.EndsWith("/"))
				thePath.TrimEnd(new char[] {'/'});

			string[] theParts = thePath.Trim().Split(new char[] { '/' });

			string server = theParts[0].Trim().ToLower() == "localhost"
								? Environment.MachineName
								: theParts[0].Trim().ToUpper();

			string queue = theParts[1].Trim().ToLower();

			// needed for store-and-forward capability of MSMQ (remote queue):
			if (Environment.MachineName.ToLower() != server.ToLower())
			{
				path = string.Format(@"FormatName:DIRECT=OS:{0}\private$\{1}", server, queue);
			}
			else
			{
				// local queues:
				path = string.Format(@"{0}\private$\{1}", server, queue);
			}

			this.LocalizedEndpointUri = path;

		}
	}
}