using System;
using System.Collections.Generic;

namespace Monaco.Endpoint
{
	/// <summary>
	/// Base implementation of an endpoint that all concrete instances
	/// should implement.
	/// </summary>
	public abstract class BaseEndpoint : IEndpoint, IDisposable
	{
		protected BaseEndpoint(string scheme)
		{
			Scheme = scheme;
			Protocol = string.Concat(Scheme, "://");
			Properties = new Dictionary<string, string>();
		}

		/// <summary>
		/// This will allow an endpoint to be constructed by name instead of full Uri specification;
		/// </summary>
		/// <param name="endpointName"></param>
		/// <returns></returns>
		public Uri BuildUriFromEndpointName(string endpointName)
		{
			var uri = new Uri(string.Format("{0}://{1}", this.Scheme, endpointName));
			return uri;
		}

		#region IDisposable Members

		public void Dispose()
		{
			Disposing(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region IEndpoint Members

		public virtual string Scheme { get; private set; }
		public virtual string Protocol { get; private set; }
		public virtual Uri EndpointUri { get; set; }
		public virtual string LocalizedEndpointUri { get; set; }
		public virtual Dictionary<string, string> Properties { get; set; }

		public void Localize()
		{
			DoLocalize();
			LoadPropertiesFromUri();
		}

		#endregion

		/// <summary>
		/// Allows for the concrete endpoint to examine the query string name/value 
		/// pair for custom actions that can be applied to the endpoint behavior.
		/// </summary>
		public event Action<string, string> OnQueryStringNameValuePairRetrieved;

		/// <summary>
		/// This will build the endpoint representation 
		/// from the current uri representing the message 
		/// store.
		/// </summary>
		/// <param name="endpoint"></param>
		public void Configure(Uri endpoint)
		{
			EndpointUri = endpoint;
			Localize();
		}

		public virtual void DoLocalize()
		{
		}

		public virtual void OnDisposing()
		{
		}

		private void LoadPropertiesFromUri()
		{
			string queryString = GetQueryString();

			if (string.IsNullOrEmpty(queryString)) return;

			string[] nameValueParts = queryString.Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries);

			if (nameValueParts.Length > 0)
			{
				foreach (string nameValuePart in nameValueParts)
				{
					string[] pieces = nameValuePart.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);

					if (pieces.Length > 0 && pieces.Length == 2)
					{
						if (OnQueryStringNameValuePairRetrieved != null)
						{
							OnQueryStringNameValuePairRetrieved(pieces[0].Trim(), pieces[1].Trim());
						}
						else
						{
							Properties.Add(pieces[0].Trim(), pieces[1].Trim());
						}
					}
				}
			}
		}

		private string GetQueryString()
		{
			string query = EndpointUri.Query;

			if (string.IsNullOrEmpty(EndpointUri.Query) == false) return query;

			// need to extract query-string from uri based endpoint that is not understood:
			if (EndpointUri.OriginalString.Contains("?") || EndpointUri.OriginalString.Contains("%3F"))
			{
				int start = EndpointUri.OriginalString.IndexOf("%3F");

				if (start < 0)
				{
					start = EndpointUri.OriginalString.IndexOf("?");
				}

				query = EndpointUri.OriginalString.Substring(start);
			}

			query = query.Replace("?", string.Empty);

			return query;
		}

		private void Disposing(bool disposing)
		{
			if (disposing)
			{
				OnDisposing();
			}
		}
	}
}