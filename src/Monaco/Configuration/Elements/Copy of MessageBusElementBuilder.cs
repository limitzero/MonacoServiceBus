using System;
using Castle.Core.Configuration;
using Monaco.Agents.Scheduler;
using Monaco.Agents.Scheduler.Tasks.Configuration;
using Monaco.Agents.Scheduler.Tasks.Configuration.Impl;
using Monaco.Endpoint;
using Monaco.Endpoint.Health.Tasks;
using Monaco.Exceptions;
using Monaco.Internals.Reflection;
using Monaco.Internals.Serialization;
using Monaco.Transport;
using Monaco.Transport.Msmq;

namespace Monaco.Configuration.Elements
{
	/// <summary>
	/// Configuration element parser for the message bus node.
	/// 
	/// <![CDATA[
	/// <message-bus>
	///    <endpoint
	///        name="message bus"
	///        uri="msmq://localhost/monaco.esb"
	///        poision-uri="msmq://localhost/monaco.esb.poison"
	///        concurrency="1"
	///        max-retries="2"
	///        status-interval="00:00:02"
	///        status-interval-grace-period="00:00:05" />
	///  </message-bus>
	///  ]]>
	/// </summary>
	public class MessageBusElementBuilder : BaseElementBuilder
	{
		private const string _element = "message-bus";

		public override bool IsMatchFor(string name)
		{
			return _element.Trim() == name.Trim().ToLower();
		}

		public override void Build(IConfiguration configuration)
		{
			//ITransport transport = null;
			//IReflection reflection = Kernel.Resolve<IReflection>();

			string endpoint = configuration.Attributes["endpoint"] ?? string.Empty;

			if (string.IsNullOrEmpty(endpoint) == true)
			{
				throw NoMessageBusEndpointDefinedException();
			}

			string error = configuration.Attributes["error"] ?? string.Empty;
			string threads = configuration.Attributes["threads"] ?? "1";
			string retries = configuration.Attributes["max-retries"] ?? "5";
			//string statusInterval = configuration.Attributes["status-interval"] ?? "00:00:20";
			//string statusIntervalGracePeriod = configuration.Attributes["status-interval-grace-period"] ?? "00:00:05";

			// configure the transport for the bus:
			this.ConfigureTransportForBus(configuration, endpoint, error, Int32.Parse(retries), Int32.Parse(threads));

			// configure the "heartbeat" for the bus:
			this.ConfigureHeartbeatForBus(configuration, endpoint);

			// configure the control endpoint for the bus:
			this.ConfigureControlEndpointForBus(configuration);

			// examine the transport for the bus instance:
			//IConfiguration transportNode = null;
			//string transportType = string.Empty;
			//string isTransactional = "false";

			//if(configuration.Children.Count > 0)
			//{
			//    transportNode = configuration.Children[0];
			//}
				
			//if(transportNode != null && transportNode.Name == "transport")
			//{
			//    transportType = transportNode.Attributes["type"] ?? string.Empty;
			//    isTransactional = transportNode.Attributes["transactional"] ?? "false";

			//    if (string.IsNullOrEmpty(transportType) == true)
			//    {
			//        throw NoTransportDefinedForMessageBusException();
			//    }

			//    // must be defined as a fully qualified .NET type:
			//    if(transportType.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries).Length > 2)
			//    {
			//        throw TransportTypeNotDefinedAsFullyQualifiedTypeException(transportType);
			//    }
			//}
			//else
			//{
			//    // use the default msmq transport:
			//    transportType = typeof(MsmqTransport).AssemblyQualifiedName;
			//    isTransactional = "true";
			//}

			//// create the transport and assign the properties:
			//transport = BuildTransport(reflection, transportType);

			//transport.EndpointUri = endpoint;
			//transport.IsTransactional = bool.Parse(isTransactional);
			//transport.MaxRetries = Int32.Parse(retries);
			//transport.NumberOfWorkerThreads = Int32.Parse(threads);
			//transport.SerializationProvider = Kernel.Resolve<ISerializationProvider>();

			//if (string.IsNullOrEmpty(error) == true)
			//{
			//    transport.ErrorEndpointUri = string.Concat(endpoint, ".error");
			//}
			//else
			//{
			//    transport.ErrorEndpointUri = error;
			//}

			//Kernel.AddComponentInstance(typeof(ITransport).Name, typeof(ITransport), transport);

			//this.ConfigureHeartBeat(endpoint, statusInterval, statusIntervalGracePeriod);
		}

		private void ConfigureTransportForBus(IConfiguration configuration, string endpoint, string error, int retries, int threads)
		{
			ITransport transport = null;
			string transportType = string.Empty;
			string isTransactional = "false";

			IConfiguration transportConfiguration = FindConfigurationNode(configuration, "transport");

			if (transportConfiguration != null)
			{
				transportType = transportConfiguration.Attributes["type"] ?? string.Empty;
				isTransactional = transportConfiguration.Attributes["transactional"] ?? "false";

				// check the defined transport to make sure it is specified:
				if (string.IsNullOrEmpty(transportType) == true)
				{
					throw NoTransportDefinedForMessageBusException();
				}

				// must be defined as a fully qualified .NET type:
				if (transportType.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Length > 2)
				{
					throw TransportTypeNotDefinedAsFullyQualifiedTypeException(transportType);
				}
			}
			else
			{
				// use the default Msmq transport for the bus:
				transportType = typeof(MsmqTransport).AssemblyQualifiedName;
				isTransactional = "true";
			}

			// create the transport and assign the properties:
			IReflection reflection = Kernel.Resolve<IReflection>();
			transport = BuildTransport(reflection, transportType);

			transport.EndpointUri = endpoint;
			transport.IsTransactional = bool.Parse(isTransactional);
			transport.MaxRetries = retries;
			transport.NumberOfWorkerThreads = threads;
			transport.SerializationProvider = Kernel.Resolve<ISerializationProvider>();

			if (string.IsNullOrEmpty(error) == true)
			{
				transport.ErrorEndpointUri = string.Concat(endpoint, ".error");
			}
			else
			{
				transport.ErrorEndpointUri = error;
			}

			Kernel.AddComponentInstance(typeof(ITransport).Name, typeof(ITransport), transport);
		}

		private void ConfigureHeartbeatForBus(IConfiguration configuration, string endpoint)
		{
			IConfiguration heartbeatConfiguration = FindConfigurationNode(configuration, "heartbeat");

			if (heartbeatConfiguration == null) return;

			string interval = configuration.Attributes["interval"] ?? string.Empty;
			string gracePeriod = configuration.Attributes["grace-period"] ?? "00:00:20";

			if (string.IsNullOrEmpty(interval) == true) return;

			EndpointHeartBeatTask task = new EndpointHeartBeatTask(Guid.NewGuid().ToString(), endpoint, interval, gracePeriod);
			IScheduler scheduler = Kernel.Resolve<IScheduler>();

			ITaskConfiguration heartbeatTask = new TaskConfiguration();
			heartbeatTask.TaskName = string.Format("Heartbeat task for endpoint <{0}>@<{1}>", endpoint, interval);
			heartbeatTask.ComponentInstance = task;
			heartbeatTask.Interval = interval;
			heartbeatTask.HaltOnError = false;

			scheduler.CreateFromConfiguration(heartbeatTask);
		}

		private void ConfigureControlEndpointForBus(IConfiguration configuration)
		{
			IConfiguration controlEndpointConfiguration = FindConfigurationNode(configuration, "control");

			if (controlEndpointConfiguration == null) return;

			string endpoint = controlEndpointConfiguration.Attributes["endpoint"] ?? string.Empty;
			string interval = controlEndpointConfiguration.Attributes["broadcast-interval"] ?? string.Empty;
			string recycle = controlEndpointConfiguration.Attributes["recycle-interval"] ?? "00:05:00";

			if (string.IsNullOrEmpty(endpoint)) return;

			// register the control endpoint:
			IControlEndpoint controlEndpoint = new ControlEndpoint {Uri = endpoint};
			Kernel.AddComponentInstance(typeof(IControlEndpoint).Name, typeof(IControlEndpoint),  controlEndpoint);

			if (string.IsNullOrEmpty(interval)) return;

			IScheduler scheduler = Kernel.Resolve<IScheduler>();

			EndpointsStatusBroadcastTask broadcastTask = new EndpointsStatusBroadcastTask();
			scheduler.CreateScheduledItem("Broadcast All Endpoint Status", interval.Trim(), broadcastTask, "Produce", true, true);

			EndpointRecycleStatisticsTask task = new EndpointRecycleStatisticsTask();
			scheduler.CreateScheduledItem("Recycle All Endpoint Statistics", recycle.Trim(), task, "Produce", true, true);


		}

		private static IConfiguration FindConfigurationNode(IConfiguration configuration, string nodeToSearchFor)
		{
			IConfiguration configurationNode = null;

			for (int index = 0; index < configuration.Children.Count; index++)
			{
				var node = configuration.Children[index];

				if (node.Name.Trim().ToLower() != nodeToSearchFor) continue;

				configurationNode = node;
				break;
			}

			return configurationNode;
		}

		private void ConfigureHeartBeat(string endpoint, string interval, string gracePeriod)
		{
			EndpointHeartBeatTask task = new EndpointHeartBeatTask(Guid.NewGuid().ToString(), endpoint, interval, gracePeriod);
			IScheduler scheduler = Kernel.Resolve<IScheduler>();

			ITaskConfiguration heartbeatTask = new TaskConfiguration();
			heartbeatTask.TaskName = string.Format("Heartbeat task for endpoint <{0}>@<{1}>", endpoint, interval);
			heartbeatTask.ComponentInstance = task;
			heartbeatTask.Interval = interval;
			heartbeatTask.HaltOnError = false;

			scheduler.CreateFromConfiguration(heartbeatTask);
		}

		private static ITransport BuildTransport(IReflection reflection, string transportType)
		{
			ITransport transport = null;
			object instance = null;

			try
			{
				instance = reflection.BuildInstance(transportType);
			}
			catch (Exception exception)
			{
				throw CouldNotBuildTransportFromDefinedTypeException(transportType, exception);
			}

			transport = instance as ITransport;

			if (transport == null)
			{
				throw TransportNotDefinedFromTargetInterfaceException(transportType);
			}

			return transport;
		}

		private static MonacoConfigurationException TransportTypeNotDefinedAsFullyQualifiedTypeException(string transportType)
		{
			throw new MonacoConfigurationException(string.Format("The transport type '{0}' was not defined as a fully qualified type.", transportType));
		}

		private static MonacoConfigurationException TransportNotDefinedFromTargetInterfaceException(string transportType)
		{
			return
				new MonacoConfigurationException(string.Format("The following transport '{0}' is not defined from '{1}'. " +
															   "Please ensure that this transport inherits this interface for message transmisstion on the message bus.",
															   transportType, typeof(ITransport).FullName));
		}

		private static MonacoConfigurationException CouldNotBuildTransportFromDefinedTypeException(string transportType, Exception exception)
		{
			return
				new MonacoConfigurationException(string.Format("The following type could not be built '{0}' for the transport. " +
															   "Please specify the correct type name and update the configuration file.",
															   transportType), exception);
		}

		private static MonacoConfigurationException NoMessageBusEndpointDefinedException()
		{
			return new MonacoConfigurationException("For the message bus, the endpoint uri specifying its location was not specified. " +
					"Please specify an endpoint location that is specific to the endpoint transport semantics.");
		}

		private static MonacoConfigurationException NoTransportDefinedForMessageBusException()
		{
			return new MonacoConfigurationException("There was not a transport defined for the current bus instance. Please specifiy a fully qualified " +
			"type that derives from " + typeof(ITransport).FullName + " add create a section under the message bus with the following: " +
			"<transport add=\"{your concrete transport implemenation}\"> in the configuration file");
		}
	}
}