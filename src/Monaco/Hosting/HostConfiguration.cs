using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Castle.Core.Configuration;
using Monaco.Configuration;
using IConfiguration = Monaco.Configuration.IConfiguration;

namespace Monaco.Hosting
{
	public class HostConfiguration
	{
		public HostConfiguration()
		{
			MaxThreads = 1;
			MaxRetries = 5;
			//HeartBeatInterval = "00:00:05";
			//GracePeriodInterval = "00:00:05";
			Messages = new Dictionary<string, string>();
		}

		protected string Endpoint { get; private set; }
		protected string ErrorEndpoint { get; private set; }
		protected int MaxThreads { get; private set; }
		protected string HeartBeatInterval { get; private set; }
		protected string GracePeriodInterval { get; private set; }
		protected int MaxRetries { get; private set; }
		protected IDictionary<string, string> Messages { get; private set; }
		public IConfiguration Configuration { get; private set; }

		public HostConfiguration UsingConfiguration(Expression<Func<IConfiguration, IConfiguration>> configuration)
		{
			this.Configuration = configuration.Compile().Invoke(Monaco.Configuration.Configuration.Instance);
			return this;
		}

		/// <summary>
		/// This will define the endpoint that the service bus
		/// will listen to for processing messages.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns></returns>
		public HostConfiguration Bus(string endpoint)
		{
			Endpoint = endpoint;
			return this;
		}

		public HostConfiguration Error(string endpoint)
		{
			ErrorEndpoint = endpoint;
			return this;
		}

		public HostConfiguration Retries(int retries)
		{
			MaxRetries = retries;
			return this;
		}

		public HostConfiguration Heartbeat(string interval)
		{
			HeartBeatInterval = interval;
			return this;
		}

		public HostConfiguration GracePeriod(string interval)
		{
			GracePeriodInterval = interval;
			return this;
		}

		/// <summary>
		/// This will tell the service bus how to route received 
		/// messages from a given assembly namespace back 
		/// to the endpoint for processing.
		/// </summary>
		/// <param name="assemblyName">Namespace that contains the remote messages</param>
		/// <param name="endpoint">Endpoint to send these messages back to</param>
		/// <returns></returns>
		public HostConfiguration Receive(string assemblyName, string endpoint)
		{
			Messages.Add(assemblyName, endpoint);
			return this;
		}

		public HostConfiguration Receive<TMessage>(string endpoint)
			where TMessage : IMessage
		{
			Messages.Add(typeof(TMessage).Namespace, endpoint);
			return this;
		}

		public Castle.Core.Configuration.IConfiguration Build()
		{
			var configuration =
				new MutableConfiguration(MonacoFacility.FACILITY_ID);

			MutableConfiguration messageBus = configuration.CreateChild("message-bus");

			messageBus.Attribute("endpoint", Endpoint);

			if (string.IsNullOrEmpty(ErrorEndpoint))
			{
				ErrorEndpoint = string.Concat(Endpoint, ".error");
			}
			messageBus.Attribute("error", ErrorEndpoint);

			messageBus.Attribute("threads", MaxThreads.ToString());
			messageBus.Attribute("retries", MaxRetries.ToString());

			// create the "heart-beat" (along with grace period):
			if (string.IsNullOrEmpty(HeartBeatInterval) == false)
			{
				MutableConfiguration heartbeat = messageBus.CreateChild("heartbeat");
				heartbeat.Attribute("interval", HeartBeatInterval);

				if (string.IsNullOrEmpty(GracePeriodInterval))
					GracePeriodInterval = "00:00:05";

				heartbeat.Attribute("grace-period", GracePeriodInterval);
			}

			// create the remote messages that can be processing on the service bus:
			MutableConfiguration messages = configuration.CreateChild("messages");

			foreach (var message in Messages)
			{
				MutableConfiguration receive = messages.CreateChild("add");
				receive.Attribute("name", message.Key);
				receive.Attribute("endpoint", message.Value);
			}

			return configuration;
		}
	}
}