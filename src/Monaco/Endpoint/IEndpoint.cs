using System;
using System.Collections.Generic;

namespace Monaco.Endpoint
{
	/// <summary>
	/// Contract for an endpoint that represents a physical location to a message store.
	/// </summary>
	public interface IEndpoint
	{
		/// <summary>
		/// Gets the addressing scheme for the endpoint uri denoting where 
		/// the messages will be located for receipt or delivery
		/// </summary>
		string Scheme { get; }

		/// <summary>
		/// Gets the scheme plus addressing method to access the resource 
		/// (typically {scheme}://{path to resource}/?{options}
		/// </summary>
		string Protocol { get; }

		/// <summary>
		/// Gets or sets the endpoint full uri to the message store 
		/// where messages can be received or delivered.
		/// </summary>
		Uri EndpointUri { get; set; }

		/// <summary>
		/// Gets the localized endpoint definition specific to the technology
		/// to connect to the message store.
		/// </summary>
		string LocalizedEndpointUri { get; set; }

		/// <summary>
		/// Gets or set any additional name/value properties
		/// needed to connect to the message store.
		/// </summary>
		Dictionary<string, string> Properties { get; set; }

		/// <summary>
		/// This will transform the <seealso cref="EndpointUri"/>
		/// to the <seealso cref="LocalizedEndpointUri"/> definition
		/// needed to access the message store.
		/// </summary>
		void Localize();
	}
}