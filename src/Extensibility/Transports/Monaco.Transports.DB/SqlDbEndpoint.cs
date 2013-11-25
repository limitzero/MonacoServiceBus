using System;
using Monaco.Endpoint;

namespace Monaco.Transports.DB
{
	public class SqlDbEndpoint : BaseEndpoint
	{
		private const string scheme = "sqldb";
		private readonly string endpointAddressFormat = string.Format("{0}://{{tablename}}", scheme);

		public string EndpointAddressFormat { get; private set; }

		public SqlDbEndpoint() :
			base(scheme)
		{
			this.EndpointAddressFormat = endpointAddressFormat;
		}

		public override void DoLocalize()
		{
			string table = this.EndpointUri.OriginalString.Replace(this.Protocol, string.Empty).Trim();

			if(string.IsNullOrEmpty(table))
				throw new UriFormatException(string.Format("The addressing scheme for the SqlDb endpoint is '{0}' but was expecting '{1}'", 
					this.EndpointUri.OriginalString, this.EndpointAddressFormat));

			table = table.Replace(".", "_");

			this.LocalizedEndpointUri = table;
		}
	}
}