using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Monaco.Endpoint;

namespace Monaco.Transports.File
{
	public class FileEndpoint : BaseEndpoint
	{
		private const string _endpointAddressFormat = "file://{directory location}?wildcard={*.file extention}";

		public FileEndpoint() :
			base("file")
		{
			base.OnQueryStringNameValuePairRetrieved += OnFileEndpointQueryStringNameValuePairRetrieved;
		}

		public override void OnDisposing()
		{
			base.OnQueryStringNameValuePairRetrieved -= OnFileEndpointQueryStringNameValuePairRetrieved;
		}

		private void OnFileEndpointQueryStringNameValuePairRetrieved(string name, string value)
		{
			if(name.ToLower() == FileEndpointOptions.ProcessedExtension.ToString().ToLower())
			{
				if (string.IsNullOrEmpty(value)) value = "*.processed";
				this.Properties.Add(FileEndpointOptions.ProcessedExtension.ToString(), value);
			}
		}

		public override void DoLocalize()
		{
			// transform the file endpoint to a physical representation for accessing the file system:
			string thePath = this.EndpointUri.OriginalString.Replace(this.Protocol, string.Empty);

			thePath = thePath.Replace("%3F", "?"); //catch the URL encoding

			// drop the starting "/" character (if found):
			if (thePath.StartsWith(@"/"))
			{
				thePath = thePath.TrimStart(@"/".ToCharArray());
			}

			int queryStringPos = thePath.IndexOf("?");

			if (queryStringPos > 0)
			{
				// must remove everything after the "?" including the question mark itself:
				thePath = thePath.Substring(0, queryStringPos);
			}
			else
			{
				// drop the query string parameters (if suppllied):
				thePath = thePath.Replace(string.Concat("?", this.EndpointUri.Query), string.Empty);
			}

			// drop the trailing "\" character (if found):
			if (thePath.EndsWith(@"\"))
			{
				thePath = thePath.TrimEnd(@"\".ToCharArray());
			}

			// directory location is all we need, the folder structure will be the storage location:
			this.LocalizedEndpointUri = @thePath.Replace(@"/",@"\");

		}

	}
}
