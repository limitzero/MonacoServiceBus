using System;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Monaco.Bus.Agents.Scheduler;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration.Impl;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.Services.HealthMonitoring;
using Monaco.Bus.Services.HealthMonitoring.Tasks;
using Monaco.Bus.Services.Subscriptions.Tasks;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Impl.Bus;
using Monaco.Endpoint.Impl.Control;
using Monaco.Endpoint.Impl.Log;
using Monaco.Transport;

namespace Monaco.Configuration.Elements
{
	/// <summary>
	/// Configuration element parser for the message bus node.
	/// 
	/// <![CDATA[
	/// <message-bus
	///		threads="1"
	///		retries="5"
	///		error="msmq://localhost/error.queue"
	///		endpoint="msmq://localhost/local.service.bus"
	///		log="msmq://localhost/local.service.bus.log">
	///       
	///		<heartbeat interval="00:00:60" grace-period="00:00:05" />
	/// 
	///		<control endpoint="msmq://locahost/control.endpoint"
	///											 broadcast-interval="00:01:00"
	///											 recycle-interval="00:05:00"/>
	/// 
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

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			string endpoint = configuration.Attributes["endpoint"] ?? string.Empty;

			if (string.IsNullOrEmpty(endpoint))
			{
				throw NoMessageBusEndpointDefinedException();
			}

			string error = configuration.Attributes["error"] ?? string.Empty;
			string log = configuration.Attributes["log"] ?? string.Empty;
			string threads = configuration.Attributes["threads"] ?? "1";
			string retries = configuration.Attributes["retries"] ?? "5";

			// configure the transport for the bus:
			ConfigureBusEndpoint(configuration, endpoint,
								 error, log, Int32.Parse(retries), Int32.Parse(threads));

			// configure the "heartbeat" for the bus:
			ConfigureHeartbeatForBus(configuration, endpoint);

			// configure the control endpoint for the bus:
			ConfigureControlEndpointForBus(configuration);

			// configure the log endpoint for the bus (if not writing to the log file):
			//this.ConfigureLogEndpointForBus(configuration.Attributes["logEndpoint"]);
		}

		private void ConfigureBusEndpoint(Castle.Core.Configuration.IConfiguration configuration,
										  string endpoint,
										  string error,
										  string log,
										  int retries,
										  int threads)
		{
			// build the bus transport and bind to ITransport (only the service bus should get this active transport):
			Exchange exchange = Container.Resolve<IEndpointFactory>().Build(new Uri(endpoint));

			if (exchange != null)
			{
				if (exchange.Transport != null)
				{
					error = string.IsNullOrEmpty(error) ? string.Concat(endpoint, ".error") : error;

					exchange.Transport.MaxRetries = retries;
					exchange.Transport.NumberOfWorkerThreads = threads;

					// register the bus endpoint and use the endpoint setting on every instantiation:
					var errorEndpoint = new ServiceBusErrorEndpoint
											{
												Endpoint = new Uri(error),
											};

					// register the error endpoint and use the endpoint setting:
					Container.RegisterInstance<IServiceBusErrorEndpoint>(errorEndpoint);

					// set up the transport for the bus:
					Container.RegisterInstance<ITransport>(exchange.Transport);
				}
			}

			if (string.IsNullOrEmpty(log) == false)
			{
				// register the log endpoint and use the endpoint setting on every instantiation:
				Container.RegisterInstance<IServiceBusLogEndpoint>(new ServiceBusLogEndpoint { Endpoint = new Uri(log) });
			}
		}

		private void ConfigureHeartbeatForBus(Castle.Core.Configuration.IConfiguration configuration, string endpoint)
		{
			Castle.Core.Configuration.IConfiguration heartbeatConfiguration = FindConfigurationNode(configuration, "heartbeat");

			if (heartbeatConfiguration == null) return;

			Container.Register<HealthMonitoringService>();

			string interval = heartbeatConfiguration.Attributes["interval"] ?? string.Empty;
			string gracePeriod = heartbeatConfiguration.Attributes["grace-period"] ?? "00:00:20";

			if (string.IsNullOrEmpty(interval)) return;

			var task = new EndpointHeartBeatTask(Guid.NewGuid().ToString(), endpoint, interval, gracePeriod);
			var scheduler = Container.Resolve<IScheduler>();

			ITaskConfiguration heartbeatTask = new TaskConfiguration();
			heartbeatTask.TaskName = string.Format("Heartbeat task for endpoint <{0}>@<{1}>", endpoint, interval);
			heartbeatTask.ComponentInstance = task;
			heartbeatTask.Interval = interval;
			heartbeatTask.HaltOnError = false;
			heartbeatTask.ForceStart = true;

			scheduler.CreateFromConfiguration(heartbeatTask);
		}

		private void ConfigureControlEndpointForBus(Castle.Core.Configuration.IConfiguration configuration)
		{
			Castle.Core.Configuration.IConfiguration controlEndpointConfiguration = FindConfigurationNode(configuration, "control");

			if (controlEndpointConfiguration == null) return;

			string endpoint = controlEndpointConfiguration.Attributes["endpoint"] ?? string.Empty;
			string interval = controlEndpointConfiguration.Attributes["broadcast-interval"] ?? string.Empty;
			string recycle = controlEndpointConfiguration.Attributes["recycle-interval"] ?? "00:05:00";

			if (string.IsNullOrEmpty(endpoint)) return;

			// register the control endpoint and use the endpoint setting on every instantiation:
			Container.RegisterInstance<IControlEndpoint>(new ControlEndpoint { Uri = endpoint });

			if (string.IsNullOrEmpty(interval)) return;

			var scheduler = Container.Resolve<IScheduler>();

			var broadcastTask = new PrepareEndpointStatusTask();
			scheduler.CreateScheduledItem("Broadcast Endpoint Status", interval.Trim(), broadcastTask, "Produce", true, true);

			var recycleStatisticsTask = new RecycleStatisticsTask();
			scheduler.CreateScheduledItem("Recycle Endpoint Statistics", recycle.Trim(), recycleStatisticsTask, "Produce", true,
										  true);

			var prepareSubscriptionsTask = new PrepareSubscriptionsTask();
			scheduler.CreateScheduledItem("Prepare Subscriptions Status", interval.Trim(), prepareSubscriptionsTask, "Produce",
										  true, true);
		}

		private static Castle.Core.Configuration.IConfiguration FindConfigurationNode(Castle.Core.Configuration.IConfiguration configuration, string nodeToSearchFor)
		{
			Castle.Core.Configuration.IConfiguration configurationNode = null;

			for (int index = 0; index < configuration.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration node = configuration.Children[index];

				if (node.Name.Trim().ToLower() != nodeToSearchFor) continue;

				configurationNode = node;
				break;
			}

			return configurationNode;
		}

		private void ConfigureHeartBeat(string endpoint, string interval, string gracePeriod)
		{
			var task = new EndpointHeartBeatTask(Guid.NewGuid().ToString(), endpoint, interval, gracePeriod);
			var scheduler = Container.Resolve<IScheduler>();

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
			throw new MonacoConfigurationException(
				string.Format("The transport type '{0}' was not defined as a fully qualified type.", transportType));
		}

		private static MonacoConfigurationException TransportNotDefinedFromTargetInterfaceException(string transportType)
		{
			return
				new MonacoConfigurationException(string.Format("The following transport '{0}' is not defined from '{1}'. " +
															   "Please ensure that this transport inherits this interface for message transmisstion on the message bus.",
															   transportType, typeof(ITransport).FullName));
		}

		private static MonacoConfigurationException CouldNotBuildTransportFromDefinedTypeException(string transportType,
																								   Exception exception)
		{
			return
				new MonacoConfigurationException(string.Format("The following type could not be built '{0}' for the transport. " +
															   "Please specify the correct type name and update the configuration file.",
															   transportType), exception);
		}

		private static MonacoConfigurationException NoMessageBusEndpointDefinedException()
		{
			return
				new MonacoConfigurationException(
					"For the message bus, the endpoint uri specifying its location was not specified. " +
					"Please specify an endpoint location that is specific to the endpoint transport semantics.");
		}

		private static MonacoConfigurationException NoTransportDefinedForMessageBusException()
		{
			return
				new MonacoConfigurationException(
					"There was not a transport defined for the current bus instance. Please specifiy a fully qualified " +
					"type that derives from " + typeof(ITransport).FullName +
					" add create a section under the message bus with the following: " +
					"<transport type=\"{your concrete transport implemenation}\"> in the configuration file");
		}
	}
}