using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core.Configuration;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals.Reflection;
using Monaco.Endpoint;
using Monaco.Endpoint.Impl.Bus;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Subscriptions.Impl;
using Monaco.Transport;

namespace Monaco.Configuration.Elements
{
	/// <summary>
	/// Configuration element for loading messages into 
	/// the serializer
	/// 
	/// Node Syntax:
	/// <messages>
	///    <add name="{full namespace of assembly for messages}" 
	///             endpoint="{uri location of endpoint (optional, mapped to bus endpoint if not specified)}"/>
	///    ...
	/// </messages>
	/// </summary>
	public class MessagesElementBuilder : BaseElementBuilder
	{
		private const string _element = "messages";

		public override bool IsMatchFor(string name)
		{
			return _element.Trim() == name.Trim().ToLower();
		}

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			for (int index = 0; index < configuration.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration msg = configuration.Children[index];

				string @namespace = msg.Attributes["name"];

				if (string.IsNullOrEmpty(@namespace))
					throw NoNamespaceDefinedForEndpointMessages();

				string endpoint = msg.Attributes["endpoint"] ?? string.Empty;

				MapToEndpoint(@namespace, endpoint);
			}
		}

		private void MapToEndpoint(string @namespace, string endpoint = "")
		{
			Uri endpointUri;

			if(string.IsNullOrEmpty(endpoint) || Uri.TryCreate(endpoint, UriKind.RelativeOrAbsolute, out endpointUri) == false)
				throw new MonacoConfigurationException("The endpoint for the received messages 'add name={} endpoint={} must have a valid uri definition of the endpoint for receiving the messages.");

			// find the assembly that matches the namespace provided:
			Assembly messagesAssembly = GetAssemblyForNamespace(@namespace);

			IEnumerable<Type> messages = messagesAssembly.GetTypes()
				.Where(t => ((t.IsClass && !t.IsAbstract) || t.IsInterface) && t.Namespace == @namespace);

			IEnumerable<Type> interfaceMessages = messages.Where(m => m.IsInterface);
			IEnumerable<Type> concreteMessages = messages.Where(m => m.IsClass && !m.IsAbstract);

			MapRegisteredMessages(interfaceMessages.ToList(), @namespace, endpoint);
			MapRegisteredMessages(concreteMessages.ToList(), @namespace, endpoint);
		}

		private void MapRegisteredMessages(ICollection<Type> messages, string @namespace, string endpoint)
		{
			var subscriptionRepository = Container.Resolve<ISubscriptionRepository>();

			foreach (Type message in messages)
			{
				var subscription = new Subscription();
				subscription.IsActive = true;
				subscription.Uri = endpoint;

				if (message.FullName.StartsWith(@namespace))
				{
					subscription.Message = message.FullName;
				}

				if (string.IsNullOrEmpty(subscription.Message) == false)
					subscriptionRepository.Register(subscription);
			}
		}

		private void MapConcreteMessages(ICollection<Type> messages, string @namespace, string endpoint)
		{
			var subscriptionRepository = Container.Resolve<ISubscriptionRepository>();

			foreach (Type message in messages)
			{
				var subscription = new Subscription();
				subscription.IsActive = true;
				subscription.Uri = endpoint;

				if (message.FullName.StartsWith(@namespace))
				{
					subscription.Message = message.FullName;
				}

				if (string.IsNullOrEmpty(subscription.Message) == false)
					subscriptionRepository.Register(subscription);
			}
		}

		private void MapInterfaceBasedMessages(ICollection<Type> messages, string @namespace, string endpoint)
		{
			var subscriptionRepository = Container.Resolve<ISubscriptionRepository>();

			foreach (Type message in messages)
			{
				var subscription = new Subscription();
				subscription.IsActive = true;
				subscription.Uri = endpoint;

				if (message.FullName.StartsWith(@namespace))
				{
					subscription.Message = message.FullName;
				}

				if (string.IsNullOrEmpty(subscription.Message) == false)
					subscriptionRepository.Register(subscription);
			}
		}

		private static Assembly GetAssemblyForNamespace(string @namespace)
		{
			var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

			FileInfo assemblyFile = (from file in directory.GetFiles("*.dll")
			                         let fileName = file.Name.Replace(".dll", string.Empty).ToLower().Trim()
			                         where @namespace.Trim().ToLower().StartsWith(fileName)
			                         select file).FirstOrDefault();

			if (assemblyFile == null)
				throw CouldNotFindAssemblyStartingWithNamespaceException(@namespace);

			Assembly asm = Assembly.LoadFile(assemblyFile.FullName);

			return asm;
		}

		private static Exception NoNamespaceDefinedForEndpointMessages()
		{
			return
				new MonacoConfigurationException(
					"For creating a set of messages to be mapped to an endpoint, the name of the messages " +
					"must be supplied and conform to a namespace that holds the definition of the message " +
					"(ex: OrderApprovedMessage is found under the specific namespace MyMessages.Orders");
		}

		private static Exception CouldNotFindAssemblyStartingWithNamespaceException(string @namespace)
		{
			throw new MonacoConfigurationException(
				string.Format("The a file starting with the following namespace '{0}' could not be " +
				              "found in the executable directory for loading the messages for the message bus. Please check the namespace declaration " +
				              " in the configuration file to make sure that the namespace matches a library containing your messages.",
				              @namespace));
		}

		public static ICollection<Type> FindPublishableMessages(Type theType)
		{
			return FindServerPublishableMessages(theType.Assembly);
		}

		private static ICollection<Type> FindClientPublishableMessages(Assembly theAssembly)
		{
			var publishableMessages = new List<Type>();

			foreach (Type theType in theAssembly.GetTypes())
			{
				if (theType.IsInterface && typeof (IMessage).IsAssignableFrom(theType))
				{
					publishableMessages.Add(theType);
				}

				if (theType.IsEnum)
				{
					publishableMessages.Add(theType);
				}

				if (theType.IsClass && typeof (IMessage).IsAssignableFrom(theType))
				{
					publishableMessages.Add(theType);
				}
			}

			return publishableMessages;
		}

		public static ICollection<Type> FindServerPublishableMessages(Assembly theAssembly)
		{
			var publishableMessages = new List<Type>();

			foreach (Type theType in theAssembly.GetTypes())
			{
				if (theType.IsEnum)
				{
					publishableMessages.Add(theType);
				}

				if (theType.IsClass && typeof (IMessage).IsAssignableFrom(theType))
				{
					publishableMessages.Add(theType);
				}
			}

			return publishableMessages.Distinct().ToList();
		}

		private ICollection<Type> BuildAssemblyForInterfaceBasedMessages(ICollection<Type> messages)
		{
			ICollection<Type> theInterfaceBasedMessages = (from message in messages
			                                               where message.IsInterface &&
			                                                     message.Assembly != GetType().Assembly
			                                               select message).Distinct().ToList();

			ICollection<Type> theProxiedTypes = Container.Resolve<IReflection>()
				.BuildProxyAssemblyForContracts(theInterfaceBasedMessages, true);

			return theProxiedTypes;
		}

		private void BuildSubscriptionListing(ICollection<Type> messages, string endpoint)
		{
			var subscriptionRepository = Container.Resolve<ISubscriptionRepository>();
			var serviceBusEndpoint = Container.Resolve<IServiceBusEndpoint>();

			foreach (Type message in messages)
			{
				string theEndpoint = endpoint == string.Empty ? serviceBusEndpoint.Endpoint.ToString() : endpoint;

				subscriptionRepository.Register(
					new Subscription
						{
							Message = message.FullName,
							Uri = theEndpoint,
							IsActive = true
						});
			}
		}
	}
}